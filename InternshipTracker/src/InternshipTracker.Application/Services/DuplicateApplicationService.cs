using InternshipTracker.Application.Interfaces.Repositories;
using InternshipTracker.Domain.Interfaces;

namespace InternshipTracker.Application.Services;

public class DuplicateApplicationChecker : IDuplicateApplicationChecker
{
    private readonly IInternshipApplicationRepository _repository;

    public DuplicateApplicationChecker(IInternshipApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> HasAppliedAsync(
        Guid candidateId,
        Guid internshipId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.ExistsAsync(candidateId, internshipId, cancellationToken);
    }
}