using MediatR;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Notes.Commands.DeleteNote;

public class DeleteNoteCommandHandler(IApplicationDbContext db, IUser currentUser) : IRequestHandler<DeleteNoteCommand, Result>
{
    public async Task<Result> Handle(DeleteNoteCommand request, CancellationToken ct)
    {
        var note = await db.Notes.FindAsync(new object[] { request.NoteId }, ct);

        if (note == null)
            return Result.Failure("Note not found.");

        if (note.CreatedBy != currentUser.Id && (currentUser.Roles == null || !currentUser.Roles.Contains(RC.HyRe.Domain.Constants.Roles.HrAdmin)))
            return Result.Failure("Only the author or an HR Admin can delete this note.");

        db.Notes.Remove(note);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
