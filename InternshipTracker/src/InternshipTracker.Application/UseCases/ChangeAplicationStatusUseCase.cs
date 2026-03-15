using InternshipTracker.Application.DTOs;
using InternshipTracker.Application.Enums;
using InternshipTracker.Application.Interfaces;
using InternshipTracker.Application.Interfaces.Repositories;
using InternshipTracker.Domain.Enums;
using InternshipTracker.Domain.Exceptions;
using InternshipTracker.Domain.Interfaces;

namespace InternshipTracker.Application.UseCases;

public class ChangeApplicationStatusUseCase : IUseCase<ChangeApplicationStatusRequest>
{
    private readonly IInternshipApplicationRepository _appRepository;
    private readonly IInternshipCapacityChecker _capacityChecker;
    private readonly IUserEnrollmentChecker _userEnrollmentChecker;

    public ChangeApplicationStatusUseCase(IInternshipApplicationRepository appRepository,
        IInternshipCapacityChecker capacityChecker, IUserEnrollmentChecker userEnrollmentChecker)
    {
        _appRepository = appRepository;
        _capacityChecker = capacityChecker;
        _userEnrollmentChecker = userEnrollmentChecker;
    }

    public async Task<Result> ExecuteAsync(
        ChangeApplicationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var application = await _appRepository.GetWithDetailsAsync(request.ApplicationId, cancellationToken);

            if (application == null)
            {
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
                    // Can throw AlreadyEnrolledException, InvalidApplicationStateException, or Mismatch
                    await application.Candidate.EnrollAsync(application, _userEnrollmentChecker);
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
            // maps to a 409 Conflict
            return Result.Failure(new Error("Internship.CapacityExceeded", ex.Message, ErrorType.Conflict));
        }
        catch (AlreadyEnrolledException ex)
        {
            // maps to a 409 Conflict
            return Result.Failure(new Error("Candidate.AlreadyEnrolled", ex.Message, ErrorType.Conflict));
        }
        catch (InvalidApplicationStateException ex)
        {
            // maps to a 400 Bad Request
            return Result.Failure(new Error("Application.InvalidState", ex.Message, ErrorType.Validation));
        }
        catch (ApplicationMismatchException ex)
        {
            // maps to a 400 Bad Request
            return Result.Failure(new Error("Application.Mismatch", ex.Message, ErrorType.Validation));
        }
        catch (Exception ex)
        {
            // maps to a 500 
            return Result.Failure(new Error("System.Failure",
                "An unexpected error occurred processing the status change.", ErrorType.Failure));
        }
    }
}