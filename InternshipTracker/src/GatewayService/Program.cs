using System.Diagnostics;
using GatewayService.Resilience;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Resilience: bind options and register the custom YARP HTTP client factory.
builder.Services.Configure<ResilienceOptions>(
    builder.Configuration.GetSection(ResilienceOptions.SectionName));
builder.Services.AddSingleton<IForwarderHttpClientFactory, ResilientForwarderHttpClientFactory>();

// OpenTelemetry: register the ASP.NET Core instrumentation so that
// Activity.Current is populated for every request, which lets the
// correlation-ID middleware below derive the ID from the W3C TraceId.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("GatewayService"))
        .AddAspNetCoreInstrumentation());

// Enrich every structured log entry with the active TraceId / SpanId.
builder.Logging.Configure(options =>
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);

var app = builder.Build();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/core/swagger/v1/swagger.json", "Core Service API");
    options.SwaggerEndpoint("/users/swagger/v1/swagger.json", "User Service API");
    options.SwaggerEndpoint("/it-provision/swagger/v1/swagger.json", "IT Provision Service API");
    options.SwaggerEndpoint("/notification/swagger/v1/swagger.json", "Notification Service API");
    options.RoutePrefix = "swagger";
});

// Correlation ID middleware: derive from W3C TraceId so the value echoed in
// response headers is identical to the trace ID visible in any tracing tool.
app.Use(async (context, next) =>
{
    const string correlationIdHeader = "X-Correlation-ID";

    var correlationId =
        context.Request.Headers[correlationIdHeader].FirstOrDefault()
        ?? Activity.Current?.TraceId.ToString()
        ?? Guid.NewGuid().ToString("N");

    // Ensure downstream services receive the header via YARP forwarding.
    if (!context.Request.Headers.ContainsKey(correlationIdHeader))
        context.Request.Headers[correlationIdHeader] = correlationId;

    context.Response.OnStarting(() =>
    {
        context.Response.Headers[correlationIdHeader] = correlationId;
        return Task.CompletedTask;
    });

    await next();
});

app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
