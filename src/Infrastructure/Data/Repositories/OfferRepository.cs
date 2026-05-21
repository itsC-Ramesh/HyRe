using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Infrastructure.Data;

namespace RC.HyRe.Infrastructure.Data.Repositories;

public class OfferRepository : IOfferRepository
{
    private readonly ApplicationDbContext _context;

    public OfferRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Offer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Offers
            .AsNoTracking()
            .Include(o => o.Application).ThenInclude(a => a.Candidate)
            .Include(o => o.Application).ThenInclude(a => a.Requisition)
            .Include(o => o.LetterDocument)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Offer?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default)
        => await _context.Offers
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.ApplicationId == applicationId, ct);

    public async Task<PaginatedList<Offer>> GetPagedAsync(
        OfferStatus? statusFilter, string userId, string userRole, int page, int limit, CancellationToken ct = default)
    {
        IQueryable<Offer> query = userRole switch
        {
            Roles.HrAdmin or Roles.Administrator => _context.Offers,
            Roles.HiringManager => _context.Offers
                .Where(o => o.Application.Requisition.OwnerId == userId),
            _ => _context.Offers.Where(_ => false)
        };

        if (statusFilter.HasValue)
            query = query.Where(o => o.Status == statusFilter.Value);

        query = query.OrderByDescending(o => o.Created);

        return await PaginatedList<Offer>.CreateAsync(query, page, limit, ct);
    }

    public async Task<IReadOnlyList<Offer>> GetPendingApprovalAsync(TimeSpan olderThan, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow - olderThan;

        return await _context.Offers
            .AsNoTracking()
            .Include(o => o.Application).ThenInclude(a => a.Candidate)
            .Include(o => o.Application).ThenInclude(a => a.Requisition)
            .Where(o => o.Status == OfferStatus.PendingApproval && o.Created < cutoff)
            .OrderBy(o => o.Created)
            .ToListAsync(ct);
    }
}
