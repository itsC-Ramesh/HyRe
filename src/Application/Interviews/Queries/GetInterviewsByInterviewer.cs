using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Queries;

[Authorize(Roles = $"{Roles.Interviewer},{Roles.HiringManager},{Roles.HrAdmin}")]
public record GetInterviewsByInterviewer(
    InterviewStatus? StatusFilter,
    int Page,
    int Limit
) : IRequest<Result<PaginatedList<InterviewDto>>>;

public class GetInterviewsByInterviewerHandler
    : IRequestHandler<GetInterviewsByInterviewer, Result<PaginatedList<InterviewDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetInterviewsByInterviewerHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<PaginatedList<InterviewDto>>> Handle(
        GetInterviewsByInterviewer request, CancellationToken ct)
    {
        var query = _context.Interviews
            .AsNoTracking()
            .Where(i => i.InterviewerId == _user.Id);

        if (request.StatusFilter.HasValue)
            query = query.Where(i => i.Status == request.StatusFilter.Value);

        query = query
            .Include(i => i.Application).ThenInclude(a => a.Candidate)
            .Include(i => i.Application).ThenInclude(a => a.Requisition)
            .Include(i => i.Scorecard)
            .OrderByDescending(i => i.ScheduledAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .Select(i => new InterviewDto(
                i.Id,
                i.ApplicationId,
                i.Application.Candidate.Name,
                i.Application.Requisition.Title,
                i.InterviewerId,
                i.Type,
                i.ScheduledAt,
                i.DurationMin,
                i.Status,
                i.MeetingLink,
                i.Scorecard != null))
            .ToListAsync(ct);

        return Result.Success(PaginatedList<InterviewDto>.Create(items, totalCount, request.Page, request.Limit));
    }
}
