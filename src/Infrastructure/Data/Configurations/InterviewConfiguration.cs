using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class InterviewConfiguration : IEntityTypeConfiguration<Interview>
{
    public void Configure(EntityTypeBuilder<Interview> builder)
    {
        builder.ToTable("interviews");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InterviewerId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(i => i.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.MeetingLink)
            .HasMaxLength(2000);

        builder.Property(i => i.DurationMin)
            .HasDefaultValue(60);

        builder.HasIndex(i => new { i.ApplicationId, i.Status });

        // Scorecard: one per interview (unique)
        builder.HasOne(i => i.Scorecard)
            .WithOne(s => s.Interview)
            .HasForeignKey<Scorecard>(s => s.InterviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(i => i.DomainEvents);
    }
}
