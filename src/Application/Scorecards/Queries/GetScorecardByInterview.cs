using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Scorecards.Queries;

[Authorize(Permissions = Permissions.ScorecardsRead)]
public record GetScorecardByInterview(Guid InterviewId) : IRequest<Result<ScorecardDto>>;

public class GetScorecardByInterviewHandler
    : IRequestHandler<GetScorecardByInterview, Result<ScorecardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetScorecardByInterviewHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<ScorecardDto>> Handle(
        GetScorecardByInterview request, CancellationToken ct)
    {
        var scorecard = await _context.Scorecards
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.InterviewId == request.InterviewId && s.InterviewerId == _user.Id, ct);

        if (scorecard is null)
            return Result.Failure<ScorecardDto>("Scorecard not found.");

        return Result.Success(ScorecardMappingHelper.MapToDto(scorecard));
    }
}
