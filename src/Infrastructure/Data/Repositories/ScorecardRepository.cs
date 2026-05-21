using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Infrastructure.Data;

namespace RC.HyRe.Infrastructure.Data.Repositories;

public class ScorecardRepository : IScorecardRepository
{
    private readonly ApplicationDbContext _context;

    public ScorecardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Scorecard?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Scorecards
            .AsNoTracking()
            .Include(s => s.Interview).ThenInclude(i => i.Application).ThenInclude(a => a.Candidate)
            .Include(s => s.Interview).ThenInclude(i => i.Application).ThenInclude(a => a.Requisition)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Scorecard?> GetByInterviewIdAsync(Guid interviewId, CancellationToken ct = default)
        => await _context.Scorecards
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.InterviewId == interviewId, ct);

    public async Task<PaginatedList<Scorecard>> GetByInterviewerAsync(
        string interviewerId, int page, int limit, CancellationToken ct = default)
    {
        var query = _context.Scorecards
            .Where(s => s.InterviewerId == interviewerId)
            .OrderByDescending(s => s.Created);

        return await PaginatedList<Scorecard>.CreateAsync(query, page, limit, ct);
    }

    public async Task<bool> AllSubmittedForApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => !await _context.Interviews
            .AnyAsync(i => i.ApplicationId == applicationId
                        && i.Status == InterviewStatus.Completed
                        && i.Scorecard == null, ct);
}
