namespace RC.HyRe.Domain.Events;

/// <summary>
/// Raised when an interviewer submits a Scorecard.
/// The notification engine will notify the hiring manager and check if all scorecards
/// for the application are now submitted (all_scorecards.submitted).
/// </summary>
public class ScorecardSubmittedEvent : BaseEvent
{
    public ScorecardSubmittedEvent(
        Guid scorecardId,
        Guid interviewId,
        string? actorId)
    {
        ScorecardId = scorecardId;
        InterviewId = interviewId;
        ActorId = actorId;
    }

    public Guid ScorecardId { get; }
    public Guid InterviewId { get; }
    public string? ActorId { get; }
}
