using InternshipTracker.Infrastructure;
using InternshipTracker.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();



await app.Services.ApplyAutomaticMigrationsAsync();

app.MapGet("/", () => "Hello World!");

app.Run();