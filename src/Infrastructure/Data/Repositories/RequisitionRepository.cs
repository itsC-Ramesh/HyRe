using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Infrastructure.Data;

namespace RC.HyRe.Infrastructure.Data.Repositories;

public class RequisitionRepository : IRequisitionRepository
{
    private readonly ApplicationDbContext _context;

    public RequisitionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Requisition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Requisitions
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<PaginatedList<Requisition>> GetPagedAsync(
        RequisitionStatus? statusFilter,
        string? departmentFilter,
        string userId,
        string userRole,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // RBAC scoping: hiring managers only see their own requisitions
        IQueryable<Requisition> query = userRole switch
        {
            Roles.HiringManager => _context.Requisitions.Where(r => r.OwnerId == userId),
            _ => _context.Requisitions   // HR_ADMIN and EXECUTIVE see all
        };

        if (statusFilter.HasValue)
            query = query.Where(r => r.Status == statusFilter.Value);

        if (!string.IsNullOrWhiteSpace(departmentFilter))
            query = query.Where(r => r.Department == departmentFilter);

        query = query.OrderByDescending(r => r.Created);

        return await PaginatedList<Requisition>.CreateAsync(query, page, limit, cancellationToken);
    }
}
