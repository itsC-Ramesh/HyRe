using System.Text.Json;

namespace RC.HyRe.Domain.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public required string EntityType { get; set; }
    public string? EntityId { get; set; }
    public required string Action { get; set; }
    public string? ActorId { get; set; }
    public string? ActorRole { get; set; }
    public string? IpAddress { get; set; }
    public JsonDocument? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
