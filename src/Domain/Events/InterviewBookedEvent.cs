namespace RC.HyRe.Domain.Events;

/// <summary>
/// Raised when an interview is booked (Interview.Book() is called).
/// Triggers in-app notification to the interviewer and email to the candidate.
/// </summary>
public class InterviewBookedEvent : BaseEvent
{
    public InterviewBookedEvent(
        Guid interviewId,
        Guid applicationId,
        string interviewerId,
        string? actorId)
    {
        InterviewId = interviewId;
        ApplicationId = applicationId;
        InterviewerId = interviewerId;
        ActorId = actorId;
    }

    public Guid InterviewId { get; }
    public Guid ApplicationId { get; }
    public string InterviewerId { get; }
    public string? ActorId { get; }
}
