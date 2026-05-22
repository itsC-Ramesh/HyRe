using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Pipeline.Queries;

[Authorize(Permissions = Permissions.PipelineRead)]
public record GetPipelineByRequisition(Guid RequisitionId) : IRequest<Result<PipelineDto>>;

public class GetPipelineByRequisitionHandler : IRequestHandler<GetPipelineByRequisition, Result<PipelineDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPipelineByRequisitionHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PipelineDto>> Handle(GetPipelineByRequisition request, CancellationToken ct)
    {
        var requisition = await _context.Requisitions
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RequisitionId, ct);

        if (requisition is null)
            return Result.Failure<PipelineDto>("Requisition not found.");

        var applications = await _context.Applications
            .AsNoTracking()
            .Where(a => a.RequisitionId == request.RequisitionId && a.Stage != ApplicationStage.Rejected)
            .Include(a => a.Candidate)
            .OrderByDescending(a => a.Created)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var stages = Enum.GetValues<ApplicationStage>()
            .Where(s => s != ApplicationStage.Rejected)
            .Select(stage =>
            {
                var cards = applications
                    .Where(a => a.Stage == stage)
                    .Select(a => new PipelineApplicationCard(
                        a.Id,
                        a.CandidateId,
                        a.Candidate.Name,
                        a.Candidate.Email,
                        a.Stage,
                        (int)(now - a.LastModified).TotalDays,
                        a.Created))
                    .ToList();

                return new PipelineStageGroup(stage, cards);
            })
            .ToList();

        return Result.Success(new PipelineDto(requisition.Id, requisition.Title, stages));
    }
}
