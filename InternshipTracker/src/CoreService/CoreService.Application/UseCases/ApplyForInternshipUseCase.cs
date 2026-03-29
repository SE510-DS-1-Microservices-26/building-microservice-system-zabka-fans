using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Enums;
using CoreService.Application.Factories;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace CoreService.Application.UseCases;

public class ApplyForInternshipUseCase : IUseCase<ApplyForInternshipRequest, ApplyForInternshipResponse>
{
    private readonly IInternshipApplicationRepository _applicationRepository;
    private readonly InternshipApplicationFactory _domainFactory;
    private readonly IInternshipRepository _internshipRepository;
    private readonly IUserCoreRepository _userCoreRepository;
    private readonly ILogger<ApplyForInternshipUseCase> _logger;

    public ApplyForInternshipUseCase(
        IInternshipRepository internshipRepository,
        IInternshipApplicationRepository applicationRepository,
        InternshipApplicationFactory domainFactory,
        IUserCoreRepository userCoreRepository,
        ILogger<ApplyForInternshipUseCase> logger)
    {
        _internshipRepository = internshipRepository;
        _applicationRepository = applicationRepository;
        _domainFactory = domainFactory;
        _userCoreRepository = userCoreRepository;
        _logger = logger;
    }

    public async Task<Result<ApplyForInternshipResponse>> ExecuteAsync(
        ApplyForInternshipRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Applying for internship {InternshipId} by user {UserId}",
            request.InternshipId, request.UserId);

        var user = await _userCoreRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not synced to core database", request.UserId);
            return Result<ApplyForInternshipResponse>.Failure(new Error("User.NotSynced",
                $"User with ID {request.UserId} has not been synced to the core database yet.",
                ErrorType.Validation));
        }

        var internship = await _internshipRepository.GetByIdAsync(request.InternshipId, cancellationToken);
        if (internship == null)
        {
            _logger.LogWarning("Internship {InternshipId} not found", request.InternshipId);
            return Result<ApplyForInternshipResponse>.Failure(new Error("Internship.NotFound",
                $"Internship with ID {request.InternshipId} was not found.",
                ErrorType.NotFound));
        }

        var application =
            await _domainFactory.CreateAsync(request.UserId, user.Level, internship, user, cancellationToken);

        await _applicationRepository.AddAsync(application, cancellationToken);
        await _applicationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Application {ApplicationId} created for user {UserId} on internship {InternshipId}",
            application.Id, request.UserId, request.InternshipId);

        return Result<ApplyForInternshipResponse>.Success(
            new ApplyForInternshipResponse(application.Id, application.Status));
    }
}