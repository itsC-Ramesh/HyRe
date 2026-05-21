namespace RC.HyRe.Domain.Events;

/// <summary>
/// Raised when an offer is initiated and sent for approval.
/// Triggers in-app notification to the designated approver.
/// </summary>
public class OfferInitiatedEvent : BaseEvent
{
    public OfferInitiatedEvent(
        Guid offerId,
        Guid applicationId,
        string? actorId)
    {
        OfferId = offerId;
        ApplicationId = applicationId;
        ActorId = actorId;
    }

    public Guid OfferId { get; }
    public Guid ApplicationId { get; }
    public string? ActorId { get; }
}
