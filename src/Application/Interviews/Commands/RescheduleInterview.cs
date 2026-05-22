using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record RescheduleInterview(Guid InterviewId, DateTimeOffset NewScheduledAt) : IRequest<Result>;

public class RescheduleInterviewHandler : IRequestHandler<RescheduleInterview, Result>
{
    private readonly IApplicationDbContext _context;

    public RescheduleInterviewHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(RescheduleInterview request, CancellationToken ct)
    {
        var interview = await _context.Interviews.FindAsync([request.InterviewId], ct);
        if (interview is null)
            return Result.Failure("Interview not found.");

        if (interview.Status != InterviewStatus.Scheduled)
            return Result.Failure("Only scheduled interviews can be rescheduled.");

        interview.ScheduledAt = request.NewScheduledAt;
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
