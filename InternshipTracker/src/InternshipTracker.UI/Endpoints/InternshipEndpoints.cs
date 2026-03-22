using InternshipTracker.Application.DTOs.Requests;
using InternshipTracker.Application.DTOs.Responses;
using InternshipTracker.Application.Interfaces;
using InternshipTracker.UI.Helpers;

namespace InternshipTracker.UI.Endpoints;

public static class InternshipEndpoints
{
    public static WebApplication MapInternshipEndpoints(this WebApplication app)
    {
        var internshipGroup = app.MapGroup("/internships").WithTags("Internships");
        internshipGroup.MapGet("/{id:guid}", GetInternship);
        internshipGroup.MapPost("/", CreateInternship);
        return app;
    }

    private static async Task<IResult> GetInternship(
        Guid id,
        IUseCase<GetInternshipRequest, InternshipResponse> useCase)
    {
        var result = await useCase.ExecuteAsync(new GetInternshipRequest(id));
        return result.IsSuccess
            ? Results.Ok(result.Value!)
            : ResultMapper.MapError(result.Error!);
    }

    private static async Task<IResult> CreateInternship(
        CreateInternshipRequest request,
        IUseCase<CreateInternshipRequest, InternshipResponse> useCase)
    {
        var result = await useCase.ExecuteAsync(request);
        return result.IsSuccess
            ? Results.Created($"/internships/{result.Value!.Id}", result.Value)
            : ResultMapper.MapError(result.Error!);
    }
}