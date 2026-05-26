using FluentValidation;
using MediatR;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Tags.Commands.DeleteTag;

[Authorize(Roles = Roles.HrAdmin)]
public record DeleteTagCommand(Guid TagId) : IRequest<Result>;

public class DeleteTagCommandValidator : AbstractValidator<DeleteTagCommand>
{
    public DeleteTagCommandValidator()
    {
        RuleFor(v => v.TagId).NotEmpty();
    }
}
