using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Scorecards.Queries;

public record ScorecardDto(
    Guid Id,
    Guid InterviewId,
    string InterviewerId,
    Dictionary<string, int> Ratings,
    ScorecardRecommendation Recommendation,
    string Strengths,
    string Concerns,
    string? Notes,
    DateTimeOffset? SubmittedAt,
    bool IsSubmitted
);
