using RC.HyRe.Domain.Common;

namespace RC.HyRe.Domain.Entities;

public class Note : HiringBaseEntity
{
    public required string EntityType { get; set; }  // "requisition", "candidate"
    public Guid EntityId { get; set; }
    public required string Content { get; set; }
}
