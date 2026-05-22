using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record CancelInterview(Guid InterviewId, string? Reason) : IRequest<Result>;

public class CancelInterviewHandler : IRequestHandler<CancelInterview, Result>
{
    private readonly IInterviewRepository _repository;

    public CancelInterviewHandler(IInterviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(CancelInterview request, CancellationToken ct)
    {
        var interview = await _repository.GetByIdAsync(request.InterviewId, ct);
        if (interview is null)
            return Result.Failure("Interview not found.");

        if (interview.Status != InterviewStatus.Scheduled)
            return Result.Failure("Only scheduled interviews can be cancelled.");

        interview.Status = InterviewStatus.Cancelled;
        await _repository.UpdateAsync(interview, ct);
        return Result.Success();
    }
}
