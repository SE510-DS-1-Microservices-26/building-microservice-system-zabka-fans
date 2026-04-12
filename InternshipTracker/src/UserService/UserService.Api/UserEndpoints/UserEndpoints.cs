using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Requests;
using UserService.Application.DTOs.Responses;
using UserService.Application.Enums;
using UserService.Application.Interfaces;

namespace UserService.Api.UserEndpoints;

public static class UserEndpoints
{
    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        var userGroup = app.MapGroup("/users").WithTags("Users");
        userGroup.MapPost("/", CreateUser);
        userGroup.MapDelete("/{id:guid}", DeleteUser);
        return app;
    }


    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        [FromServices] IUseCase<CreateUserRequest, UserResponse> useCase)
    {
        var result = await useCase.ExecuteAsync(request);
        return result.IsSuccess
            ? Results.Created($"/users/{result.Value!.Id}", result.Value)
            : ResultMapper.MapError(result.Error!);
    }

    private static async Task<IResult> DeleteUser(
        Guid id,
        [FromServices] IUseCase<DeleteUserRequest> useCase)
    {
        var result = await useCase.ExecuteAsync(new DeleteUserRequest(id));
        return result.IsSuccess
            ? Results.NoContent()
            : ResultMapper.MapError(result.Error!);
    }
}

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