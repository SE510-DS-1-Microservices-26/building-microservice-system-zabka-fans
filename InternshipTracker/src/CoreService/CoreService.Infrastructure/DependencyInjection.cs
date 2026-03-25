using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Factories;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Application.Services;
using CoreService.Application.UseCases;
using CoreService.Domain.Interfaces;
using CoreService.Infrastructure.ExternalServices;
using CoreService.Infrastructure.Persistence;
using CoreService.Infrastructure.Persistence.Repositories;
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
        services.AddDbContext<CoreDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("CoreDb");
            if (string.IsNullOrEmpty(connectionString))
                connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            options.UseNpgsql(connectionString);
        });
        
        services.AddScoped<IInternshipRepository, InternshipRepository>();
        services.AddScoped<IInternshipApplicationRepository, InternshipApplicationRepository>();

        services.AddScoped<IDuplicateApplicationChecker, DuplicateApplicationChecker>();
        services.AddScoped<IInternshipCapacityChecker, InternshipCapacityChecker>();

        services.AddScoped<InternshipApplicationFactory>();
        
        services.AddScoped<IUseCase<CreateInternshipRequest, InternshipResponse>, CreateInternshipUseCase>();
        services.AddScoped<IUseCase<GetInternshipRequest, InternshipResponse>, GetInternshipUseCase>();
        services
            .AddScoped<IUseCase<ApplyForInternshipRequest, ApplyForInternshipResponse>, ApplyForInternshipUseCase>();
        services.AddScoped<IUseCase<ChangeApplicationStatusRequest>, ChangeApplicationStatusUseCase>();
        
        services.AddHttpClient<IUserValidationService, HttpUserValidationService>(client =>
        {
            client.BaseAddress = new Uri(configuration["UsersService:BaseUrl"]!);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }
}