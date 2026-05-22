using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Entities;

namespace RC.HyRe.Application.Common.Interfaces.Repositories;

/// <summary>
/// All queries that return lists of candidates apply RBAC scoping at the DB level.
/// Never fetch all rows and filter in-process.
/// </summary>
public interface ICandidateRepository
{
    Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Candidate?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Returns a page of candidates, scoped by the caller's role.</summary>
    Task<PaginatedList<Candidate>> GetPagedAsync(
        string? nameFilter,
        string userId,
        string userRole,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(Candidate candidate, CancellationToken ct = default);
    Task UpdateAsync(Candidate candidate, CancellationToken ct = default);
}
