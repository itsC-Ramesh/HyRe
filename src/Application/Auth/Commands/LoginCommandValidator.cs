namespace RC.HyRe.Application.Auth.Commands;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(v => v.Password)
            .NotEmpty();
    }
}
