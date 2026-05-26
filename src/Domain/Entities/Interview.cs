using RC.HyRe.Domain.Common;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Domain.Events;

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

    /// <summary>IDs of additional panel members (AspNetUsers.Id).</summary>
    public List<string> PanelMemberIds { get; set; } = new();

    // Navigation
    public JobApplication Application { get; set; } = null!;
    public Scorecard? Scorecard { get; set; }

    /// <summary>
    /// Books the interview, setting its status to Scheduled,
    /// and raises an InterviewBookedEvent.
    /// </summary>
    public void Book(string? actorId = null)
    {
        Status = InterviewStatus.Scheduled;
        AddDomainEvent(new InterviewBookedEvent(Id, ApplicationId, InterviewerId, actorId));
    }

    public void Complete()
    {
        Status = InterviewStatus.Completed;
        AddDomainEvent(new InterviewCompletedEvent(Id, ApplicationId));
    }
}
