using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreService.Infrastructure.Saga;

public class OnboardingSagaStateMap : SagaClassMap<OnboardingSagaState>
{
    protected override void Configure(EntityTypeBuilder<OnboardingSagaState> entity, ModelBuilder model)
    {
        entity.ToTable("OnboardingSagaState");

        entity.Property(x => x.CurrentState)
            .HasMaxLength(64)
            .IsRequired();

        entity.Property(x => x.CandidateName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.CandidateEmail)
            .HasMaxLength(320)
            .IsRequired();

        entity.Property(x => x.CorporateEmail)
            .HasMaxLength(320);

        entity.Property(x => x.FaultReason)
            .HasMaxLength(1000);
    }
}

