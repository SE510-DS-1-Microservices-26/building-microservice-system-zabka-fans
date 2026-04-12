using Microsoft.EntityFrameworkCore;
using UserService.Api;
using UserService.Api.Middleware;
using UserService.Api.UserEndpoints;
using UserService.Infrastructure;
using UserService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

if (args.Contains("--migrate"))
{
    await MigrateAndExit(app);
    return;
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();

app.MapUserEndpoints();
app.MapHealthEndpoints();

app.Run();

static async Task MigrateAndExit(WebApplication app)
{
    const int maxRetries = 10;
    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
            await db.Database.MigrateAsync();
            app.Logger.LogInformation("UserService migrations applied successfully.");
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

public partial class Program { }
