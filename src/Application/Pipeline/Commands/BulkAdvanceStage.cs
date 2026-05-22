using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Pipeline.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record BulkAdvanceStage(List<Guid> ApplicationIds, ApplicationStage NewStage) : IRequest<Result<int>>;

public class BulkAdvanceStageHandler : IRequestHandler<BulkAdvanceStage, Result<int>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public BulkAdvanceStageHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<int>> Handle(BulkAdvanceStage request, CancellationToken ct)
    {
        if (request.NewStage == ApplicationStage.Rejected)
            return Result.Failure<int>("Use RejectApplication for rejections.");

        var applications = await _context.Applications
            .Where(a => request.ApplicationIds.Contains(a.Id))
            .ToListAsync(ct);

        if (applications.Count != request.ApplicationIds.Count)
            return Result.Failure<int>("One or more applications not found.");

        foreach (var app in applications)
        {
            app.AdvanceStage(request.NewStage, _user.Id);
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success(applications.Count);
    }
}
