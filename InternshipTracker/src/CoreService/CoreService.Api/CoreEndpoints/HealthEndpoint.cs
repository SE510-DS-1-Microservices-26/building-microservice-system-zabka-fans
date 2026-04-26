namespace CoreService.Api.CoreEndpoints;

public static class HealthEndpoint
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/health").WithTags("Health");
        group.MapGet("/", () => Results.Ok(new { Status = "Core is healthy" }));
        return app;
    }
}