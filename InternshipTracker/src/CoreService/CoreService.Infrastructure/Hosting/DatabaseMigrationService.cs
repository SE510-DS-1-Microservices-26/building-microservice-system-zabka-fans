using CoreService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreService.Infrastructure.Hosting;

public class DatabaseMigrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 10;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
                await db.Database.MigrateAsync(stoppingToken);
                _logger.LogInformation("CoreService database migrations applied successfully.");
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested && attempt < maxRetries)
            {
                _logger.LogWarning(ex,
                    "CoreService migration attempt {Attempt}/{Max} failed. Retrying in 3 s…",
                    attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}

