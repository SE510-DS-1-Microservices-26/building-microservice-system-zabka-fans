using CoreService.Application.DTOs;
using CoreService.Application.Enums;

namespace CoreService.Api.Helpers;

public static class ResultMapper
{
    public static IResult MapError(Error error)
    {
        return error.Type switch
        {
            ErrorType.NotFound => Results.NotFound(error),
            ErrorType.Validation => Results.BadRequest(error),
            ErrorType.Conflict => Results.Conflict(error),
            _ => Results.Problem(error.Description)
        };
    }
}