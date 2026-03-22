using InternshipTracker.Application.DTOs.Requests;
using InternshipTracker.Application.DTOs.Responses;
using InternshipTracker.Application.Interfaces;
using InternshipTracker.Application.Interfaces.Repositories;
using InternshipTracker.Application.Services;
using InternshipTracker.Application.UseCases;
using InternshipTracker.Domain.Factories;
using InternshipTracker.Domain.Interfaces;
using InternshipTracker.Infrastructure.Persistence;
using InternshipTracker.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InternshipTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInternshipRepository, InternshipRepository>();
        services.AddScoped<IInternshipApplicationRepository, InternshipApplicationRepository>();
        
        services.AddScoped<IDuplicateApplicationChecker, DuplicateApplicationChecker>();
        services.AddScoped<IInternshipCapacityChecker, InternshipCapacityChecker>();
        services.AddScoped<IUserEnrollmentChecker, UserEnrollmentChecker>();

        services.AddScoped<InternshipApplicationFactory>();
        
        services.AddScoped<IUseCase<CreateUserRequest, CreateUserResponse>, CreateUserUseCase>();
        services.AddScoped<IUseCase<GetUserRequest, GetUserResponse>, GetUserUseCase>();
        services.AddScoped<IUseCase<CreateInternshipRequest, InternshipResponse>, CreateInternshipUseCase>();
        services.AddScoped<IUseCase<GetInternshipRequest, InternshipResponse>, GetInternshipUseCase>();
        services.AddScoped<IUseCase<ApplyForInternshipRequest, ApplyForInternshipResponse>, ApplyForInternshipUseCase>();
        services.AddScoped<IUseCase<ChangeApplicationStatusRequest>, ChangeApplicationStatusUseCase>();

        return services;
    }
}