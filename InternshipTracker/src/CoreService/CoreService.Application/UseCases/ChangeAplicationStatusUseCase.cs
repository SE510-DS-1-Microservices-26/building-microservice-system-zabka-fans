using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.Enums;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Domain.Enums;
using CoreService.Domain.Exceptions;
using CoreService.Domain.Interfaces;

namespace CoreService.Application.UseCases;

public class ChangeApplicationStatusUseCase : IUseCase<ChangeApplicationStatusRequest>
{
    private readonly IInternshipApplicationRepository _appRepository;
    private readonly IInternshipCapacityChecker _capacityChecker;

    public ChangeApplicationStatusUseCase(IInternshipApplicationRepository appRepository,
        IInternshipCapacityChecker capacityChecker)
    {
        _appRepository = appRepository;
        _capacityChecker = capacityChecker; }

    public async Task<Result> ExecuteAsync(
        ChangeApplicationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await _appRepository.GetWithDetailsAsync(request.ApplicationId, cancellationToken);

            if (application == null)
                return Result.Failure(new Error(
                    "Application.NotFound",
                    $"Application with ID {request.ApplicationId} was not found.",
                    ErrorType.NotFound));

            switch (request.NewStatus)
            {
                case ApplicationStatus.Accepted:
                    await application.Internship.OfferPositionAsync(application, _capacityChecker);
                    break;

                case ApplicationStatus.Enrolled:
                    if (application.Status != ApplicationStatus.Accepted)
                    {
                        throw new InvalidApplicationStateException(
                            $"Cannot enroll application in status {application.Status}. Must be Accepted.");
                    }
                    
                    var isAlreadyEnrolled = await _appRepository.HasStatusAsync(application.CandidateId, ApplicationStatus.Enrolled, cancellationToken);
                    if (isAlreadyEnrolled)                    {
                        throw new AlreadyEnrolledException(
                            $"Candidate with ID {application.CandidateId} is already enrolled in another internship.");
                    }
                    application.MarkAsEnrolled();
                    break;

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

            return Result.Success();
        }
        catch (CapacityExceededException ex)
        {
            return Result.Failure(new Error("Internship.CapacityExceeded", ex.Message, ErrorType.Conflict));
        }
        catch (AlreadyEnrolledException ex)
        {
            return Result.Failure(new Error("Candidate.AlreadyEnrolled", ex.Message, ErrorType.Conflict));
        }
        catch (InvalidApplicationStateException ex)
        {
            return Result.Failure(new Error("Application.InvalidState", ex.Message, ErrorType.Validation));
        }
        catch (ApplicationMismatchException ex)
        {
            return Result.Failure(new Error("Application.Mismatch", ex.Message, ErrorType.Validation));
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error("System.Failure",
                "An unexpected error occurred processing the status change.", ErrorType.Failure));
        }
    }
}