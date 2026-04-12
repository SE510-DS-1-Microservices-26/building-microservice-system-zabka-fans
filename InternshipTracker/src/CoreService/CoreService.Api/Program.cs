using CoreService.Api.CoreEndpoints;
using CoreService.Api.Middleware;
using CoreService.Infrastructure;
using CoreService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCoreInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();


if (!app.Environment.IsEnvironment("Testing"))
{
    // Retry migration up to 5 times — the DB container may still be initialising
    const int maxRetries = 5;
    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            app.Logger.LogWarning(ex, "Database migration attempt {Attempt}/{Max} failed. Retrying in 3 s…", attempt, maxRetries);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

app.MapApplicationEndpoints();
app.MapInternshipEndpoints();
app.MapUserEndpoints();
app.MapHealthEndpoints();
app.Run();

public partial class Program { }