using RC.HyRe.Domain.Common;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Domain.Entities;

/// <summary>
/// A single person who has applied for one or more roles.
/// Email is the deduplication key — there is exactly one Candidate record per person.
/// </summary>
public class Candidate : HiringBaseEntity
{
    public required string Name { get; set; }

    /// <summary>Unique identifier for the person across all applications.</summary>
    public required string Email { get; set; }

    public string? Phone { get; set; }

    public CandidateSource Source { get; set; } = CandidateSource.Direct;

    /// <summary>Referrer name, agency name, UTM value, etc.</summary>
    public string? SourceDetail { get; set; }

    /// <summary>FK → Document (resume file). Nullable until a resume is uploaded.</summary>
    public Guid? ResumeDocId { get; set; }

    // Navigation
    public Document? ResumeDocument { get; set; }
    public ICollection<JobApplication> Applications { get; set; } = [];
}
