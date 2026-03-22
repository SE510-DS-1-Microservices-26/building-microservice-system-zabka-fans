using InternshipTracker.Application.Interfaces;
using InternshipTracker.Application.Interfaces.Repositories;
using InternshipTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternshipTracker.Infrastructure.Persistence.Repositories;

public class InternshipRepository : IInternshipRepository
{
    private readonly AppDbContext _context;

    public InternshipRepository(AppDbContext context)
    {
        _context = context;
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task AddAsync(Internship entity, CancellationToken cancellationToken = default)
    {
        await _context.Internships.AddAsync(entity, cancellationToken);
    }

    public async Task<Internship?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Internships.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Internship>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Internships.ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Internship entity, CancellationToken cancellationToken = default)
    {
        _context.Internships.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var internship = await GetByIdAsync(id, cancellationToken);
        if (internship != null) _context.Internships.Remove(internship);
    }
}