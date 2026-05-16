using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RC.HyRe.Domain.Entities;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasMaxLength(100);

        // Payload stored as JSONB
        builder.Property(n => n.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        // Frequent query: unread notifications for a user
        builder.HasIndex(n => new { n.RecipientId, n.ReadAt });

        builder.Ignore(n => n.DomainEvents);
    }
}
