var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var app = builder.Build();

app.Use(async (context, next) =>
{
    const string correlationIdHeader = "X-Correlation-ID";
    if (!context.Request.Headers.ContainsKey(correlationIdHeader)) {
        context.Request.Headers[correlationIdHeader] = Guid.NewGuid().ToString();
    }
    
    var correlationId = context.Request.Headers[correlationIdHeader].ToString();
    context.Response.Headers[correlationIdHeader] = correlationId;

    await next();
});

app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok(new {status = "Healthy"}));

app.Run();
