using InternshipTracker.Application.Interfaces.Repositories;
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
        // 1. Register the DbContext with PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // 2. Register Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInternshipRepository, InternshipRepository>();
        services.AddScoped<IInternshipApplicationRepository, InternshipApplicationRepository>();

        return services;
    }
}