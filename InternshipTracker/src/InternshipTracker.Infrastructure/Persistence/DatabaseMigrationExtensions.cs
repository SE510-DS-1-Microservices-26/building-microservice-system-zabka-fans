using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InternshipTracker.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyAutomaticMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        try
        {
            var context = scopedProvider.GetRequiredService<AppDbContext>();
            var logger = scopedProvider.GetRequiredService<ILogger<AppDbContext>>();

            logger.LogInformation("Attempting to apply database migrations...");

            if (context.Database.IsRelational())
                await context.Database.MigrateAsync();
            else
                await context.Database.EnsureCreatedAsync();

            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            var logger = scopedProvider.GetRequiredService<ILogger<AppDbContext>>();
            logger.LogError(ex, "An error occurred while applying database migrations.");
            throw;
        }
    }
}