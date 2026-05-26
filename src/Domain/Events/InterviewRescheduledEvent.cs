namespace RC.HyRe.Domain.Events;

public class InterviewRescheduledEvent : BaseEvent
{
    public InterviewRescheduledEvent(Guid interviewId, Guid applicationId, string interviewerId, DateTimeOffset previousTime, DateTimeOffset newTime)
    {
        InterviewId = interviewId;
        ApplicationId = applicationId;
        InterviewerId = interviewerId;
        PreviousTime = previousTime;
        NewTime = newTime;
    }

    public Guid InterviewId { get; }
    public Guid ApplicationId { get; }
    public string InterviewerId { get; }
    public DateTimeOffset PreviousTime { get; }
    public DateTimeOffset NewTime { get; }
}
