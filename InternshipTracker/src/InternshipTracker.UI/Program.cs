using DotNetEnv;
using InternshipTracker.Infrastructure;
using InternshipTracker.Infrastructure.Persistence;
using InternshipTracker.UI.Endpoints;

Env.Load("../../.env");
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
await app.Services.ApplyAutomaticMigrationsAsync();
app.MapInternshipEndpoints();
app.MapUserEndpoints();
app.MapApplicationEndpoints();
app.MapHealthEndpoints();

app.Run();

public partial class Program
{
}