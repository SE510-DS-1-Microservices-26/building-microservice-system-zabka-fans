using CoreService.Application.Interfaces.Repositories;
using CoreService.Domain.Interfaces;

namespace CoreService.Application.Services;

public class InternshipCapacityChecker : IInternshipCapacityChecker
{
    private readonly IInternshipApplicationRepository _repository;

    public InternshipCapacityChecker(IInternshipApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> CountReservedSpotsAsync(Guid internshipId, CancellationToken cancellationToken = default)
    {
        return await _repository.CountReservedSpotsAsync(
            internshipId,
            cancellationToken);
    }
}