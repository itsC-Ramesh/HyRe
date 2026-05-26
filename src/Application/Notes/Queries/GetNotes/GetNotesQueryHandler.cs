using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Notes.Queries.GetNotes;

public class GetNotesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetNotesQuery, Result<PaginatedList<NoteDto>>>
{
    public async Task<Result<PaginatedList<NoteDto>>> Handle(GetNotesQuery request, CancellationToken ct)
    {
        var query = db.Notes.AsNoTracking()
            .Where(n => n.EntityType == request.EntityType && n.EntityId == request.EntityId)
            .OrderByDescending(n => n.Created)
            .Select(n => new NoteDto(n.Id, n.EntityType, n.EntityId, n.Content, n.CreatedBy ?? "System", n.Created, n.LastModified));

        return Result.Success(await PaginatedList<NoteDto>.CreateAsync(query, request.PageNumber, request.PageSize, ct));
    }
}
