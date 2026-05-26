using FluentValidation;
using MediatR;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Notes.Commands.DeleteNote;

[Authorize(Roles = Roles.HrAdmin + "," + Roles.HiringManager)]
public record DeleteNoteCommand(Guid NoteId) : IRequest<Result>;

public class DeleteNoteCommandValidator : AbstractValidator<DeleteNoteCommand>
{
    public DeleteNoteCommandValidator()
    {
        RuleFor(v => v.NoteId).NotEmpty();
    }
}
