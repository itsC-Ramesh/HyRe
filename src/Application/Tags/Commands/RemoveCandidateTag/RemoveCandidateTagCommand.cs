using FluentValidation;
using MediatR;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Tags.Commands.RemoveCandidateTag;

[Authorize(Roles = Roles.HrAdmin + "," + Roles.HiringManager + "," + Roles.Interviewer)]
public record RemoveCandidateTagCommand(Guid CandidateId, Guid TagId) : IRequest<Result>;

public class RemoveCandidateTagCommandValidator : AbstractValidator<RemoveCandidateTagCommand>
{
    public RemoveCandidateTagCommandValidator()
    {
        RuleFor(v => v.CandidateId).NotEmpty();
        RuleFor(v => v.TagId).NotEmpty();
    }
}
