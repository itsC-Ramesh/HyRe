using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record MarkNoShow(Guid InterviewId) : IRequest<Result>;

public class MarkNoShowHandler : IRequestHandler<MarkNoShow, Result>
{
    private readonly IInterviewRepository _repository;

    public MarkNoShowHandler(IInterviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(MarkNoShow request, CancellationToken ct)
    {
        var interview = await _repository.GetByIdAsync(request.InterviewId, ct);
        if (interview is null)
            return Result.Failure("Interview not found.");

        if (interview.Status != InterviewStatus.Scheduled)
            return Result.Failure("Only scheduled interviews can be marked as no-show.");

        interview.Status = InterviewStatus.NoShow;
        await _repository.UpdateAsync(interview, ct);
        return Result.Success();
    }
}
