using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Entities;

namespace RC.HyRe.Application.Common.Interfaces.Repositories;

public interface IApplicationRepository
{
    Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all applications for a requisition, scoped to the caller's role.
    /// HIRING_MANAGER must own the requisition; HR_ADMIN sees all.
    /// </summary>
    Task<PaginatedList<JobApplication>> GetByRequisitionAsync(
        Guid requisitionId,
        string userId,
        string userRole,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>Returns true if an active application already exists for this pair.</summary>
    Task<bool> ExistsDuplicateAsync(
        Guid candidateId,
        Guid requisitionId,
        CancellationToken cancellationToken = default);
}
