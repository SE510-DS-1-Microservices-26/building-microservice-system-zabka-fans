using NotificationService.Api.Middleware;
using NotificationService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNotificationInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSwagger();

app.MapGet("/health", () => Results.Ok(new { Status = "NotificationService is healthy" }));

app.Run();

