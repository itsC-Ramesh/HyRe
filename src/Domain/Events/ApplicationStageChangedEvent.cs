using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Domain.Events;

/// <summary>
/// Raised when an Application moves from one pipeline stage to another.
/// The notification engine (4.3) will subscribe to this to send stage-change emails.
/// </summary>
public class ApplicationStageChangedEvent : BaseEvent
{
    public ApplicationStageChangedEvent(
        Guid applicationId,
        ApplicationStage previousStage,
        ApplicationStage newStage,
        string? actorId)
    {
        ApplicationId = applicationId;
        PreviousStage = previousStage;
        NewStage = newStage;
        ActorId = actorId;
    }

    public Guid ApplicationId { get; }
    public ApplicationStage PreviousStage { get; }
    public ApplicationStage NewStage { get; }
    public string? ActorId { get; }
}
