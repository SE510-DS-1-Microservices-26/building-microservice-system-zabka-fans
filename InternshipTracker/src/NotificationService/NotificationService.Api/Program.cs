using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNotificationInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (args.Contains("--migrate"))
{
    await MigrateAndExit(app);
    return;
}

app.UseSwagger();

app.MapGet("/health", () => Results.Ok(new { Status = "NotificationService is healthy" }));

app.Run();

static async Task MigrateAndExit(WebApplication app)
{
    const int maxRetries = 10;
    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            await db.Database.MigrateAsync();
            app.Logger.LogInformation("NotificationService migrations applied successfully.");
            return;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            app.Logger.LogWarning(ex,
                "Migration attempt {Attempt}/{Max} failed. Retrying in 3 s…",
                attempt, maxRetries);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}
