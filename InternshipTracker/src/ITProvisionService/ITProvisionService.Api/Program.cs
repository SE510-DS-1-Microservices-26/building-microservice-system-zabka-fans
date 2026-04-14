using ITProvisionService.Api.Middleware;
using ITProvisionService.Infrastructure;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddITProvisionInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ITProvisionService"))
        .AddAspNetCoreInstrumentation()
        .AddSource("MassTransit"));

builder.Logging.Configure(options =>
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSwagger();

app.MapGet("/health", () => Results.Ok(new { Status = "ITProvisionService is healthy" }));

app.Run();

