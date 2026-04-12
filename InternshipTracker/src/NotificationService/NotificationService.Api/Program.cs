using NotificationService.Application;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNotificationApplication(builder.Configuration);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { Status = "NotificationService is healthy" }));

app.Run();
