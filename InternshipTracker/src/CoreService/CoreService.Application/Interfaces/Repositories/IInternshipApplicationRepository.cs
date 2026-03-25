using CoreService.Domain.Entities;
using CoreService.Domain.Enums;

namespace CoreService.Application.Interfaces.Repositories;

public interface IInternshipApplicationRepository
{
    IUnitOfWork UnitOfWork { get; }
    Task AddAsync(InternshipApplication application, CancellationToken cancellationToken = default);
    Task<InternshipApplication?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountReservedSpotsAsync(Guid internshipId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid candidateId, Guid internshipId, CancellationToken cancellationToken = default);

    Task<bool> HasStatusAsync(Guid candidateId, ApplicationStatus status,
        CancellationToken cancellationToken = default);
}