namespace InternshipTracker.Domain.Interfaces;

public interface IUserEnrollmentChecker
{
    public Task<bool> IsAlreadyEnrolledAsync(Guid candidateId, CancellationToken cancellationToken = default);
}