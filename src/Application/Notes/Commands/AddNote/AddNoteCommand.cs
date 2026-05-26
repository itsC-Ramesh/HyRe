using FluentValidation;
using MediatR;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Notes.Commands.AddNote;

[Authorize(Roles = Roles.HrAdmin + "," + Roles.HiringManager)]
public record AddNoteCommand(string EntityType, Guid EntityId, string Content) : IRequest<Result<Guid>>;

public class AddNoteCommandValidator : AbstractValidator<AddNoteCommand>
{
    public AddNoteCommandValidator()
    {
        RuleFor(v => v.EntityType).NotEmpty().MaximumLength(50);
        RuleFor(v => v.EntityId).NotEmpty();
        RuleFor(v => v.Content).NotEmpty();
    }
}
