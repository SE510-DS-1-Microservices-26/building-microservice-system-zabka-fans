using CoreService.Application.Interfaces;
using CoreService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Infrastructure.Persistence;

public class CoreDbContext : DbContext, IUnitOfWork
{
    public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options)
    {
    }
    public DbSet<Internship> Internships => Set<Internship>();
    public DbSet<InternshipApplication> Applications => Set<InternshipApplication>();

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);
    }
}