using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Common.Interfaces;

public interface IEventLogService
{
    Task<PaginatedList<EventLogDto>> GetTimelineAsync(string entityType, Guid entityId, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);
    Task<PaginatedList<EventLogDto>> GetEventLogByActionAsync(string action, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);
}

public record EventLogDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? ActorId,
    string? ActorName,
    string? PayloadJson,
    DateTimeOffset Created
);
