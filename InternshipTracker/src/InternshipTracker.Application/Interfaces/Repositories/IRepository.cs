using InternshipTracker.Domain.Entities;

namespace InternshipTracker.Application.Interfaces.Repositories;

public interface IRepository<T> : IReadOnlyRepository<T> where T : IEntity
{
    IUnitOfWork UnitOfWork { get; }
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}