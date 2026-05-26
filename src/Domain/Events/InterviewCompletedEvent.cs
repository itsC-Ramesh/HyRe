namespace RC.HyRe.Domain.Events;

/// <summary>
/// Raised when an interview is marked as completed (Interview.Complete() is called).
/// </summary>
public class InterviewCompletedEvent : BaseEvent
{
    public InterviewCompletedEvent(Guid interviewId, Guid applicationId)
    {
        InterviewId = interviewId;
        ApplicationId = applicationId;
    }

    public Guid InterviewId { get; }
    public Guid ApplicationId { get; }
}
