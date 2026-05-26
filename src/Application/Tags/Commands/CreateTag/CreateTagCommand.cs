using FluentValidation;
using MediatR;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Tags.Commands.CreateTag;

[Authorize(Roles = Roles.HrAdmin)]
public record CreateTagCommand(string Name, string? Color) : IRequest<Result<Guid>>;

public class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(v => v.Name).NotEmpty().MaximumLength(100);
        RuleFor(v => v.Color).MaximumLength(20);
    }
}
