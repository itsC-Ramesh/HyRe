using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Infrastructure.Data;

namespace RC.HyRe.Infrastructure.Data.Repositories;

public class CandidateRepository : ICandidateRepository
{
    private readonly ApplicationDbContext _context;

    public CandidateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Candidates
            .Include(c => c.ResumeDocument)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Candidate?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.Candidates
            .FirstOrDefaultAsync(c => c.Email == email.ToLowerInvariant(), cancellationToken);

    public async Task<PaginatedList<Candidate>> GetPagedAsync(
        string? nameFilter,
        string userId,
        string userRole,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // RBAC scoping at query level — never fetch-all-then-filter
        // HR_ADMIN and HIRING_MANAGER can see all candidates (for now)
        // CANDIDATE role sees only their own record (single self-service lookup elsewhere)
        // INTERVIEWER sees only candidates assigned to their interviews
        IQueryable<Candidate> query = userRole switch
        {
            Roles.Interviewer => _context.Candidates
                .Where(c => c.Applications
                    .Any(a => a.Interviews
                        .Any(i => i.InterviewerId == userId))),
            _ => _context.Candidates
        };

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            var lower = nameFilter.ToLowerInvariant();
            query = query.Where(c => c.Name.ToLower().Contains(lower)
                                  || c.Email.ToLower().Contains(lower));
        }

        query = query.OrderByDescending(c => c.Created);

        return await PaginatedList<Candidate>.CreateAsync(query, page, limit, cancellationToken);
    }

    public async Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.Candidates
            .AnyAsync(c => c.Email == email.ToLowerInvariant(), cancellationToken);

    public async Task AddAsync(Candidate candidate, CancellationToken ct = default)
    {
        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Candidate candidate, CancellationToken ct = default)
    {
        _context.Candidates.Update(candidate);
        await _context.SaveChangesAsync(ct);
    }
}
