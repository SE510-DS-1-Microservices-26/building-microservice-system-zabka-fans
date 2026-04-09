using CoreService.Api.Helpers;
using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CoreService.Api.CoreEndpoints;

public static class UserEndpoints
{
    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        var userGroup = app.MapGroup("/users").WithTags("Users");
        userGroup.MapGet("/", GetAllUsers);
        userGroup.MapGet("/{id:guid}", GetUser);
        return app;
    }

    private static async Task<IResult> GetAllUsers(
        [AsParameters] GetAllUsersRequest request,
        [FromServices] IUseCase<GetAllUsersRequest, PagedResult<UserCoreResponse>> useCase)
    {
        var result = await useCase.ExecuteAsync(request);
        return result.IsSuccess
            ? Results.Ok(result.Value!)
            : ResultMapper.MapError(result.Error!);
    }

    private static async Task<IResult> GetUser(
        Guid id,
        [FromServices] IUseCase<GetUserRequest, UserCoreResponse> useCase)
    {
        var result = await useCase.ExecuteAsync(new GetUserRequest(id));
        return result.IsSuccess
            ? Results.Ok(result.Value!)
            : ResultMapper.MapError(result.Error!);
    }
}

