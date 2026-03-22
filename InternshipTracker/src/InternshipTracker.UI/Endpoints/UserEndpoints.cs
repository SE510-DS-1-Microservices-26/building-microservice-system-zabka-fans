using InternshipTracker.Application.DTOs.Requests;
using InternshipTracker.Application.DTOs.Responses;
using InternshipTracker.Application.Interfaces;
using InternshipTracker.UI.Helpers;

namespace InternshipTracker.UI.Endpoints;

public static class UserEndpoints
{
    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        var userGroup = app.MapGroup("/users").WithTags("Users");
        userGroup.MapGet("/{id:guid}", GetUser);
        userGroup.MapPost("/", CreateUser);
        return app;
    }

    private static async Task<IResult> GetUser(Guid id, IUseCase<GetUserRequest, GetUserResponse> useCase)
    {
        var result = await useCase.ExecuteAsync(new GetUserRequest(id));
        return result.IsSuccess
            ? Results.Ok(result.Value!)
            : ResultMapper.MapError(result.Error!);
    }

    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        IUseCase<CreateUserRequest, CreateUserResponse> useCase)
    {
        var result = await useCase.ExecuteAsync(request);
        return result.IsSuccess
            ? Results.Created($"/users/{result.Value!.Id}", result.Value)
            : ResultMapper.MapError(result.Error!);
    }
}