using InternshipTracker.Domain.Entities;
using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.Interfaces.Repositories;

public interface IInternshipApplicationRepository : IRepository<InternshipApplication>
{
    Task<InternshipApplication?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountReservedSpotsAsync(Guid internshipId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid candidateId, Guid internshipId, CancellationToken cancellationToken = default);

    Task<bool> HasStatusAsync(Guid candidateId, ApplicationStatus status,
        CancellationToken cancellationToken = default);
}