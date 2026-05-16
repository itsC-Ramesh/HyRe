using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class RequisitionConfiguration : IEntityTypeConfiguration<Requisition>
{
    public void Configure(EntityTypeBuilder<Requisition> builder)
    {
        builder.ToTable("requisitions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(r => r.Department)
            .IsRequired()
            .HasMaxLength(200);

        // OwnerId is a string FK to AspNetUsers (Identity PK)
        builder.Property(r => r.OwnerId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(r => r.JdText)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.Headcount)
            .HasDefaultValue(1);

        builder.HasIndex(r => new { r.Status, r.Department });

        builder.HasMany(r => r.Applications)
            .WithOne(a => a.Requisition)
            .HasForeignKey(a => a.RequisitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(r => r.DomainEvents);
    }
}
