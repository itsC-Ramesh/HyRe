namespace RC.HyRe.Application.Auth.Commands;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(v => v.RefreshToken)
            .NotEmpty();
    }
}
