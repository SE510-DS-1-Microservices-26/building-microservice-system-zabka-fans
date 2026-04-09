using System.Net;
using CoreService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InternshipTracker.Tests;

public class HealthEndpointTests
{
    [Test]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    // Replace PostgreSQL CoreDbContext with in-memory
                    var dbOptionsDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<CoreDbContext>));
                    if (dbOptionsDescriptor != null)
                        services.Remove(dbOptionsDescriptor);

                    services.AddDbContext<CoreDbContext>(options =>
                        options.UseInMemoryDatabase("TestCoreDb"));

                    // Remove MassTransit hosted services — no RabbitMQ needed for health check
                    var hostedServices = services
                        .Where(d => d.ServiceType == typeof(IHostedService) &&
                                    d.ImplementationType?.Namespace?.Contains("MassTransit") == true)
                        .ToList();
                    foreach (var s in hostedServices)
                        services.Remove(s);
                });
            });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}