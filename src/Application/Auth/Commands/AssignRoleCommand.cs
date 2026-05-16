using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Auth.Commands;

[Authorize(Permissions = Permissions.UsersUpdate)]
public record AssignRoleCommand(string UserId, string Role) : IRequest<Result>;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IAuditService _auditService;
    private readonly IApplicationDbContext _context;

    public AssignRoleCommandHandler(IIdentityService identityService, IAuditService auditService, IApplicationDbContext context)
    {
        _identityService = identityService;
        _auditService = auditService;
        _context = context;
    }

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.AssignRoleAsync(request.UserId, request.Role);

        if (result.Succeeded)
        {
            await _auditService.LogAsync("user.role_assigned", "user", request.UserId, new { role = request.Role }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
