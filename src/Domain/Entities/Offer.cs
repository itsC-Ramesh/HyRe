using RC.HyRe.Domain.Common;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Domain.Entities;

/// <summary>
/// An employment offer extended to a candidate after a successful interview process.
/// One offer per application (unique constraint).
/// Salary is stored as integer in the smallest currency unit (paise for INR).
/// </summary>
public class Offer : HiringBaseEntity
{
    public Guid ApplicationId { get; set; }

    /// <summary>Salary in smallest currency unit (paise for INR, cents for USD).</summary>
    public int Salary { get; set; }

    public string Currency { get; set; } = "INR";

    public DateTimeOffset StartDate { get; set; }

    public ContractType ContractType { get; set; }

    public DateTimeOffset ExpiryDate { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Draft;

    /// <summary>FK → Document (the generated offer letter PDF). Set after generation.</summary>
    public Guid? LetterDocId { get; set; }

    public DateTimeOffset? SignedAt { get; set; }

    // Navigation
    public JobApplication Application { get; set; } = null!;
    public Document? LetterDocument { get; set; }

    /// <summary>
    /// Records candidate acceptance of the offer and raises an OfferAcceptedEvent.
    /// The application stage transition is handled by the command handler.
    /// </summary>
    public void Accept(string? actorId = null)
    {
        if (Status != OfferStatus.Sent)
            throw new InvalidOperationException("Only a sent offer can be accepted.");

        Status = OfferStatus.Accepted;
        SignedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new OfferAcceptedEvent(Id, ApplicationId, actorId));
    }

    /// <summary>Records that the candidate declined the offer.</summary>
    public void Decline(string? actorId = null)
    {
        if (Status != OfferStatus.Sent)
            throw new InvalidOperationException("Only a sent offer can be declined.");

        Status = OfferStatus.Declined;
    }
}
