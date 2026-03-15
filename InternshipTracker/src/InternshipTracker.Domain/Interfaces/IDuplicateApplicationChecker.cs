namespace InternshipTracker.Domain.Interfaces;

public interface IDuplicateApplicationChecker
{
    Task<bool> HasAppliedAsync(Guid candidateId, Guid internshipId, CancellationToken cancellationToken = default);
}