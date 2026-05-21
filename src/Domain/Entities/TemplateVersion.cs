using RC.HyRe.Domain.Common;

namespace RC.HyRe.Domain.Entities;

/// <summary>
/// An immutable snapshot of a Template at a particular version.
/// Created automatically whenever a template is updated.
/// </summary>
public class TemplateVersion : HiringBaseEntity
{
    public Guid TemplateId { get; set; }

    public int Version { get; set; }

    /// <summary>Subject line at this version.</summary>
    public required string Subject { get; set; }

    /// <summary>Body content at this version.</summary>
    public required string Body { get; set; }

    // Navigation
    public Template Template { get; set; } = null!;
}
