using FluentValidation;
using MediatR;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Tags.Commands.AssignCandidateTag;

[Authorize(Roles = Roles.HrAdmin + "," + Roles.HiringManager + "," + Roles.Interviewer)]
public record AssignCandidateTagCommand(Guid CandidateId, Guid TagId) : IRequest<Result>;

public class AssignCandidateTagCommandValidator : AbstractValidator<AssignCandidateTagCommand>
{
    public AssignCandidateTagCommandValidator()
    {
        RuleFor(v => v.CandidateId).NotEmpty();
        RuleFor(v => v.TagId).NotEmpty();
    }
}
