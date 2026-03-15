using InternshipTracker.Domain.Entities;
using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Application.Interfaces.Repositories;

public interface IInternshipApplicationRepository : IRepository<InternshipApplication>
{
    Task<InternshipApplication?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    
    // For the Capacity Checker
    Task<int> CountByInternshipAndStatusesAsync(Guid internshipId, ApplicationStatus[] statuses, CancellationToken cancellationToken = default);
    
    // For the Duplicate Checker
    Task<bool> ExistsAsync(Guid candidateId, Guid internshipId, CancellationToken cancellationToken = default);
    
    // For the Enrollment Checker
    Task<bool> HasStatusAsync(Guid candidateId, ApplicationStatus status, CancellationToken cancellationToken = default);
}