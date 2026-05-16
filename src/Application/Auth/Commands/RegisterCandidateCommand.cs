using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Auth.Commands;

public record RegisterCandidateCommand(string Email, string Password) : IRequest<Result>;

public class RegisterCandidateCommandHandler : IRequestHandler<RegisterCandidateCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IAuditService _auditService;
    private readonly IApplicationDbContext _context;

    public RegisterCandidateCommandHandler(IIdentityService identityService, IAuditService auditService, IApplicationDbContext context)
    {
        _identityService = identityService;
        _auditService = auditService;
        _context = context;
    }

    public async Task<Result> Handle(RegisterCandidateCommand request, CancellationToken cancellationToken)
    {
        var (result, userId) = await _identityService.CreateUserAsync(request.Email, request.Password);

        if (result.Succeeded)
        {
            await _identityService.AssignRoleAsync(userId, Roles.Candidate);
            await _auditService.LogAsync("candidate.registered", "user", userId, null, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
