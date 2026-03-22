using InternshipTracker.Application.DTOs.Requests;
using InternshipTracker.Application.DTOs.Responses;
using InternshipTracker.Application.Interfaces;
using InternshipTracker.UI.Helpers;

namespace InternshipTracker.UI.Endpoints;

public static class ApplicationEndpoints
{
    public static WebApplication MapApplicationEndpoints(this WebApplication app)
    {
        var applicationGroup = app.MapGroup("/applications").WithTags("Internships");
        applicationGroup.MapPost("/", ApplyForApplication);
        applicationGroup.MapPatch("/{id:guid}/status", ChangeApplicationStatus);
        return app;
    }
    
    private static async Task<IResult> ApplyForApplication(
        ApplyForInternshipRequest request,
        IUseCase<ApplyForInternshipRequest, ApplyForInternshipResponse> useCase)
    {
        var result = await useCase.ExecuteAsync(request);
        return result.IsSuccess
            ? Results.Created($"/applications/{result.Value!.ApplicationId}", result.Value)
            : ResultMapper.MapError(result.Error!);
    }
    
    private static async Task<IResult> ChangeApplicationStatus(
        Guid id,                                               
        ChangeApplicationStatusRequest request,
        IUseCase<ChangeApplicationStatusRequest> useCase)                                                                      
    {                                                    
        var result = await useCase.ExecuteAsync(request with { ApplicationId = id });                                          
        return result.IsSuccess                                                      
            ? Results.NoContent()                                                                                              
            : ResultMapper.MapError(result.Error!);                                                                            
    }
}