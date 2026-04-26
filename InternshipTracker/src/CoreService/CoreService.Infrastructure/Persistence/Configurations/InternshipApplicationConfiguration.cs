using CoreService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreService.Infrastructure.Persistence.Configurations;

public class InternshipApplicationConfiguration : IEntityTypeConfiguration<InternshipApplication>
{
    public void Configure(EntityTypeBuilder<InternshipApplication> builder)
    {
        builder.ToTable("InternshipApplications");

        builder.HasKey(internshipApplication => internshipApplication.Id);

        builder.Property(internshipApplication => internshipApplication.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(internshipApplication => internshipApplication.CandidateId)
            .IsRequired();
        
        builder.Property(internshipApplication => internshipApplication.CandidateLevel)
            .HasConversion<string>()
            .IsRequired();

        builder.HasOne(internshipApplication => internshipApplication.Candidate)
            .WithMany()
            .HasForeignKey(internshipApplication => internshipApplication.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(internshipApplication => internshipApplication.Internship)
            .WithMany()
            .HasForeignKey("InternshipId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex("CandidateId", "InternshipId")
            .IsUnique();
    }
}