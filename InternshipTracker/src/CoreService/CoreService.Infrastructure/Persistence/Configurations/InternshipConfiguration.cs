using CoreService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreService.Infrastructure.Persistence.Configurations;

public class InternshipConfiguration : IEntityTypeConfiguration<Internship>
{
    public void Configure(EntityTypeBuilder<Internship> builder)
    {
        builder.ToTable("Internships");

        builder.HasKey(internship => internship.Id);

        builder.Property(internship => internship.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(internship => internship.Capacity)
            .IsRequired();

        builder.Property(internship => internship.MinimumLevel)
            .HasConversion<string>()
            .IsRequired();
    }
}