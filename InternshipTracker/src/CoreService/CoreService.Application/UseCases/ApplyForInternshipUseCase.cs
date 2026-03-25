using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Enums;
using CoreService.Application.Factories;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Domain.Exceptions;

namespace CoreService.Application.UseCases;

public class ApplyForInternshipUseCase : IUseCase<ApplyForInternshipRequest, ApplyForInternshipResponse>
{
    private readonly IInternshipApplicationRepository _applicationRepository;
    private readonly InternshipApplicationFactory _domainFactory;
    private readonly IInternshipRepository _internshipRepository;
    private readonly IUserValidationService _userValidationService;

    public ApplyForInternshipUseCase(
        IInternshipRepository internshipRepository,
        IInternshipApplicationRepository applicationRepository,
        InternshipApplicationFactory domainFactory,
        IUserValidationService userValidationService)
    {
        _internshipRepository = internshipRepository;
        _applicationRepository = applicationRepository;
        _domainFactory = domainFactory;
        _userValidationService = userValidationService;
    }

    public async Task<Result<ApplyForInternshipResponse>> ExecuteAsync(
        ApplyForInternshipRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userValidationService.GetUserInfoAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<ApplyForInternshipResponse>.Failure(new Error("User.NotFound",
                    $"User with ID {request.UserId} was not found.",
                    ErrorType.Validation));
            }

            var internship = await _internshipRepository.GetByIdAsync(request.InternshipId, cancellationToken);
            if (internship == null)
            {
                return Result<ApplyForInternshipResponse>.Failure(new Error("Internship.NotFound",
                    $"Internship with ID {request.InternshipId} was not found.",
                    ErrorType.NotFound));
            }

            var application =
                await _domainFactory.CreateAsync(request.UserId, user.Level, internship, cancellationToken);

            await _applicationRepository.AddAsync(application, cancellationToken);
            await _applicationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ApplyForInternshipResponse>.Success(
                new ApplyForInternshipResponse(application.Id, application.Status));
        }
        catch (UnderqualifiedException ex)
        {
            return Result<ApplyForInternshipResponse>.Failure(
                new Error("Application.Underqualified", ex.Message, ErrorType.Validation));
        }
        catch (DuplicateApplicationException ex)
        {
            return Result<ApplyForInternshipResponse>.Failure(
                new Error("Application.Duplicate", ex.Message, ErrorType.Conflict));
        }
        catch (HttpRequestException ex)
        {
            return Result<ApplyForInternshipResponse>.Failure(
                new Error("UserService.Unavailable", "Failed to validate user information due to a service error.", ErrorType.ServiceUnavailable));
        }
        catch (Exception ex)
        {
            return Result<ApplyForInternshipResponse>.Failure(
                new Error("System.Failure", "An unexpected error occurred.", ErrorType.Failure));
        }
    }
}