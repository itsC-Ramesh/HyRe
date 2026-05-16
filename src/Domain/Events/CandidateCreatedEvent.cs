namespace RC.HyRe.Domain.Events;

/// <summary>
/// Raised when a new Candidate record is created.
/// Used by analytics to count application volume and by comms for acknowledgement emails.
/// </summary>
public class CandidateCreatedEvent : BaseEvent
{
    public CandidateCreatedEvent(
        Guid candidateId,
        string email,
        string? actorId)
    {
        CandidateId = candidateId;
        Email = email;
        ActorId = actorId;
    }

    public Guid CandidateId { get; }
    public string Email { get; }
    public string? ActorId { get; }
}
