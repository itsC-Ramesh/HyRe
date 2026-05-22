using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record MarkCompleted(Guid InterviewId) : IRequest<Result>;

public class MarkCompletedHandler : IRequestHandler<MarkCompleted, Result>
{
    private readonly IApplicationDbContext _context;

    public MarkCompletedHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(MarkCompleted request, CancellationToken ct)
    {
        var interview = await _context.Interviews.FindAsync([request.InterviewId], ct);
        if (interview is null)
            return Result.Failure("Interview not found.");

        if (interview.Status != InterviewStatus.Scheduled)
            return Result.Failure("Only scheduled interviews can be marked as completed.");

        interview.Status = InterviewStatus.Completed;
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
