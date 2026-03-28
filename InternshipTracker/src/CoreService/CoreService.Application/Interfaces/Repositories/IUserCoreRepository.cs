using CoreService.Domain.Entities;

namespace CoreService.Application.Interfaces.Repositories;

public interface IUserCoreRepository
{
    Task<UserCore?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

