using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.DTOs.Requests;
using UserService.Application.DTOs.Responses;
using UserService.Application.Interfaces;
using UserService.Application.UseCases;
using UserService.Infrastructure.Messaging;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Persistence.Repositories;

namespace UserService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddUserDatabase(configuration);
        services.AddRepositories();
        services.AddUseCases();
        services.AddMessaging(configuration);

        return services;
    }

    private static void AddUserDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<UserDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("UsersDb");
            if (string.IsNullOrEmpty(connectionString))
                connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException(
                    "Database connection string is not configured. " +
                    "Set 'ConnectionStrings:UsersDb' in appsettings or the 'CONNECTION_STRING' environment variable.");

            options.UseNpgsql(connectionString);
        });
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserDbMessagePublisher, UserDbMessagePublisher>();
    }

    private static void AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IUseCase<CreateUserRequest, UserResponse>, CreateUserUseCase>();
        services.AddScoped<IUseCase<GetUserRequest, UserResponse>, GetUserByIdUseCase>();
        services.AddScoped<IUseCase<DeleteUserRequest>, DeleteUserUseCase>();
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
            cfg.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(rabbitHost, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                rabbit.UseRawJsonSerializer(RawSerializerOptions.AddTransportHeaders, isDefault: true);
                rabbit.ConfigureEndpoints(context);
            });

            cfg.AddEntityFrameworkOutbox<UserDbContext>(outboxCfg =>
            {
                outboxCfg.QueryDelay = TimeSpan.FromSeconds(60);
                outboxCfg.DuplicateDetectionWindow = TimeSpan.FromSeconds(60);
                outboxCfg.UsePostgres();
                outboxCfg.UseBusOutbox();
            });
        });
    }
}