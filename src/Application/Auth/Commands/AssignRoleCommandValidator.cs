using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Auth.Commands;

public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(v => v.UserId)
            .NotEmpty();

        RuleFor(v => v.Role)
            .NotEmpty()
            .Must(role => Roles.AllRolesArray.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.AllRolesArray)}");
    }
}
