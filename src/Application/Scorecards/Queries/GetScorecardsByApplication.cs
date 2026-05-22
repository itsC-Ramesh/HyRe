using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Scorecards.Queries;

[Authorize(Permissions = Permissions.ScorecardsRead)]
public record GetScorecardsByApplication(Guid ApplicationId)
    : IRequest<Result<List<ScorecardDto>>>;

public class GetScorecardsByApplicationHandler
    : IRequestHandler<GetScorecardsByApplication, Result<List<ScorecardDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetScorecardsByApplicationHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<List<ScorecardDto>>> Handle(
        GetScorecardsByApplication request, CancellationToken ct)
    {
        var isPrivileged = _user.Roles!.Contains(Roles.HrAdmin)
                        || _user.Roles.Contains(Roles.HiringManager);

        if (!isPrivileged)
        {
            // Blind review: interviewers only see their own scorecard
            var ownScorecard = await _context.Scorecards
                .AsNoTracking()
                .Where(s => s.InterviewerId == _user.Id
                         && s.Interview.ApplicationId == request.ApplicationId)
                .ToListAsync(ct);

            return Result.Success(ownScorecard.Select(ScorecardMappingHelper.MapToDto).ToList());
        }

        var scorecards = await _context.Scorecards
            .AsNoTracking()
            .Where(s => s.Interview.ApplicationId == request.ApplicationId)
            .ToListAsync(ct);

        return Result.Success(scorecards.Select(ScorecardMappingHelper.MapToDto).ToList());
    }
}
