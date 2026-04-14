using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using UserService.Api;
using UserService.Api.Middleware;
using UserService.Api.UserEndpoints;
using UserService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("UserService"))
        .AddAspNetCoreInstrumentation()
        .AddSource("MassTransit"));

builder.Logging.Configure(options =>
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();

app.MapUserEndpoints();
app.MapHealthEndpoints();

app.Run();


public partial class Program { }
