namespace RC.HyRe.Domain.Events;

/// <summary>
/// Raised when a candidate accepts an offer (Offer.Accept() is called).
/// The notification engine (4.3) will trigger onboarding checklist creation,
/// HRIS export, and stage → Hired transition via a command handler.
/// </summary>
public class OfferAcceptedEvent : BaseEvent
{
    public OfferAcceptedEvent(
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
