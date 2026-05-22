using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Interviews.Queries;

[Authorize(Permissions = Permissions.PipelineRead)]
public record GetInterviewsByApplication(Guid ApplicationId, int Page, int Limit)
    : IRequest<Result<PaginatedList<InterviewDto>>>;

public class GetInterviewsByApplicationHandler
    : IRequestHandler<GetInterviewsByApplication, Result<PaginatedList<InterviewDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetInterviewsByApplicationHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<InterviewDto>>> Handle(
        GetInterviewsByApplication request, CancellationToken ct)
    {
        var query = _context.Interviews
            .AsNoTracking()
            .Where(i => i.ApplicationId == request.ApplicationId)
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
