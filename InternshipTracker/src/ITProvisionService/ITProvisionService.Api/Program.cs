using ITProvisionService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddITProvisionInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { Status = "ITProvisionService is healthy" }));

app.Run();
