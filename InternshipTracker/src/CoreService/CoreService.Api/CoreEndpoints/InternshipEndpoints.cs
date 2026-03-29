using CoreService.Api.Helpers;
using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CoreService.Api.CoreEndpoints;

public static class InternshipEndpoints
{
    public static WebApplication MapInternshipEndpoints(this WebApplication app)
    {
        var internshipGroup = app.MapGroup("/internships").WithTags("Internships");
        internshipGroup.MapGet("/{id:guid}", GetInternship);
        internshipGroup.MapGet("/", GetAllInternships);
        internshipGroup.MapPost("/", CreateInternship);
        return app;
    }

    private static async Task<IResult> GetInternship(
        Guid id,
        [FromServices] IUseCase<GetInternshipRequest, InternshipResponse> useCase)
    {
        var result = await useCase.ExecuteAsync(new GetInternshipRequest(id));
        return result.IsSuccess
            ? Results.Ok(result.Value!)
            : ResultMapper.MapError(result.Error!);
    }

    private static async Task<IResult> GetAllInternships(
        [AsParameters] GetAllInternshipsRequest request,
        [FromServices] IUseCase<GetAllInternshipsRequest, PagedResult<InternshipResponse>> useCase)
    {
        var result = await useCase.ExecuteAsync(request);
        return result.IsSuccess
            ? Results.Ok(result.Value!)
            : ResultMapper.MapError(result.Error!);
    }

    private static async Task<IResult> CreateInternship(
        CreateInternshipRequest request,
        [FromServices] IUseCase<CreateInternshipRequest, InternshipResponse> useCase)
    {
        var result = await useCase.ExecuteAsync(request);
        return result.IsSuccess
            ? Results.Created($"/internships/{result.Value!.Id}", result.Value)
            : ResultMapper.MapError(result.Error!);
    }
}