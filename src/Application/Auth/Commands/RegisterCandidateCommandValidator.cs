namespace RC.HyRe.Application.Auth.Commands;

public class RegisterCandidateCommandValidator : AbstractValidator<RegisterCandidateCommand>
{
    public RegisterCandidateCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(v => v.Password)
            .StrongPassword();
    }
}
