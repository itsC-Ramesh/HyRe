using MediatR;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Notes.Queries.GetNotes;

public record GetNotesQuery(string EntityType, Guid EntityId, int PageNumber = 1, int PageSize = 20) : IRequest<Result<PaginatedList<NoteDto>>>;

public record NoteDto(Guid Id, string EntityType, Guid EntityId, string Content, string CreatedBy, DateTimeOffset CreatedAt, DateTimeOffset LastModified);
