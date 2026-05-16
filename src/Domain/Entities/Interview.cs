using RC.HyRe.Domain.Common;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Domain.Entities;

/// <summary>
/// A scheduled interview between a candidate (via Application) and an interviewer.
/// Each interview produces exactly one Scorecard.
/// InterviewerId references AspNetUsers.Id (string FK).
/// </summary>
public class Interview : HiringBaseEntity
{
    public Guid ApplicationId { get; set; }

    /// <summary>FK → AspNetUsers.Id (string, matches IdentityUser.Id).</summary>
    public required string InterviewerId { get; set; }

    public InterviewType Type { get; set; }

    public DateTimeOffset ScheduledAt { get; set; }

    public int DurationMin { get; set; } = 60;

    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;

    public string? MeetingLink { get; set; }

    // Navigation
    public JobApplication Application { get; set; } = null!;
    public Scorecard? Scorecard { get; set; }
}
