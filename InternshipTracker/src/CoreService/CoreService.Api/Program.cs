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