using InternshipTracker.Application.Interfaces.Repositories;
using InternshipTracker.Domain.Enums;
using InternshipTracker.Domain.Interfaces;

namespace InternshipTracker.Application.Services;

public class UserEnrollmentChecker : IUserEnrollmentChecker
{
    private readonly IInternshipApplicationRepository _repository;

    public UserEnrollmentChecker(IInternshipApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> IsAlreadyEnrolledAsync(
        Guid candidateId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.HasStatusAsync(candidateId, ApplicationStatus.Enrolled, cancellationToken);
    }
}