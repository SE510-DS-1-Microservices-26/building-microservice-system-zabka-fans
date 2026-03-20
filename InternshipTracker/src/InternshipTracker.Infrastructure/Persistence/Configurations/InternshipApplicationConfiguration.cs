using InternshipTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternshipTracker.Infrastructure.Persistence.Configurations;

public class InternshipApplicationConfiguration : IEntityTypeConfiguration<InternshipApplication>
{
    public void Configure(EntityTypeBuilder<InternshipApplication> builder)
    {
        builder.ToTable("InternshipApplications");

        builder.HasKey(internshipApplication => internshipApplication.Id);

        builder.Property(internshipApplication => internshipApplication.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.HasOne(internshipApplication => internshipApplication.Candidate)
            .WithMany()
            .HasForeignKey("CandidateId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(internshipApplication => internshipApplication.Internship)
            .WithMany()
            .HasForeignKey("InternshipId")
            .OnDelete(DeleteBehavior.Cascade);

        // safeguard: prevents a candidate from applying to the same internship twice
        builder.HasIndex("CandidateId", "InternshipId")
            .IsUnique();
    }
}