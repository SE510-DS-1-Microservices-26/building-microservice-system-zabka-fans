using CoreService.Application.Interfaces;
using CoreService.Domain.Entities;
using CoreService.Infrastructure.Saga;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Infrastructure.Persistence;

public class CoreDbContext : DbContext, IUnitOfWork
{
    public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options)
    {
    }

    public DbSet<Internship> Internships => Set<Internship>();
    public DbSet<InternshipApplication> Applications => Set<InternshipApplication>();
    public DbSet<UserCore> Users => Set<UserCore>();
    public DbSet<OnboardingSagaState> OnboardingSagaStates => Set<OnboardingSagaState>();

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        new OnboardingSagaStateMap().Configure(modelBuilder);
    }
}