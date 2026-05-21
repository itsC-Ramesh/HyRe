namespace RC.HyRe.Domain.Events;

/// <summary>
/// Raised when a candidate declines an offer (Offer.Decline() is called).
/// Triggers in-app notification to the requisition owner.
/// </summary>
public class OfferDeclinedEvent : BaseEvent
{
    public OfferDeclinedEvent(
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
