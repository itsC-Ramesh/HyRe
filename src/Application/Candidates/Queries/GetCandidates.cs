using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Candidates.Queries;

[Authorize(Permissions = Permissions.CandidatesRead)]
public record GetCandidates(
    string? NameFilter,
    int Page,
    int Limit
) : IRequest<Result<PaginatedList<CandidateDto>>>;

public class GetCandidatesHandler : IRequestHandler<GetCandidates, Result<PaginatedList<CandidateDto>>>
{
    private readonly ICandidateRepository _repository;
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetCandidatesHandler(
        ICandidateRepository repository,
        IApplicationDbContext context,
        IUser user)
    {
        _repository = repository;
        _context = context;
        _user = user;
    }

    public async Task<Result<PaginatedList<CandidateDto>>> Handle(GetCandidates request, CancellationToken ct)
    {
        var paged = await _repository.GetPagedAsync(
            request.NameFilter,
            _user.Id!,
            _user.Roles!.First(),
            request.Page,
            request.Limit,
            ct);

        var candidateIds = paged.Items.Select(c => c.Id).ToList();

        var applications = await _context.Applications
            .AsNoTracking()
            .Where(a => candidateIds.Contains(a.CandidateId))
            .Include(a => a.Requisition)
            .Select(a => new
            {
                a.CandidateId,
                ApplicationId = a.Id,
                a.RequisitionId,
                RequisitionTitle = a.Requisition.Title,
                a.Stage,
                a.Created
            })
            .ToListAsync(ct);

        var dtos = paged.Items.Select(c =>
        {
            var apps = applications
                .Where(a => a.CandidateId == c.Id)
                .Select(a => new CandidateApplicationSummary(
                    a.ApplicationId,
                    a.RequisitionId,
                    a.RequisitionTitle,
                    a.Stage,
                    a.Created))
                .ToList();

            return new CandidateDto(
                c.Id,
                c.Name,
                c.Email,
                c.Phone,
                c.Source,
                c.SourceDetail,
                c.ResumeDocId,
                apps,
                c.Created);
        }).ToList();

        return Result.Success(PaginatedList<CandidateDto>.Create(
            dtos, paged.TotalCount, paged.Page, paged.Limit));
    }
}
