using InternshipTracker.Domain.Entities;

namespace InternshipTracker.Application.Interfaces.Repositories;

public interface IReadOnlyRepository<TEntity> where TEntity : IEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
}