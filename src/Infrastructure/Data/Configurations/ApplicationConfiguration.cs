using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        // Use table name "job_applications" to avoid clashing with PostgreSQL's reserved word "application"
        builder.ToTable("job_applications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Stage)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.RejectionReason)
            .HasMaxLength(1000);

        // Unique constraint — one application per candidate per requisition
        builder.HasIndex(a => new { a.CandidateId, a.RequisitionId })
            .IsUnique();

        builder.HasIndex(a => new { a.RequisitionId, a.Stage });

        builder.HasMany(a => a.Interviews)
            .WithOne(i => i.Application)
            .HasForeignKey(i => i.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Offer)
            .WithOne(o => o.Application)
            .HasForeignKey<Offer>(o => o.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(a => a.DomainEvents);
    }
}
