using InternshipTracker.Application.DTOs;
using InternshipTracker.Application.Enums;

namespace InternshipTracker.UI.Helpers;

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