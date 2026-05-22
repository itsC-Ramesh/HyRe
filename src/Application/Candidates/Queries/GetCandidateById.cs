using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Candidates.Queries;

[Authorize(Permissions = Permissions.CandidatesRead)]
public record GetCandidateById(Guid Id) : IRequest<Result<CandidateDto>>;

public class GetCandidateByIdHandler : IRequestHandler<GetCandidateById, Result<CandidateDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCandidateByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CandidateDto>> Handle(GetCandidateById request, CancellationToken ct)
    {
        var candidate = await _context.Candidates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (candidate is null)
            return Result.Failure<CandidateDto>("Candidate not found.");

        var applications = await _context.Applications
            .AsNoTracking()
            .Where(a => a.CandidateId == request.Id)
            .Include(a => a.Requisition)
            .OrderByDescending(a => a.Created)
            .Select(a => new CandidateApplicationSummary(
                a.Id,
                a.RequisitionId,
                a.Requisition.Title,
                a.Stage,
                a.Created))
            .ToListAsync(ct);

        var dto = new CandidateDto(
            candidate.Id,
            candidate.Name,
            candidate.Email,
            candidate.Phone,
            candidate.Source,
            candidate.SourceDetail,
            candidate.ResumeDocId,
            applications,
            candidate.Created);

        return Result.Success(dto);
    }
}
