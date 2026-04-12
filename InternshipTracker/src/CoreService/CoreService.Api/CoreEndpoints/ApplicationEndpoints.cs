using CoreService.Api.Helpers;
using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Interfaces;
using CoreService.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CoreService.Api.CoreEndpoints;

public static class ApplicationEndpoints
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        var applicationGroup = app.MapGroup("/applications").WithTags("Applications");
        applicationGroup.MapGet("/", GetAllApplications);
        applicationGroup.MapGet("/{id:guid}", GetApplicationById);
        applicationGroup.MapPost("/", ApplyForApplication);
        applicationGroup.MapPost("/{id:guid}/accept", AcceptApplication);
        applicationGroup.MapPost("/{id:guid}/enroll", EnrollApplication);
        return app;
    }

    private static async Task<IResult> GetApplicationById(
        Guid id,
        [FromServices] IUseCase<GetApplicationRequest, ApplicationResponse> useCase)
    {
        var result = await useCase.ExecuteAsync(new GetApplicationRequest(id));
        return result.IsSuccess
            ? Results.Ok(result.Value!)
            : ResultMapper.MapError(result.Error!);
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

    private static async Task<IResult> AcceptApplication(
        Guid id,
        AcceptApplicationRequest request,
        [FromServices] IUseCase<ChangeApplicationStatusRequest> useCase)
    {
        var result = await useCase.ExecuteAsync(
            new ChangeApplicationStatusRequest(id, ApplicationStatus.Accepted));
        return result.IsSuccess
            ? Results.Ok()
            : ResultMapper.MapError(result.Error!);
    }

    private static async Task<IResult> EnrollApplication(
        Guid id,
        EnrollApplicationRequest request,
        [FromServices] IUseCase<ChangeApplicationStatusRequest> useCase)
    {
        var result = await useCase.ExecuteAsync(
            new ChangeApplicationStatusRequest(id, ApplicationStatus.Enrolled));
        return result.IsSuccess
            ? Results.Accepted()
            : ResultMapper.MapError(result.Error!);
    }
}