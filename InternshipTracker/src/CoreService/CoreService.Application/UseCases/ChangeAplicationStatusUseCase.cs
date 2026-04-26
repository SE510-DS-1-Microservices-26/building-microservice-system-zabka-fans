using Contracts.Events;
using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.Enums;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Domain.Enums;
using CoreService.Domain.Exceptions;
using CoreService.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoreService.Application.UseCases;

public class ChangeApplicationStatusUseCase : IUseCase<ChangeApplicationStatusRequest>
{
    private readonly IInternshipApplicationRepository _appRepository;
    private readonly IInternshipCapacityChecker _capacityChecker;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ChangeApplicationStatusUseCase> _logger;

    public ChangeApplicationStatusUseCase(
        IInternshipApplicationRepository appRepository,
        IInternshipCapacityChecker capacityChecker,
        IPublishEndpoint publishEndpoint,
        ILogger<ChangeApplicationStatusUseCase> logger)
    {
        _appRepository = appRepository;
        _capacityChecker = capacityChecker;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<Result> ExecuteAsync(
        ChangeApplicationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Changing application {ApplicationId} status to {NewStatus}",
            request.ApplicationId, request.NewStatus);

        var application = await _appRepository.GetWithDetailsAsync(request.ApplicationId, cancellationToken);

        if (application == null)
        {
            _logger.LogWarning("Application {ApplicationId} not found", request.ApplicationId);
            return Result.Failure(new Error(
                "Application.NotFound",
                $"Application with ID {request.ApplicationId} was not found.",
                ErrorType.NotFound));
        }

        switch (request.NewStatus)
        {
            case ApplicationStatus.Accepted:
                await application.Internship.OfferPositionAsync(application, _capacityChecker);
                break;

            case ApplicationStatus.Enrolled:
                if (application.Status != ApplicationStatus.Accepted)
                {
                    throw new InvalidApplicationStateException(
                        $"Cannot begin enrollment from status {application.Status}. Must be Accepted.");
                }

                var isAlreadyEnrolled = await _appRepository.HasStatusAsync(
                    application.CandidateId, ApplicationStatus.Enrolled, cancellationToken);
                if (isAlreadyEnrolled)
                {
                    throw new AlreadyEnrolledException(
                        $"Candidate with ID {application.CandidateId} is already enrolled in another internship.");
                }

                application.MarkAsEnrolling();
                await _appRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

                await _publishEndpoint.Publish(new OnboardingStartedEvent(
                    application.Id,
                    application.CandidateId,
                    application.Candidate.Name,
                    application.Candidate.Email), cancellationToken);

                _logger.LogInformation(
                    "Application {ApplicationId} set to Enrolling — onboarding saga started",
                    request.ApplicationId);

                return Result.Success();

            case ApplicationStatus.Rejected:
                application.MarkAsRejected();
                break;

            case ApplicationStatus.Pending:
                return Result.Failure(new Error(
                    "Application.InvalidTransition",
                    "Cannot manually revert an application to Pending.",
                    ErrorType.Validation));

            default:
                return Result.Failure(new Error(
                    "Application.UnknownStatus",
                    "Invalid status transition requested.",
                    ErrorType.Validation));
        }

        await _appRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Application {ApplicationId} status changed to {NewStatus}",
            request.ApplicationId, request.NewStatus);

        return Result.Success();
    }
}