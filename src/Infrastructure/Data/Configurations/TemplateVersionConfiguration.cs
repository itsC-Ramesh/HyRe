using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RC.HyRe.Domain.Entities;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class TemplateVersionConfiguration : IEntityTypeConfiguration<TemplateVersion>
{
    public void Configure(EntityTypeBuilder<TemplateVersion> builder)
    {
        builder.ToTable("template_versions");

        builder.HasKey(tv => tv.Id);

        // FK to Template with cascade delete
        builder.HasOne(tv => tv.Template)
            .WithMany(t => t.Versions)
            .HasForeignKey(tv => tv.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(tv => tv.Version)
            .IsRequired();

        builder.Property(tv => tv.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(tv => tv.Body)
            .IsRequired()
            .HasColumnType("text");

        // Unique composite index: one version number per template
        builder.HasIndex(tv => new { tv.TemplateId, tv.Version })
            .IsUnique();

        builder.Ignore(tv => tv.DomainEvents);
    }
}
