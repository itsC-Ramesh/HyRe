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
    private static readonly Dictionary<ApplicationStage, ApplicationStage[]> AllowedTransitions = new()
    {
        [ApplicationStage.Applied] = new[] { ApplicationStage.Screened, ApplicationStage.Rejected },
        [ApplicationStage.Screened] = new[] { ApplicationStage.Interview, ApplicationStage.Rejected },
        [ApplicationStage.Interview] = new[] { ApplicationStage.Offer, ApplicationStage.Rejected },
        [ApplicationStage.Offer] = new[] { ApplicationStage.Hired, ApplicationStage.Rejected },
        [ApplicationStage.Hired] = Array.Empty<ApplicationStage>(),
        [ApplicationStage.Rejected] = Array.Empty<ApplicationStage>(),
    };

    public void AdvanceStage(ApplicationStage newStage, string? actorId = null)
    {
        if (newStage == ApplicationStage.Rejected)
        {
            // Reject() calls AdvanceStage — allow it through the guard
        }
        else if (!AllowedTransitions.ContainsKey(Stage) || !AllowedTransitions[Stage].Contains(newStage))
        {
            throw new InvalidOperationException(
                $"Cannot advance from {Stage} to {newStage}.");
        }

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
