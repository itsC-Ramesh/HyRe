using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace RC.HyRe.Infrastructure.Services;

public class EventLogService : IEventLogService
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public EventLogService(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<PaginatedList<EventLogDto>> GetTimelineAsync(string entityType, Guid entityId, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var query = _context.EventLogs
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.Created);

        var count = await query.CountAsync(ct);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var dtos = new List<EventLogDto>();
        foreach (var item in items)
        {
            string? actorName = null;
            if (!string.IsNullOrEmpty(item.ActorId))
            {
                actorName = await _identityService.GetUserNameAsync(item.ActorId);
            }

            dtos.Add(new EventLogDto(
                item.Id,
                item.EntityType,
                item.EntityId,
                item.Action,
                item.ActorId,
                actorName,
                item.PayloadJson,
                item.Created
            ));
        }

        return PaginatedList<EventLogDto>.Create(dtos, count, pageNumber, pageSize);
    }

    public async Task<PaginatedList<EventLogDto>> GetEventLogByActionAsync(string action, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var query = _context.EventLogs
            .Where(e => e.Action == action)
            .OrderByDescending(e => e.Created);

        var count = await query.CountAsync(ct);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var dtos = new List<EventLogDto>();
        foreach (var item in items)
        {
            string? actorName = null;
            if (!string.IsNullOrEmpty(item.ActorId))
            {
                actorName = await _identityService.GetUserNameAsync(item.ActorId);
            }

            dtos.Add(new EventLogDto(
                item.Id,
                item.EntityType,
                item.EntityId,
                item.Action,
                item.ActorId,
                actorName,
                item.PayloadJson,
                item.Created
            ));
        }

        return PaginatedList<EventLogDto>.Create(dtos, count, pageNumber, pageSize);
    }
}
