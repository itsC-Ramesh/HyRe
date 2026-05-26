using FluentValidation;
using MediatR;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Notes.Commands.UpdateNote;

[Authorize(Roles = Roles.HrAdmin + "," + Roles.HiringManager)]
public record UpdateNoteCommand(Guid NoteId, string Content) : IRequest<Result>;

public class UpdateNoteCommandValidator : AbstractValidator<UpdateNoteCommand>
{
    public UpdateNoteCommandValidator()
    {
        RuleFor(v => v.NoteId).NotEmpty();
        RuleFor(v => v.Content).NotEmpty();
    }
}
