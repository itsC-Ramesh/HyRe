using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Common.Interfaces.Repositories;

public interface IOfferRepository
{
    Task<Offer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Offer?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default);
    Task<PaginatedList<Offer>> GetPagedAsync(OfferStatus? statusFilter, string userId, string userRole, int page, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<Offer>> GetPendingApprovalAsync(TimeSpan olderThan, CancellationToken ct = default);
}
