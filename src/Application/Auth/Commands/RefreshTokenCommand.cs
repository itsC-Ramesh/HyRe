using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Auth.Commands;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResult>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    private readonly IIdentityService _identityService;
    private readonly IApplicationDbContext _context;

    public RefreshTokenCommandHandler(IIdentityService identityService, IApplicationDbContext context)
    {
        _identityService = identityService;
        _context = context;
    }

    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);
        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }
}
