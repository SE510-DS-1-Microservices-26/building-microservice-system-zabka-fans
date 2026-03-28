using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Infrastructure.Persistence.Repositories;

public class UserCoreRepository : IUserCoreRepository
{
    private readonly CoreDbContext _context;

    public UserCoreRepository(CoreDbContext context)
    {
        _context = context;
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task<UserCore?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<UserCore?> AddAsync(UserCore user, CancellationToken cancellationToken = default)
    {
        var addedUser = await _context.Users.AddAsync(user, cancellationToken);
        if (addedUser.State == EntityState.Added)
        {
            return addedUser.Entity;
        }

        return null;
    }

    public async Task<bool> DeleteAsync(UserCore user, CancellationToken cancellationToken = default)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
        if (existingUser == null)
        {
            return false;
        }

        _context.Users.Remove(existingUser);
        return true;
    }
}