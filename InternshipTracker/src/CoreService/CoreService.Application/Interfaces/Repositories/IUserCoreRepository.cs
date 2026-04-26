using CoreService.Domain.Entities;

namespace CoreService.Application.Interfaces.Repositories;

public interface IUserCoreRepository
{
    IUnitOfWork UnitOfWork { get; }
    Task<UserCore?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserCore?> AddAsync(UserCore user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(UserCore user, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<UserCore> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}

