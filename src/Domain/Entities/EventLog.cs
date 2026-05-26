using RC.HyRe.Domain.Common;

namespace RC.HyRe.Domain.Entities;

/// <summary>
/// Append-only log of every significant domain action. Never updated, never deleted.
/// Used to build candidate activity timelines and track events.
/// </summary>
public class EventLog : HiringBaseEntity
{
    /// <summary>Type of entity, e.g., "application", "interview", "offer", etc.</summary>
    public required string EntityType { get; set; }
    
    public required Guid EntityId { get; set; }
    
    /// <summary>Action performed, e.g., "stage.changed", "interview.booked", etc.</summary>
    public required string Action { get; set; }
    
    /// <summary>FK -> AspNetUsers, null = system</summary>
    public string? ActorId { get; set; }
    
    /// <summary>JSON serialized payload for metadata: previous_stage, new_stage, etc.</summary>
    public string? PayloadJson { get; set; }
}
