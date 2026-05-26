using MediatR;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Notes.Commands.UpdateNote;

public class UpdateNoteCommandHandler(IApplicationDbContext db, IUser currentUser) : IRequestHandler<UpdateNoteCommand, Result>
{
    public async Task<Result> Handle(UpdateNoteCommand request, CancellationToken ct)
    {
        var note = await db.Notes.FindAsync(new object[] { request.NoteId }, ct);

        if (note == null)
            return Result.Failure("Note not found.");

        if (note.CreatedBy != currentUser.Id && (currentUser.Roles == null || !currentUser.Roles.Contains(RC.HyRe.Domain.Constants.Roles.HrAdmin)))
            return Result.Failure("Only the author or an HR Admin can update this note.");

        note.Content = request.Content;

        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
