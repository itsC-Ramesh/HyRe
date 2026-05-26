using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RC.HyRe.Domain.Entities;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class EventLogConfiguration : IEntityTypeConfiguration<EventLog>
{
    public void Configure(EntityTypeBuilder<EventLog> builder)
    {
        builder.ToTable("event_log");

        builder.HasIndex(e => new { e.EntityType, e.EntityId });
        builder.HasIndex(e => e.Action);
        builder.HasIndex(e => e.Created);

        builder.Property(e => e.PayloadJson)
            .HasColumnType("jsonb"); // If using Postgres, otherwise it will just map to text on sqlite/sqlserver usually but jsonb is fine if supported, wait. Let's see if other entities use jsonb.
    }
}
