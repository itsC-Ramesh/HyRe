using RC.HyRe.Domain.Common;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Domain.Entities;

/// <summary>
/// Links a Candidate to a Requisition and tracks their progress through pipeline stages.
/// One candidate can have multiple applications (one per requisition).
/// A unique constraint on (CandidateId, RequisitionId) prevents duplicates.
/// </summary>
public class JobApplication : HiringBaseEntity
{
    public Guid CandidateId { get; set; }

    public Guid RequisitionId { get; set; }

    public ApplicationStage Stage { get; set; } = ApplicationStage.Applied;

    public string? RejectionReason { get; set; }

    // Navigation
    public Candidate Candidate { get; set; } = null!;
    public Requisition Requisition { get; set; } = null!;
    public ICollection<Interview> Interviews { get; set; } = [];
    public Offer? Offer { get; set; }

    /// <summary>
    /// Moves the application to a new stage and raises a domain event.
    /// Use this method for all stage transitions — never set Stage directly.
    /// </summary>
    public void AdvanceStage(ApplicationStage newStage, string? actorId = null)
    {
        var previousStage = Stage;
        Stage = newStage;
        AddDomainEvent(new ApplicationStageChangedEvent(Id, previousStage, newStage, actorId));
    }

    /// <summary>Rejects the application with an optional reason.</summary>
    public void Reject(string? reason = null, string? actorId = null)
    {
        RejectionReason = reason;
        AdvanceStage(ApplicationStage.Rejected, actorId);
    }
}
