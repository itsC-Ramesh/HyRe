using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("templates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(t => t.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Body)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true);

        builder.Property(t => t.Version)
            .HasDefaultValue(1);

        builder.Property(t => t.IsBuiltIn)
            .HasDefaultValue(false);

        // Composite index for common query pattern
        builder.HasIndex(t => new { t.Category, t.IsActive });

        builder.Ignore(t => t.DomainEvents);
    }
}
