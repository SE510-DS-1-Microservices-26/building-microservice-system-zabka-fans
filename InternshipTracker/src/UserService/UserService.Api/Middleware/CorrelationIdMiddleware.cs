namespace UserService.Api.Middleware;
public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext context)
    {
        // Use the header forwarded by an upstream service or gateway;
        // otherwise mint a new correlation ID for this request.
        var correlationId =
            context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        // Make the ID available to downstream code within the same request pipeline.
        context.Items[CorrelationIdHeader] = correlationId;
        // Attach to the response before headers are flushed.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });
        await _next(context);
    }
}
