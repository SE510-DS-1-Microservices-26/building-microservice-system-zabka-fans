using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Factories;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Application.Services;
using CoreService.Application.UseCases;
using CoreService.Domain.Interfaces;
using CoreService.Infrastructure.Messaging.Consumers;
using CoreService.Infrastructure.Persistence;
using CoreService.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCoreDatabase(configuration);
        services.AddRepositories();
        services.AddDomainServices();
        services.AddUseCases();
        services.AddMessaging(configuration);

        return services;
    }

    private static void AddCoreDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CoreDbContext>(options =>
        {
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
                connectionString = configuration.GetConnectionString("CoreDb");

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException(
                    "Core DB connection string is not configured. " +
                    "Set 'ConnectionStrings:CoreDb' in appsettings or the 'CONNECTION_STRING' environment variable.");

            options.UseNpgsql(connectionString);
        });
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IInternshipRepository, InternshipRepository>();
        services.AddScoped<IInternshipApplicationRepository, InternshipApplicationRepository>();
        services.AddScoped<IUserCoreRepository, UserCoreRepository>();
    }

    private static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IDuplicateApplicationChecker, DuplicateApplicationChecker>();
        services.AddScoped<IInternshipCapacityChecker, InternshipCapacityChecker>();
        services.AddScoped<InternshipApplicationFactory>();
    }

    private static void AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IUseCase<CreateInternshipRequest, InternshipResponse>, CreateInternshipUseCase>();
        services.AddScoped<IUseCase<GetInternshipRequest, InternshipResponse>, GetInternshipUseCase>();
        services.AddScoped<IUseCase<GetAllInternshipsRequest, PagedResult<InternshipResponse>>, GetAllInternshipsUseCase>();
        services.AddScoped<IUseCase<GetUserRequest, UserCoreResponse>, GetUserCoreUseCase>();
        services
            .AddScoped<IUseCase<ApplyForInternshipRequest, ApplyForInternshipResponse>, ApplyForInternshipUseCase>();
        services.AddScoped<IUseCase<GetAllApplicationsRequest, PagedResult<ApplicationResponse>>, GetAllApplicationsUseCase>();
        services.AddScoped<IUseCase<ChangeApplicationStatusRequest>, ChangeApplicationStatusUseCase>();
    }

    private static void AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitUser = configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPass = configuration["RabbitMQ:Password"] ?? "guest";

        services.AddMassTransit(cfg =>
        {
            cfg.AddConsumer<UserDbMessageConsumer>();
            cfg.AddConsumer<UserDbMessageFaultConsumer>();

            cfg.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(rabbitHost, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                rabbit.UseRawJsonDeserializer(RawSerializerOptions.AnyMessageType);
                rabbit.ConfigureEndpoints(context);
            });

            cfg.AddEntityFrameworkOutbox<CoreDbContext>(outboxCfg =>
            {
                outboxCfg.QueryDelay = TimeSpan.FromSeconds(60);
                outboxCfg.DuplicateDetectionWindow = TimeSpan.FromSeconds(60);
                outboxCfg.UsePostgres();
            });

            cfg.AddConfigureEndpointsCallback((context, name, receiveConfigurator) =>
            {
                receiveConfigurator.UseEntityFrameworkOutbox<CoreDbContext>(context);
                receiveConfigurator.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
            });
        });
    }
}