using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Enums;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace CoreService.Application.UseCases;

public class GetApplicationByIdUseCase : IUseCase<GetApplicationRequest, ApplicationResponse>
{
    private readonly IInternshipApplicationRepository _applicationRepository;
    private readonly ILogger<GetApplicationByIdUseCase> _logger;

    public GetApplicationByIdUseCase(
        IInternshipApplicationRepository applicationRepository,
        ILogger<GetApplicationByIdUseCase> logger)
    {
        _applicationRepository = applicationRepository;
        _logger = logger;
    }

    public async Task<Result<ApplicationResponse>> ExecuteAsync(
        GetApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationRepository.GetWithDetailsAsync(request.ApplicationId, cancellationToken);

        if (application == null)
        {
            _logger.LogWarning("Application {ApplicationId} not found", request.ApplicationId);
            return Result<ApplicationResponse>.Failure(new Error(
                "Application.NotFound",
                $"Application with ID {request.ApplicationId} was not found.",
                ErrorType.NotFound));
        }

        var response = new ApplicationResponse(
            application.Id,
            application.Candidate.Id,
            application.Candidate.Name,
            application.Candidate.Level,
            application.Internship.Id,
            application.Internship.Title,
            application.Status);

        return Result<ApplicationResponse>.Success(response);
    }
}
