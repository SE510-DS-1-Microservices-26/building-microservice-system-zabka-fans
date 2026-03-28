using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Enums;
using CoreService.Application.Factories;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;

namespace CoreService.Application.UseCases;

public class ApplyForInternshipUseCase : IUseCase<ApplyForInternshipRequest, ApplyForInternshipResponse>
{
    private readonly IInternshipApplicationRepository _applicationRepository;
    private readonly InternshipApplicationFactory _domainFactory;
    private readonly IInternshipRepository _internshipRepository;
    private readonly IUserCoreRepository _userCoreRepository;

    public ApplyForInternshipUseCase(
        IInternshipRepository internshipRepository,
        IInternshipApplicationRepository applicationRepository,
        InternshipApplicationFactory domainFactory,
        IUserCoreRepository userCoreRepository)
    {
        _internshipRepository = internshipRepository;
        _applicationRepository = applicationRepository;
        _domainFactory = domainFactory;
        _userCoreRepository = userCoreRepository;
    }

    public async Task<Result<ApplyForInternshipResponse>> ExecuteAsync(
        ApplyForInternshipRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userCoreRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<ApplyForInternshipResponse>.Failure(new Error("User.NotSynced",
                $"User with ID {request.UserId} has not been synced to the core database yet.",
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
            await _domainFactory.CreateAsync(request.UserId, user.Level, internship, user, cancellationToken);

        await _applicationRepository.AddAsync(application, cancellationToken);
        await _applicationRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ApplyForInternshipResponse>.Success(
            new ApplyForInternshipResponse(application.Id, application.Status));
    }
}