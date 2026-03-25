using CoreService.Api.CoreEndpoints;
using CoreService.Infrastructure;
using CoreService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCoreInfrastructure(builder.Configuration);
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
    await db.Database.MigrateAsync();
}

app.MapApplicationEndpoints();
app.MapInternshipEndpoints();
app.MapHealthEndpoints();
app.Run();

public partial class Program { }