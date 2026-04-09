using System.Text.Json;
using UserService.Domain.Exceptions;

namespace UserService.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated: {ErrorCode} — {Message}", ex.ErrorCode, ex.Message);
            await WriteProblemResponse(context, MapDomainStatusCode(ex.ErrorCode), ex.ErrorCode, ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemResponse(context, StatusCodes.Status400BadRequest, "Validation.Failed", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemResponse(context, StatusCodes.Status500InternalServerError,
                "System.Failure", "An unexpected error occurred.");
        }
    }

    private static int MapDomainStatusCode(string errorCode) => errorCode switch
    {
        "User.InvalidEmail" => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status400BadRequest
    };

    private static async Task WriteProblemResponse(
        HttpContext context, int statusCode, string code, string description)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var body = new { code, description, statusCode };
        await context.Response.WriteAsync(JsonSerializer.Serialize(body,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}

