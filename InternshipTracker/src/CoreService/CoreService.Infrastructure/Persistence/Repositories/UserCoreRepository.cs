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

    public async Task<UserCore?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}

