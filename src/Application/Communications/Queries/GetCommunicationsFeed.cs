using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Communications.Queries;

public record GetCommunicationsFeed(Guid EntityId) : IRequest<Result<List<CommunicationItemDto>>>;

public record CommunicationItemDto(
    string Type,
    Guid Id,
    string Content,
    string? AuthorId,
    DateTimeOffset CreatedAt,
    string? Metadata
);

public class GetCommunicationsFeedHandler : IRequestHandler<GetCommunicationsFeed, Result<List<CommunicationItemDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetCommunicationsFeedHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CommunicationItemDto>>> Handle(GetCommunicationsFeed request, CancellationToken ct)
    {
        var logs = await _context.EventLogs
            .AsNoTracking()
            .Where(e => e.EntityId == request.EntityId)
            .Select(e => new CommunicationItemDto(
                "EventLog",
                e.Id,
                e.Action,
                e.ActorId,
                e.Created,
                e.PayloadJson
            ))
            .ToListAsync(ct);

        var notes = await _context.Notes
            .AsNoTracking()
            .Where(n => n.EntityId == request.EntityId)
            .Select(n => new CommunicationItemDto(
                "Note",
                n.Id,
                n.Content,
                n.CreatedBy,
                n.Created,
                null
            ))
            .ToListAsync(ct);

        var combined = logs.Concat(notes)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return Result.Success(combined);
    }
}
