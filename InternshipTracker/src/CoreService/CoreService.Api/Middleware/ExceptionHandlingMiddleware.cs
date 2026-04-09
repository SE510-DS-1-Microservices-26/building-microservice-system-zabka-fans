using System.Text.Json;
using CoreService.Application.Exceptions;
using CoreService.Domain.Exceptions;

namespace CoreService.Api.Middleware;

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
        catch (ApplicationValidationException ex)
        {
            _logger.LogWarning(ex, "Application validation error: {ErrorCode} — {Message}", ex.ErrorCode, ex.Message);
            await WriteProblemResponse(context, StatusCodes.Status400BadRequest, ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated: {ErrorCode} — {Message}", ex.ErrorCode, ex.Message);
            await WriteProblemResponse(context, MapDomainStatusCode(ex.ErrorCode), ex.ErrorCode, ex.Message);
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
        _ when errorCode.Contains("NotFound") => StatusCodes.Status404NotFound,
        _ when errorCode.Contains("Duplicate") => StatusCodes.Status409Conflict,
        _ when errorCode.Contains("AlreadyEnrolled") => StatusCodes.Status409Conflict,
        _ when errorCode.Contains("CapacityExceeded") => StatusCodes.Status409Conflict,
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
