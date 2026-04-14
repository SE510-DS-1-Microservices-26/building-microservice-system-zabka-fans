using CoreService.Api.CoreEndpoints;
using CoreService.Api.Middleware;
using CoreService.Infrastructure;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCoreInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CoreService"))
        .AddAspNetCoreInstrumentation()
        .AddSource("MassTransit"));

builder.Logging.Configure(options =>
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();

app.MapApplicationEndpoints();
app.MapInternshipEndpoints();
app.MapUserEndpoints();
app.MapHealthEndpoints();
app.Run();


public partial class Program { }