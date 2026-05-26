using FluentValidation;
using MediatR;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Requisitions.Commands.CloneRequisition;

[Authorize(Roles = Roles.HrAdmin + "," + Roles.HiringManager)]
public record CloneRequisitionCommand(Guid RequisitionId) : IRequest<Result<Guid>>;

public class CloneRequisitionCommandValidator : AbstractValidator<CloneRequisitionCommand>
{
    public CloneRequisitionCommandValidator()
    {
        RuleFor(v => v.RequisitionId).NotEmpty();
    }
}
