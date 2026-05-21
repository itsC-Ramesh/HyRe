using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Auth.Commands;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(v => v.Password)
            .StrongPassword();

        RuleFor(v => v.Role)
            .NotEmpty()
            .Must(role => Roles.AllRolesArray.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.AllRolesArray)}");
    }
}
