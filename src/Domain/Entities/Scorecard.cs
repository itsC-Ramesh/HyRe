using System.Text.Json;
using RC.HyRe.Domain.Common;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Domain.Entities;

/// <summary>
/// Structured feedback submitted by an interviewer after completing an interview.
/// One scorecard per interview (unique constraint). Blind until submitted.
/// Ratings are stored as JSONB: { technical, communication, problemSolving, cultureFit } — each 1–5.
/// </summary>
public class Scorecard : HiringBaseEntity
{
    public Guid InterviewId { get; set; }

    /// <summary>FK → AspNetUsers.Id (string, matches IdentityUser.Id).</summary>
    public required string InterviewerId { get; set; }

    /// <summary>
    /// JSONB ratings: { "technical": 4, "communication": 3, "problemSolving": 5, "cultureFit": 4 }
    /// Each value is 1–5.
    /// </summary>
    public JsonDocument Ratings { get; set; } = JsonDocument.Parse("{}");

    public ScorecardRecommendation Recommendation { get; set; }

    public required string Strengths { get; set; }

    public required string Concerns { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    // Navigation
    public Interview Interview { get; set; } = null!;

    /// <summary>
    /// Marks the scorecard as submitted, locking it from further edits,
    /// and raises a ScorecardSubmittedEvent.
    /// </summary>
    public void Submit(string? actorId = null)
    {
        if (SubmittedAt.HasValue)
            throw new InvalidOperationException("Scorecard has already been submitted.");

        SubmittedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new ScorecardSubmittedEvent(Id, InterviewId, actorId));
    }
}
