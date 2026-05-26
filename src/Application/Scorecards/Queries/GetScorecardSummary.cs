using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Scorecards.Queries;

[Authorize(Permissions = Permissions.ScorecardsRead)]
public record GetScorecardSummary(Guid ApplicationId)
    : IRequest<Result<ScorecardSummaryDto>>;

public class GetScorecardSummaryHandler
    : IRequestHandler<GetScorecardSummary, Result<ScorecardSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetScorecardSummaryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<ScorecardSummaryDto>> Handle(
        GetScorecardSummary request, CancellationToken ct)
    {
        var interviews = await _context.Interviews
            .AsNoTracking()
            .Where(i => i.ApplicationId == request.ApplicationId)
            .ToListAsync(ct);

        var interviewIds = interviews.Select(i => i.Id).ToList();

        var scorecards = await _context.Scorecards
            .AsNoTracking()
            .Where(s => interviewIds.Contains(s.InterviewId))
            .ToListAsync(ct);

        var submittedCount = scorecards.Count(s => s.SubmittedAt.HasValue);
        var pendingCount = scorecards.Count - submittedCount;

        var submittedScorecards = scorecards.Where(s => s.SubmittedAt.HasValue).ToList();
        var averageRatings = new Dictionary<string, double>();

        if (submittedScorecards.Any())
        {
            var dimensions = new[] { "technical", "communication", "problemSolving", "cultureFit" };
            foreach (var dim in dimensions)
            {
                var values = submittedScorecards
                    .Where(s => s.Ratings != null)
                    .Select(s =>
                    {
                        if (s.Ratings!.RootElement.TryGetProperty(dim, out var val))
                            return (double)val.GetInt32();
                        return 0.0;
                    })
                    .ToList();

                averageRatings[dim] = values.Any() ? values.Average() : 0;
            }
        }

        var recommendationBreakdown = submittedScorecards
            .GroupBy(s => s.Recommendation.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return Result.Success(new ScorecardSummaryDto(
            TotalInterviews: interviews.Count,
            SubmittedCount: submittedCount,
            PendingCount: pendingCount,
            AverageRatings: averageRatings,
            RecommendationBreakdown: recommendationBreakdown,
            Scorecards: submittedScorecards.Select(s => new ScorecardSummaryItemDto(
                s.Id,
                s.InterviewId,
                s.InterviewerId,
                s.Recommendation.ToString(),
                s.SubmittedAt
            )).ToList()
        ));
    }
}

public record ScorecardSummaryDto(
    int TotalInterviews,
    int SubmittedCount,
    int PendingCount,
    Dictionary<string, double> AverageRatings,
    Dictionary<string, int> RecommendationBreakdown,
    List<ScorecardSummaryItemDto> Scorecards
);

public record ScorecardSummaryItemDto(
    Guid Id,
    Guid InterviewId,
    string InterviewerId,
    string Recommendation,
    DateTimeOffset? SubmittedAt
);
