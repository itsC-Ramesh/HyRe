using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Scorecards.Queries;

[Authorize(Roles = $"{Roles.Interviewer},{Roles.HiringManager},{Roles.HrAdmin}")]
public record GetScorecardsByInterviewer(
    int Page,
    int Limit
) : IRequest<Result<PaginatedList<ScorecardDto>>>;

public class GetScorecardsByInterviewerHandler
    : IRequestHandler<GetScorecardsByInterviewer, Result<PaginatedList<ScorecardDto>>>
{
    private readonly IScorecardRepository _repository;
    private readonly IUser _user;

    public GetScorecardsByInterviewerHandler(IScorecardRepository repository, IUser user)
    {
        _repository = repository;
        _user = user;
    }

    public async Task<Result<PaginatedList<ScorecardDto>>> Handle(
        GetScorecardsByInterviewer request, CancellationToken ct)
    {
        var paged = await _repository.GetByInterviewerAsync(
            _user.Id!, request.Page, request.Limit, ct);

        var dtos = paged.Items
            .Select(ScorecardMappingHelper.MapToDto)
            .ToList();

        return Result.Success(PaginatedList<ScorecardDto>.Create(
            dtos, paged.TotalCount, paged.Page, paged.Limit));
    }
}
