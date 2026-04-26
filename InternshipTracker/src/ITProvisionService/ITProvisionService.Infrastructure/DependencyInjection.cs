using ITProvisionService.Infrastructure.Consumers;
using ITProvisionService.Infrastructure.Hosting;
using ITProvisionService.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ITProvisionService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddITProvisionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddITProvisionDatabase(configuration);
        services.AddMessaging(configuration);
        services.AddHostedService<DatabaseMigrationService>();

        return services;
    }

    private static void AddITProvisionDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ITProvisionDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("ITProvisionDb");
            if (string.IsNullOrEmpty(connectionString))
                connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException(
                    "IT Provision DB connection string is not configured. " +
                    "Set 'ConnectionStrings:ITProvisionDb' in appsettings or the 'CONNECTION_STRING' environment variable.");

            options.UseNpgsql(connectionString);
        });
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
            cfg.AddConsumer<ProvisionCorporateAccountConsumer>();

            cfg.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(rabbitHost, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                rabbit.ConfigureEndpoints(context);
            });

            cfg.AddEntityFrameworkOutbox<ITProvisionDbContext>(outboxCfg =>
            {
                outboxCfg.QueryDelay = TimeSpan.FromSeconds(60);
                outboxCfg.DuplicateDetectionWindow = TimeSpan.FromSeconds(60);
                outboxCfg.UsePostgres();
            });

            cfg.AddConfigureEndpointsCallback((context, name, receiveConfigurator) =>
            {
                receiveConfigurator.UseEntityFrameworkOutbox<ITProvisionDbContext>(context);
                receiveConfigurator.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
            });
        });
    }
}

