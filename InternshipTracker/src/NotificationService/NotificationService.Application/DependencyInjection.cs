using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NotificationService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MassTransit + outbox configuration has moved to NotificationService.Infrastructure.
        return services;
    }
}

