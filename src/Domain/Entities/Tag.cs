using RC.HyRe.Domain.Common;

namespace RC.HyRe.Domain.Entities;

public class Tag : HiringBaseEntity
{
    public required string Name { get; set; }   // unique
    public string? Color { get; set; }          // hex color, e.g. "#4CAF50"
}
