using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Auth.Commands;

[Authorize(Permissions = Permissions.UsersCreate)]
public record RegisterUserCommand(string Email, string Password, string Role) : IRequest<Result>;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IAuditService _auditService;
    private readonly IApplicationDbContext _context;

    public RegisterUserCommandHandler(IIdentityService identityService, IAuditService auditService, IApplicationDbContext context)
    {
        _identityService = identityService;
        _auditService = auditService;
        _context = context;
    }

    public async Task<Result> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var (result, userId) = await _identityService.CreateUserAsync(request.Email, request.Password);

        if (result.Succeeded)
        {
            var roleResult = await _identityService.AssignRoleAsync(userId, request.Role);
            if (!roleResult.Succeeded)
            {
                return roleResult;
            }

            await _auditService.LogAsync("user.registered", "user", userId, new { role = request.Role }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
