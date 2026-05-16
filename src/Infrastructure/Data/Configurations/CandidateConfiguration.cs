using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("candidates");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(320); // RFC 5321 max

        builder.HasIndex(c => c.Email)
            .IsUnique();

        builder.Property(c => c.Phone)
            .HasMaxLength(30);

        builder.Property(c => c.Source)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.SourceDetail)
            .HasMaxLength(500);

        // Resume document FK — nullable; no cascade delete (document outlives candidate record)
        builder.HasOne(c => c.ResumeDocument)
            .WithOne(d => d.CandidateResume)
            .HasForeignKey<Candidate>(c => c.ResumeDocId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.Applications)
            .WithOne(a => a.Candidate)
            .HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit columns (set by AuditableEntityInterceptor)
        builder.Property(c => c.Created).IsRequired();
        builder.Property(c => c.LastModified).IsRequired();
        builder.Property(c => c.CreatedBy).HasMaxLength(450);
        builder.Property(c => c.LastModifiedBy).HasMaxLength(450);

        builder.Ignore(c => c.DomainEvents);
    }
}
