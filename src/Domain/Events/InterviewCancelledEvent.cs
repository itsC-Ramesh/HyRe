namespace RC.HyRe.Domain.Events;

public class InterviewCancelledEvent : BaseEvent
{
    public InterviewCancelledEvent(Guid interviewId, Guid applicationId, string interviewerId, string? reason)
    {
        InterviewId = interviewId;
        ApplicationId = applicationId;
        InterviewerId = interviewerId;
        Reason = reason;
    }

    public Guid InterviewId { get; }
    public Guid ApplicationId { get; }
    public string InterviewerId { get; }
    public string? Reason { get; }
}
