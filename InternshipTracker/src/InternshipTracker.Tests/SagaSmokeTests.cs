using System.Net;
using System.Net.Http.Json;
using Contracts.Commands;
using Contracts.Events;
using CoreService.Application.DTOs.Responses;
using CoreService.Domain.Enums;
using CoreService.Infrastructure.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace InternshipTracker.Tests;

public class SagaSmokeTests
{
    private PostgreSqlContainer _postgres = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("smoke_test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Remove ALL CoreDbContext-related registrations (options + internal configs)
                    var dbRelated = services
                        .Where(d =>
                        {
                            var st = d.ServiceType;
                            var it = d.ImplementationType;
                            bool mentions(Type? t) => t != null &&
                                (t == typeof(CoreDbContext) || t == typeof(DbContextOptions<CoreDbContext>)
                                 || (t.IsGenericType && t.GenericTypeArguments.Contains(typeof(CoreDbContext))));
                            return mentions(st) || mentions(it);
                        })
                        .ToList();
                    foreach (var d in dbRelated) services.Remove(d);

                    services.AddDbContext<CoreDbContext>(opts =>
                        opts.UseNpgsql(_postgres.GetConnectionString()));

                    // Remove all MassTransit registrations from the production DI
                    var mtDescriptors = services
                        .Where(d => d.ServiceType.FullName?.Contains("MassTransit") == true
                                    || d.ImplementationType?.FullName?.Contains("MassTransit") == true)
                        .ToList();
                    foreach (var d in mtDescriptors) services.Remove(d);

                    var hostedServices = services
                        .Where(d => d.ServiceType == typeof(IHostedService)
                                    && d.ImplementationType?.Namespace?.Contains("MassTransit") == true)
                        .ToList();
                    foreach (var d in hostedServices) services.Remove(d);

                    // Re-register MassTransit with in-memory transport, saga, and mock leaf consumers
                    services.AddMassTransitTestHarness(cfg =>
                    {
                        cfg.AddSagaStateMachine<
                                CoreService.Infrastructure.Saga.OnboardingSagaStateMachine,
                                CoreService.Infrastructure.Saga.OnboardingSagaState>()
                            .InMemoryRepository();

                        cfg.AddConsumer<CoreService.Infrastructure.Messaging.Consumers.RevertApplicationStatusConsumer>();
                        cfg.AddConsumer<CoreService.Infrastructure.Messaging.Consumers.FinalizeEnrollmentConsumer>();
                        cfg.AddConsumer<CoreService.Infrastructure.Messaging.Consumers.FaultApplicationEnrollmentConsumer>();

                        cfg.AddConsumer<MockProvisionConsumer>();
                        cfg.AddConsumer<MockNotificationConsumer>();
                    });
                });
            });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Test]
    public async Task Enroll_HappyPath_ApplicationBecomesEnrolled()
    {
        // 1. Seed a user directly in core DB (simulates user-sync consumer)
        var userId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            db.Users.Add(new CoreService.Domain.Entities.UserCore(
                userId, "Smoke Tester", "smoke@test.com", CandidateLevel.Junior));
            await db.SaveChangesAsync();
        }

        // 2. Create an internship
        var internshipResp = await _client.PostAsJsonAsync("/internships", new
        {
            Title = "Smoke Test Internship",
            Capacity = 5,
            MinimumLevel = 1
        });
        Assert.That(internshipResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var internship = await internshipResp.Content.ReadFromJsonAsync<InternshipResponse>();

        // 3. Apply
        var applyResp = await _client.PostAsJsonAsync("/applications", new
        {
            UserId = userId,
            InternshipId = internship!.Id
        });
        Assert.That(applyResp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var application = await applyResp.Content.ReadFromJsonAsync<ApplyForInternshipResponse>();
        var appId = application!.ApplicationId;

        // 4. Accept the application directly in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var app = await db.Applications.FindAsync(appId);
            app!.MarkAsAccepted();
            await db.SaveChangesAsync();
        }

        // 5. Enroll via POST endpoint — triggers the saga
        var enrollResp = await _client.PostAsync(
            $"/applications/{appId}/enroll", null);
        Assert.That(enrollResp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

        // 6. Poll until saga completes
        ApplicationStatus finalStatus = default;
        for (var i = 0; i < 30; i++)
        {
            await Task.Delay(500);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var app = await db.Applications.AsNoTracking().FirstAsync(a => a.Id == appId);
            finalStatus = app.Status;
            if (finalStatus == ApplicationStatus.Enrolled) break;
        }

        Assert.That(finalStatus, Is.EqualTo(ApplicationStatus.Enrolled),
            "Saga should have driven the application to Enrolled status.");
    }

    // Mock leaf consumers — simulate IT Provision & Notification always succeeding

    private class MockProvisionConsumer : IConsumer<ProvisionCorporateAccountCommand>
    {
        public async Task Consume(ConsumeContext<ProvisionCorporateAccountCommand> context)
        {
            var cmd = context.Message;
            var email = cmd.CandidateName.Replace(" ", ".").ToLowerInvariant() + "@corp.test.com";
            await context.Publish(new AccountProvisionedEvent(cmd.ApplicationId, email));
        }
    }

    private class MockNotificationConsumer : IConsumer<SendWelcomeEmailCommand>
    {
        public async Task Consume(ConsumeContext<SendWelcomeEmailCommand> context)
        {
            await context.Publish(new EmailSentEvent(context.Message.ApplicationId));
        }
    }
}

