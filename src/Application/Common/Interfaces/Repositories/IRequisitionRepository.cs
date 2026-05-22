using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Common.Interfaces.Repositories;

public interface IRequisitionRepository
{
    Task<Requisition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of requisitions.
    /// HR_ADMIN and EXECUTIVE see all; HIRING_MANAGER sees only their own (OwnerId = userId).
    /// </summary>
    Task<PaginatedList<Requisition>> GetPagedAsync(
        RequisitionStatus? statusFilter,
        string? departmentFilter,
        string userId,
        string userRole,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    Task AddAsync(Requisition requisition, CancellationToken ct = default);
    Task UpdateAsync(Requisition requisition, CancellationToken ct = default);
}
