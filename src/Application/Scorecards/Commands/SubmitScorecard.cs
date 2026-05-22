using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Scorecards.Commands;

[Authorize(Permissions = Permissions.ScorecardsUpdate)]
public record SubmitScorecard(
    Guid Id,
    Dictionary<string, int> Ratings,
    string Recommendation,
    string Strengths,
    string Concerns,
    string? Notes
) : IRequest<Result>;

public class SubmitScorecardHandler : IRequestHandler<SubmitScorecard, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SubmitScorecardHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result> Handle(SubmitScorecard request, CancellationToken ct)
    {
        var scorecard = await _context.Scorecards
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (scorecard is null)
            return Result.Failure("Scorecard not found.");

        if (scorecard.InterviewerId != _user.Id)
            return Result.Failure("You can only submit your own scorecard.");

        if (scorecard.SubmittedAt.HasValue)
            return Result.Failure("Scorecard has already been submitted.");

        scorecard.Ratings = JsonDocument.Parse(JsonSerializer.Serialize(request.Ratings));
        scorecard.Recommendation = Enum.Parse<ScorecardRecommendation>(request.Recommendation);
        scorecard.Strengths = request.Strengths;
        scorecard.Concerns = request.Concerns;
        scorecard.Notes = request.Notes;

        scorecard.Submit(_user.Id);

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
