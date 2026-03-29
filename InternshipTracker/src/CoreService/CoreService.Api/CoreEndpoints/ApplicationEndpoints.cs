using CoreService.Api.Helpers;
using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CoreService.Api.CoreEndpoints;

public static class ApplicationEndpoints
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        var applicationGroup = app.MapGroup("/applications").WithTags("Internships");
        applicationGroup.MapGet("/", GetAllApplications);
        applicationGroup.MapPost("/", ApplyForApplication);
        applicationGroup.MapPatch("/{id:guid}/status", ChangeApplicationStatus);
        return app;
    }

    private static async Task<IResult> GetAllApplications(
        [AsParameters] GetAllApplicationsRequest request,
        [FromServices] IUseCase<GetAllApplicationsRequest, PagedResult<ApplicationResponse>> useCase)
    {
        var result = await useCase.ExecuteAsync(request);
        return result.IsSuccess
            ? Results.Ok(result.Value!)
            : ResultMapper.MapError(result.Error!);
    }

    private static async Task<IResult> ApplyForApplication(
        ApplyForInternshipRequest request,
        [FromServices] IUseCase<ApplyForInternshipRequest, ApplyForInternshipResponse> useCase)
    {
        var result = await useCase.ExecuteAsync(request);
        return result.IsSuccess
            ? Results.Created($"/applications/{result.Value!.ApplicationId}", result.Value)
            : ResultMapper.MapError(result.Error!);
    }

    private static async Task<IResult> ChangeApplicationStatus(
        Guid id,
        ChangeApplicationStatusRequest request,
        [FromServices] IUseCase<ChangeApplicationStatusRequest> useCase)
    {
        var result = await useCase.ExecuteAsync(request with { ApplicationId = id });
        return result.IsSuccess
            ? Results.NoContent()
            : ResultMapper.MapError(result.Error!);
    }
}