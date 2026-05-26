using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Scorecards.Commands;

[Authorize(Permissions = Permissions.ScorecardsUpdate)]
public record SaveScorecardDraft(
    Guid Id,
    Dictionary<string, int>? Ratings,
    string? Recommendation,
    string? Strengths,
    string? Concerns,
    string? Notes
) : IRequest<Result>;

public class SaveScorecardDraftHandler : IRequestHandler<SaveScorecardDraft, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SaveScorecardDraftHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result> Handle(SaveScorecardDraft request, CancellationToken ct)
    {
        var scorecard = await _context.Scorecards
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (scorecard is null)
            return Result.Failure("Scorecard not found.");

        if (scorecard.InterviewerId != _user.Id)
            return Result.Failure("You can only edit your own scorecard.");

        if (scorecard.SubmittedAt.HasValue)
            return Result.Failure("Cannot edit a submitted scorecard.");

        if (request.Ratings is not null)
            scorecard.Ratings = JsonDocument.Parse(JsonSerializer.Serialize(request.Ratings));

        if (request.Recommendation is not null)
            scorecard.Recommendation = Enum.Parse<ScorecardRecommendation>(request.Recommendation);

        if (request.Strengths is not null)
            scorecard.Strengths = request.Strengths;

        if (request.Concerns is not null)
            scorecard.Concerns = request.Concerns;

        if (request.Notes is not null)
            scorecard.Notes = request.Notes;

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
