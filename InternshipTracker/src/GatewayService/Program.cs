using GatewayService.Resilience;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(builder.Configuration["OTEL_SERVICE_NAME"] ?? "gateway-service"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Resilience: bind options and register the custom YARP HTTP client factory.
builder.Services.Configure<ResilienceOptions>(
    builder.Configuration.GetSection(ResilienceOptions.SectionName));
builder.Services.AddSingleton<IForwarderHttpClientFactory, ResilientForwarderHttpClientFactory>();

var app = builder.Build();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/core/swagger/v1/swagger.json", "Core Service API");
    options.SwaggerEndpoint("/users/swagger/v1/swagger.json", "User Service API");
    options.SwaggerEndpoint("/it-provision/swagger/v1/swagger.json", "IT Provision Service API");
    options.SwaggerEndpoint("/notification/swagger/v1/swagger.json", "Notification Service API");
    options.RoutePrefix = "swagger";
});

// Correlation ID: if the caller does not supply X-Correlation-ID, generate a
// new one and forward it on the proxied request so every downstream service
// receives the same ID for the full request chain.
app.Use(async (context, next) =>
{
    const string correlationIdHeader = "X-Correlation-ID";

    var correlationId =
        context.Request.Headers[correlationIdHeader].FirstOrDefault()
        ?? Guid.NewGuid().ToString();

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
