using CoreService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreService.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<UserCore>
{
    public void Configure(EntityTypeBuilder<UserCore> builder)
    {
        builder.ToTable("UsersCore");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(user => user.CorporateEmail)
            .HasMaxLength(320);

        builder.Property(user => user.Level)
            .HasConversion<string>()
            .IsRequired();
    }
}