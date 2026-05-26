using MediatR;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Notes.Commands.AddNote;

public class AddNoteCommandHandler(IApplicationDbContext db) : IRequestHandler<AddNoteCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddNoteCommand request, CancellationToken ct)
    {
        var note = new Note
        {
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            Content = request.Content
        };

        note.AddDomainEvent(new NoteCreatedEvent(note.EntityType, note.EntityId, note.Content));
        db.Notes.Add(note);
        await db.SaveChangesAsync(ct);

        return Result.Success(note.Id);
    }
}
