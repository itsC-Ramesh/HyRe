using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Pipeline.Queries;

[Authorize(Permissions = Permissions.PipelineRead)]
public record GetApplicationById(Guid Id) : IRequest<Result<ApplicationDetailDto>>;

public class GetApplicationByIdHandler : IRequestHandler<GetApplicationById, Result<ApplicationDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetApplicationByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ApplicationDetailDto>> Handle(GetApplicationById request, CancellationToken ct)
    {
        var application = await _context.Applications
            .AsNoTracking()
            .Include(a => a.Candidate)
            .Include(a => a.Requisition)
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct);

        if (application is null)
            return Result.Failure<ApplicationDetailDto>("Application not found.");

        var interviews = await _context.Interviews
            .AsNoTracking()
            .Where(i => i.ApplicationId == request.Id)
            .Include(i => i.Scorecard)
            .OrderByDescending(i => i.ScheduledAt)
            .Select(i => new ApplicationInterviewSummary(
                i.Id,
                i.InterviewerId,
                i.Type,
                i.ScheduledAt,
                i.Status,
                i.Scorecard != null))
            .ToListAsync(ct);

        var dto = new ApplicationDetailDto(
            application.Id,
            application.CandidateId,
            application.Candidate.Name,
            application.Candidate.Email,
            application.RequisitionId,
            application.Requisition.Title,
            application.Stage,
            application.RejectionReason,
            interviews,
            application.Created);

        return Result.Success(dto);
    }
}
