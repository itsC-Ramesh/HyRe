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

        builder.Property(n => n.PayloadJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(n => n.DeliveryChannel)
            .HasMaxLength(50);

        builder.Property(n => n.DeliveryStatus)
            .HasMaxLength(50);

        builder.HasIndex(n => n.DeliveryStatus);
        builder.HasIndex(n => new { n.RecipientId, n.ReadAt });

        builder.Ignore(n => n.DomainEvents);
    }
}
