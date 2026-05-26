using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record CancelInterview(Guid InterviewId, string? Reason) : IRequest<Result>;

public class CancelInterviewHandler : IRequestHandler<CancelInterview, Result>
{
    private readonly IApplicationDbContext _context;

    public CancelInterviewHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(CancelInterview request, CancellationToken ct)
    {
        var interview = await _context.Interviews.FindAsync([request.InterviewId], ct);
        if (interview is null)
            return Result.Failure("Interview not found.");

        if (interview.Status != InterviewStatus.Scheduled)
            return Result.Failure("Only scheduled interviews can be cancelled.");

        interview.Status = InterviewStatus.Cancelled;
        interview.AddDomainEvent(new RC.HyRe.Domain.Events.InterviewCancelledEvent(interview.Id, interview.ApplicationId, interview.InterviewerId, request.Reason));
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
