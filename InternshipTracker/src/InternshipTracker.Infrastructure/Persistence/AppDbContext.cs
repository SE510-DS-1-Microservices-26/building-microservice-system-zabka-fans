using InternshipTracker.Application.Interfaces;
using InternshipTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternshipTracker.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Internship> Internships => Set<Internship>();
    public DbSet<InternshipApplication> Applications => Set<InternshipApplication>();

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}