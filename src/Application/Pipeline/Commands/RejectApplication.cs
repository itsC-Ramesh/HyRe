using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Pipeline.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record RejectApplication(Guid ApplicationId, string? Reason) : IRequest<Result>;

public class RejectApplicationHandler : IRequestHandler<RejectApplication, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RejectApplicationHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result> Handle(RejectApplication request, CancellationToken ct)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        if (application is null)
            return Result.Failure("Application not found.");

        application.Reject(request.Reason, _user.Id);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
