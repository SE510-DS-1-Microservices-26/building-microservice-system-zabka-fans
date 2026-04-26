using UserService.Domain.Entities;

namespace UserService.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Delete(User user);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}