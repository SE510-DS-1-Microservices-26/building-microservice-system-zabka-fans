using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Domain.Entities;
using CoreService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Infrastructure.Persistence.Repositories;

public class InternshipApplicationRepository : IInternshipApplicationRepository
{
    private readonly CoreDbContext _context;

    public InternshipApplicationRepository(CoreDbContext context)
    {
        _context = context;
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task AddAsync(InternshipApplication entity, CancellationToken cancellationToken = default)
    {
        await _context.Applications.AddAsync(entity, cancellationToken);
    }

    public async Task<InternshipApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Applications.FirstOrDefaultAsync(internshipApplication => internshipApplication.Id == id,
            cancellationToken);
    }

    public async Task<IReadOnlyList<InternshipApplication>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Applications.ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(InternshipApplication entity, CancellationToken cancellationToken = default)
    {
        _context.Applications.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var internshipApplication = await GetByIdAsync(id, cancellationToken);
        if (internshipApplication != null) _context.Applications.Remove(internshipApplication);
    }

    public async Task<InternshipApplication?> GetWithDetailsAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .Include(internshipApplication => internshipApplication.Candidate)
            .Include(internshipApplication => internshipApplication.Internship)
            .FirstOrDefaultAsync(internshipApplication => internshipApplication.Id == id, cancellationToken);
    }

    public async Task<int> CountReservedSpotsAsync(Guid internshipId, CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .Where(internshipApplication => EF.Property<Guid>(internshipApplication, "InternshipId") == internshipId &&
                                            (internshipApplication.Status == ApplicationStatus.Accepted ||
                                             internshipApplication.Status == ApplicationStatus.Enrolled))
            .CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid candidateId, Guid internshipId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .AnyAsync(internshipApplication => EF.Property<Guid>(internshipApplication, "CandidateId") == candidateId
                                               && EF.Property<Guid>(internshipApplication, "InternshipId") ==
                                               internshipId,
                cancellationToken);
    }

    public async Task<bool> HasStatusAsync(Guid candidateId, ApplicationStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .AnyAsync(
                internshipApplication => EF.Property<Guid>(internshipApplication, "CandidateId") == candidateId &&
                                         internshipApplication.Status == status, cancellationToken);
    }

    public async Task<(IReadOnlyList<InternshipApplication> Items, int TotalCount)> GetPagedWithDetailsAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Applications
            .AsNoTracking()
            .Include(a => a.Candidate)
            .Include(a => a.Internship)
            .OrderByDescending(a => a.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}