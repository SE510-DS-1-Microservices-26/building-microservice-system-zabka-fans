using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ITProvisionService.Infrastructure.Persistence;

public class ITProvisionDbContext : DbContext
{
    public ITProvisionDbContext(DbContextOptions<ITProvisionDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}

