using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record RescheduleInterview(Guid InterviewId, DateTimeOffset NewScheduledAt) : IRequest<Result>;

public class RescheduleInterviewHandler : IRequestHandler<RescheduleInterview, Result>
{
    private readonly IInterviewRepository _repository;

    public RescheduleInterviewHandler(IInterviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RescheduleInterview request, CancellationToken ct)
    {
        var interview = await _repository.GetByIdAsync(request.InterviewId, ct);
        if (interview is null)
            return Result.Failure("Interview not found.");

        if (interview.Status != InterviewStatus.Scheduled)
            return Result.Failure("Only scheduled interviews can be rescheduled.");

        interview.ScheduledAt = request.NewScheduledAt;
        await _repository.UpdateAsync(interview, ct);
        return Result.Success();
    }
}
