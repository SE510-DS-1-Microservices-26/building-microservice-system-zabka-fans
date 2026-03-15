namespace InternshipTracker.Domain.Interfaces;

public interface IInternshipCapacityChecker
{
    Task<int> CountReservedSpotsAsync(Guid internshipId, CancellationToken cancellationToken = default);
}