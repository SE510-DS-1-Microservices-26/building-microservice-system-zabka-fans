using Microsoft.EntityFrameworkCore;
using UserService.Api;
using UserService.Infrastructure;
using UserService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.MigrateAsync();
}

app.MapUserEndpoints();
app.MapGet("/health", () => Results.Ok("User Service is healthy"));

app.Run();

public partial class Program { }
