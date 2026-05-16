using RC.HyRe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("events_log");
        builder.HasIndex(e => new { e.EntityType, e.EntityId });
        builder.HasIndex(e => e.CreatedAt);
    }
}
