using System.Text.Json;
using RC.HyRe.Domain.Entities;

namespace RC.HyRe.Application.Scorecards.Queries;

internal static class ScorecardMappingHelper
{
    public static ScorecardDto MapToDto(Scorecard scorecard)
    {
        var ratings = new Dictionary<string, int>();
        if (scorecard.Ratings.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in scorecard.Ratings.RootElement.EnumerateObject())
            {
                if (prop.Value.TryGetInt32(out var val))
                    ratings[prop.Name] = val;
            }
        }

        return new ScorecardDto(
            scorecard.Id,
            scorecard.InterviewId,
            scorecard.InterviewerId,
            ratings,
            scorecard.Recommendation,
            scorecard.Strengths,
            scorecard.Concerns,
            scorecard.Notes,
            scorecard.SubmittedAt,
            scorecard.SubmittedAt.HasValue);
    }
}
