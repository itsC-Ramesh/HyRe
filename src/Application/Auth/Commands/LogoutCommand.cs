using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Auth.Commands;

public record LogoutCommand(string RefreshToken) : IRequest<Result>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IAuditService _auditService;
    private readonly IUser _user;
    private readonly IApplicationDbContext _context;

    public LogoutCommandHandler(IIdentityService identityService, IAuditService auditService, IUser user, IApplicationDbContext context)
    {
        _identityService = identityService;
        _auditService = auditService;
        _user = user;
        _context = context;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.RevokeRefreshTokenAsync(request.RefreshToken);

        if (result.Succeeded && _user.Id != null)
        {
            await _auditService.LogAsync("user.logout", "user", _user.Id, null, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }
}
