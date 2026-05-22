using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Infrastructure.Data;

namespace RC.HyRe.Infrastructure.Data.Repositories;

public class InterviewRepository : IInterviewRepository
{
    private readonly ApplicationDbContext _context;

    public InterviewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Interview?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Interviews
            .AsNoTracking()
            .Include(i => i.Application).ThenInclude(a => a.Candidate)
            .Include(i => i.Application).ThenInclude(a => a.Requisition)
            .Include(i => i.Scorecard)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<PaginatedList<Interview>> GetByApplicationAsync(
        Guid applicationId, int page, int limit, CancellationToken ct = default)
    {
        var query = _context.Interviews
            .Where(i => i.ApplicationId == applicationId)
            .OrderByDescending(i => i.ScheduledAt);

        return await PaginatedList<Interview>.CreateAsync(query, page, limit, ct);
    }

    public async Task<PaginatedList<Interview>> GetByInterviewerAsync(
        string interviewerId, InterviewStatus? statusFilter, int page, int limit, CancellationToken ct = default)
    {
        var query = _context.Interviews
            .Where(i => i.InterviewerId == interviewerId);

        if (statusFilter.HasValue)
            query = query.Where(i => i.Status == statusFilter.Value);

        query = query.OrderByDescending(i => i.ScheduledAt);

        return await PaginatedList<Interview>.CreateAsync(query, page, limit, ct);
    }

    public async Task<IReadOnlyList<Interview>> GetUpcomingAsync(TimeSpan within, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _context.Interviews
            .AsNoTracking()
            .Include(i => i.Application).ThenInclude(a => a.Candidate)
            .Include(i => i.Application).ThenInclude(a => a.Requisition)
            .Where(i => i.Status == InterviewStatus.Scheduled
                     && i.ScheduledAt > now
                     && i.ScheduledAt <= now + within)
            .OrderBy(i => i.ScheduledAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Interview>> GetPastDueForScorecardAsync(CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromHours(24);

        return await _context.Interviews
            .AsNoTracking()
            .Include(i => i.Application).ThenInclude(a => a.Candidate)
            .Include(i => i.Application).ThenInclude(a => a.Requisition)
            .Where(i => i.Status == InterviewStatus.Completed
                     && i.Scorecard == null
                     && i.ScheduledAt < cutoff)
            .OrderBy(i => i.ScheduledAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Interview interview, CancellationToken ct = default)
    {
        _context.Interviews.Add(interview);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Interview interview, CancellationToken ct = default)
    {
        _context.Interviews.Update(interview);
        await _context.SaveChangesAsync(ct);
    }
}
