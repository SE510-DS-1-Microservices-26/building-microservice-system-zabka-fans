using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Infrastructure.Consumers;
using NotificationService.Infrastructure.Hosting;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddNotificationDatabase(configuration);
        services.AddMessaging(configuration);
        services.AddHostedService<DatabaseMigrationService>();

        return services;
    }

    private static void AddNotificationDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("NotificationDb");
            if (string.IsNullOrEmpty(connectionString))
                connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException(
                    "Notification DB connection string is not configured. " +
                    "Set 'ConnectionStrings:NotificationDb' in appsettings or the 'CONNECTION_STRING' environment variable.");

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
            cfg.AddConsumer<SendWelcomeEmailConsumer>();

            cfg.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(rabbitHost, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                rabbit.ConfigureEndpoints(context);
            });

            cfg.AddEntityFrameworkOutbox<NotificationDbContext>(outboxCfg =>
            {
                outboxCfg.QueryDelay = TimeSpan.FromSeconds(60);
                outboxCfg.DuplicateDetectionWindow = TimeSpan.FromSeconds(60);
                outboxCfg.UsePostgres();
            });

            cfg.AddConfigureEndpointsCallback((context, name, receiveConfigurator) =>
            {
                receiveConfigurator.UseEntityFrameworkOutbox<NotificationDbContext>(context);
                receiveConfigurator.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
            });
        });
    }
}

