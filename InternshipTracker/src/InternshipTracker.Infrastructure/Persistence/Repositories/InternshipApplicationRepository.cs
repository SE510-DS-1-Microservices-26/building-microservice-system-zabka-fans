using InternshipTracker.Application.Interfaces;
using InternshipTracker.Application.Interfaces.Repositories;
using InternshipTracker.Domain.Entities;
using InternshipTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InternshipTracker.Infrastructure.Persistence.Repositories;

public class InternshipApplicationRepository : IInternshipApplicationRepository
{
    private readonly AppDbContext _context;
    public IUnitOfWork UnitOfWork => _context;

    public InternshipApplicationRepository(AppDbContext context)
    {
        _context = context;
    }

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
        if (internshipApplication != null)
        {
            _context.Applications.Remove(internshipApplication);
        }
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
                        (internshipApplication.Status == ApplicationStatus.Accepted || internshipApplication.Status == ApplicationStatus.Enrolled))
            .CountAsync(cancellationToken);
    }
    
    public async Task<bool> ExistsAsync(Guid candidateId, Guid internshipId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .AnyAsync(internshipApplication => EF.Property<Guid>(internshipApplication, "CandidateId") == candidateId
                           && EF.Property<Guid>(internshipApplication, "InternshipId") == internshipId,
                cancellationToken);
    }
    
    public async Task<bool> HasStatusAsync(Guid candidateId, ApplicationStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .AnyAsync(internshipApplication => EF.Property<Guid>(internshipApplication, "CandidateId") == candidateId && internshipApplication.Status == status, cancellationToken);
    }
    
}