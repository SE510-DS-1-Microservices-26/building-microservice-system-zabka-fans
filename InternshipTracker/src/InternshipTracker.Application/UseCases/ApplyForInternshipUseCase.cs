using InternshipTracker.Application.DTOs;
using InternshipTracker.Application.Enums;
using InternshipTracker.Application.Interfaces;
using InternshipTracker.Application.Interfaces.Repositories;
using InternshipTracker.Domain.Exceptions;
using InternshipTracker.Domain.Factories;

namespace InternshipTracker.Application.UseCases;

public class ApplyForInternshipUseCase : IUseCase<ApplyForInternshipRequest, ApplyForInternshipResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IInternshipRepository _internshipRepository;
    private readonly IInternshipApplicationRepository _applicationRepository;
    private readonly InternshipApplicationFactory _domainFactory;

    public ApplyForInternshipUseCase(
        IUserRepository userRepository,
        IInternshipRepository internshipRepository,
        IInternshipApplicationRepository applicationRepository,
        InternshipApplicationFactory domainFactory)
    {
        _userRepository = userRepository;
        _internshipRepository = internshipRepository;
        _applicationRepository = applicationRepository;
        _domainFactory = domainFactory;
    }

    public async Task<Result<ApplyForInternshipResponse>> ExecuteAsync(
        ApplyForInternshipRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var candidate = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            
            if (candidate == null)
                return Result<ApplyForInternshipResponse>.Failure(
                    new Error("User.NotFound", "Candidate not found.", ErrorType.NotFound));

            var internship = await _internshipRepository.GetByIdAsync(request.InternshipId, cancellationToken);
            
            if (internship == null)
                return Result<ApplyForInternshipResponse>.Failure(
                    new Error("Internship.NotFound", "Internship not found.", ErrorType.NotFound));

            // Domain logic
            var application = await _domainFactory.CreateAsync(candidate, internship);

            await _applicationRepository.AddAsync(application, cancellationToken);
            await _applicationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            var response = new ApplyForInternshipResponse(application.Id, application.Status);
            return Result<ApplyForInternshipResponse>.Success(response);
        }
        catch (UnderqualifiedException ex)
        {
            // Maps to a 400 Bad Request
            return Result<ApplyForInternshipResponse>.Failure(
                new Error("Application.Underqualified", ex.Message, ErrorType.Validation));
        }
        catch (DuplicateApplicationException ex)
        {
            // Maps to a 409 Conflict
            return Result<ApplyForInternshipResponse>.Failure(
                new Error("Application.Duplicate", ex.Message, ErrorType.Conflict));
        }
        catch (Exception ex)
        {
            // Catch-all for unexpected database drops, network failures, etc. (Maps to 500)
            return Result<ApplyForInternshipResponse>.Failure(
                new Error("System.Failure", "An unexpected error occurred.", ErrorType.Failure));
        }
    }
}