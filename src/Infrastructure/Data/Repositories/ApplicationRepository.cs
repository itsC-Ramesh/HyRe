using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Infrastructure.Data;

namespace RC.HyRe.Infrastructure.Data.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Requisition)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<PaginatedList<JobApplication>> GetByRequisitionAsync(
        Guid requisitionId,
        string userId,
        string userRole,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // Hiring managers must own the requisition to see its applications
        IQueryable<JobApplication> query = userRole switch
        {
            Roles.HiringManager => _context.Applications
                .Where(a => a.RequisitionId == requisitionId
                         && a.Requisition.OwnerId == userId),
            _ => _context.Applications
                .Where(a => a.RequisitionId == requisitionId)
        };

        query = query
            .Include(a => a.Candidate)
            .OrderByDescending(a => a.Created);

        return await PaginatedList<JobApplication>.CreateAsync(query, page, limit, cancellationToken);
    }

    public async Task<bool> ExistsDuplicateAsync(
        Guid candidateId,
        Guid requisitionId,
        CancellationToken cancellationToken = default)
        => await _context.Applications
            .AnyAsync(a => a.CandidateId == candidateId
                        && a.RequisitionId == requisitionId,
                      cancellationToken);

    public async Task AddAsync(JobApplication application, CancellationToken ct = default)
    {
        _context.Applications.Add(application);
        await _context.SaveChangesAsync(ct);
    }
}
