using ITProvisionService.Api.Middleware;
using ITProvisionService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddITProvisionInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSwagger();

app.MapGet("/health", () => Results.Ok(new { Status = "ITProvisionService is healthy" }));

app.Run();

