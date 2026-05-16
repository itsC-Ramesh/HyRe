using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResult>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IIdentityService _identityService;
    private readonly IAuditService _auditService;
    private readonly IApplicationDbContext _context;

    public LoginCommandHandler(IIdentityService identityService, IAuditService auditService, IApplicationDbContext context)
    {
        _identityService = identityService;
        _auditService = auditService;
        _context = context;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.AuthenticateAsync(request.Email, request.Password);

        if (result.Succeeded)
        {
            await _auditService.LogAsync("user.login", "user", request.Email, new { success = true }, cancellationToken);
        }
        else
        {
            await _auditService.LogAsync("user.login_failed", "user", request.Email, new { reason = "invalid_credentials" }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }
}
