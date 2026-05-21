using RC.HyRe.Domain.Common;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Domain.Entities;

/// <summary>
/// An email / message template with {{variable}} placeholders.
/// Built-in templates are seeded and cannot be deleted.
/// Each edit snapshots the previous version into TemplateVersion.
/// </summary>
public class Template : HiringBaseEntity
{
    public required string Name { get; set; }

    public TemplateCategory Category { get; set; }

    /// <summary>Subject line, supports {{VariableName}} placeholders.</summary>
    public required string Subject { get; set; }

    /// <summary>Body content, supports {{VariableName}} placeholders.</summary>
    public required string Body { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Monotonically increasing version counter.</summary>
    public int Version { get; set; } = 1;

    /// <summary>If true, the template was seeded and cannot be deleted.</summary>
    public bool IsBuiltIn { get; set; } = false;

    // Navigation
    public ICollection<TemplateVersion> Versions { get; set; } = [];
}
