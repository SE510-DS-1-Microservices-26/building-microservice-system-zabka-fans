using CoreService.Domain.Entities;

namespace CoreService.Application.Interfaces.Repositories;

public interface IInternshipRepository
{
    IUnitOfWork UnitOfWork { get; }
    Task AddAsync(Internship internship, CancellationToken cancellationToken = default);
    Task<Internship?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Internship internship, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
};