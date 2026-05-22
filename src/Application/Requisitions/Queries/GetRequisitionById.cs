using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Queries;

[Authorize(Permissions = Permissions.RequisitionsRead)]
public record GetRequisitionById(Guid Id) : IRequest<Result<RequisitionDto>>;

public class GetRequisitionByIdHandler : IRequestHandler<GetRequisitionById, Result<RequisitionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRequisitionByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<RequisitionDto>> Handle(GetRequisitionById request, CancellationToken ct)
    {
        var requisition = await _context.Requisitions
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct);

        if (requisition is null)
            return Result.Failure<RequisitionDto>("Requisition not found.");

        var stageCounts = await _context.Applications
            .Where(a => a.RequisitionId == request.Id)
            .GroupBy(a => a.Stage)
            .Select(g => new { Stage = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Stage, x => x.Count, ct);

        var dto = new RequisitionDto(
            requisition.Id,
            requisition.Title,
            requisition.Department,
            requisition.OwnerId,
            requisition.JdText,
            requisition.SalaryMin,
            requisition.SalaryMax,
            requisition.Headcount,
            requisition.Status,
            stageCounts,
            requisition.Created,
            requisition.LastModified);

        return Result.Success(dto);
    }
}
