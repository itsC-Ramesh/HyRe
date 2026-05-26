namespace RC.HyRe.Domain.Events;

public class InterviewNoShowEvent : BaseEvent
{
    public InterviewNoShowEvent(Guid interviewId, Guid applicationId, string interviewerId)
    {
        InterviewId = interviewId;
        ApplicationId = applicationId;
        InterviewerId = interviewerId;
    }

    public Guid InterviewId { get; }
    public Guid ApplicationId { get; }
    public string InterviewerId { get; }
}
