using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Queries;

[Authorize(Permissions = Permissions.RequisitionsRead)]
public record GetRequisitions(
    RequisitionStatus? StatusFilter,
    string? DepartmentFilter,
    int Page,
    int Limit
) : IRequest<Result<PaginatedList<RequisitionDto>>>;

public class GetRequisitionsHandler : IRequestHandler<GetRequisitions, Result<PaginatedList<RequisitionDto>>>
{
    private readonly IRequisitionRepository _repository;
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetRequisitionsHandler(IRequisitionRepository repository, IApplicationDbContext context, IUser user)
    {
        _repository = repository;
        _context = context;
        _user = user;
    }

    public async Task<Result<PaginatedList<RequisitionDto>>> Handle(GetRequisitions request, CancellationToken ct)
    {
        var paged = await _repository.GetPagedAsync(
            request.StatusFilter,
            request.DepartmentFilter,
            _user.Id!,
            _user.Roles!.First(),
            request.Page,
            request.Limit,
            ct);

        var reqIds = paged.Items.Select(r => r.Id).ToList();

        var appCounts = await _context.Applications
            .Where(a => reqIds.Contains(a.RequisitionId))
            .GroupBy(a => new { a.RequisitionId, a.Stage })
            .Select(g => new { g.Key.RequisitionId, g.Key.Stage, Count = g.Count() })
            .ToListAsync(ct);

        var dtos = paged.Items.Select(r =>
        {
            var counts = appCounts
                .Where(ac => ac.RequisitionId == r.Id)
                .ToDictionary(ac => ac.Stage, ac => ac.Count);

            return new RequisitionDto(
                r.Id, r.Title, r.Department, r.OwnerId, r.JdText,
                r.SalaryMin, r.SalaryMax, r.Headcount, r.Status,
                counts, r.Created, r.LastModified);
        }).ToList();

        return Result.Success(PaginatedList<RequisitionDto>.Create(dtos, paged.TotalCount, paged.Page, paged.Limit));
    }
}
