using ITProvisionService.Infrastructure.Consumers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ITProvisionService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddITProvisionInfrastructure(
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
        });

        return services;
    }
}

