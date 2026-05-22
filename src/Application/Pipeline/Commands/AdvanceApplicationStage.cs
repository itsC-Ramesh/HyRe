using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Pipeline.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record AdvanceApplicationStage(Guid ApplicationId, ApplicationStage NewStage) : IRequest<Result>;

public class AdvanceApplicationStageHandler : IRequestHandler<AdvanceApplicationStage, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public AdvanceApplicationStageHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result> Handle(AdvanceApplicationStage request, CancellationToken ct)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        if (application is null)
            return Result.Failure("Application not found.");

        if (request.NewStage == ApplicationStage.Rejected)
            return Result.Failure("Use the Reject command to reject an application.");

        application.AdvanceStage(request.NewStage, _user.Id);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
