using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class ScorecardConfiguration : IEntityTypeConfiguration<Scorecard>
{
    public void Configure(EntityTypeBuilder<Scorecard> builder)
    {
        builder.ToTable("scorecards");

        builder.HasKey(s => s.Id);

        // InterviewId is also a unique FK (one scorecard per interview)
        builder.HasIndex(s => s.InterviewId)
            .IsUnique();

        builder.Property(s => s.InterviewerId)
            .IsRequired()
            .HasMaxLength(450);

        // Ratings stored as JSONB in PostgreSQL
        builder.Property(s => s.Ratings)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(s => s.Recommendation)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.Strengths)
            .IsRequired();

        builder.Property(s => s.Concerns)
            .IsRequired();

        builder.Ignore(s => s.DomainEvents);
    }
}
