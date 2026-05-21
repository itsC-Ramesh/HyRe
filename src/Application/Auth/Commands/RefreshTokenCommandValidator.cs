namespace RC.HyRe.Application.Auth.Commands;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(v => v.AccessToken)
            .NotEmpty();

        RuleFor(v => v.RefreshToken)
            .NotEmpty();
    }
}
