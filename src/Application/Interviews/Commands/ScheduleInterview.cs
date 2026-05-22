using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record ScheduleInterview(
    Guid ApplicationId,
    string InterviewerId,
    InterviewType Type,
    DateTimeOffset ScheduledAt,
    int DurationMin,
    string? MeetingLink
) : IRequest<Result<Guid>>;

public class ScheduleInterviewHandler : IRequestHandler<ScheduleInterview, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IInterviewRepository _interviewRepository;
    private readonly IScorecardRepository _scorecardRepository;

    public ScheduleInterviewHandler(
        IApplicationDbContext context,
        IInterviewRepository interviewRepository,
        IScorecardRepository scorecardRepository)
    {
        _context = context;
        _interviewRepository = interviewRepository;
        _scorecardRepository = scorecardRepository;
    }

    public async Task<Result<Guid>> Handle(ScheduleInterview request, CancellationToken ct)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        if (application is null)
            return Result.Failure<Guid>("Application not found.");

        var interview = new Interview
        {
            ApplicationId = request.ApplicationId,
            InterviewerId = request.InterviewerId,
            Type = request.Type,
            ScheduledAt = request.ScheduledAt,
            DurationMin = request.DurationMin,
            MeetingLink = request.MeetingLink,
            Status = InterviewStatus.Scheduled
        };

        interview.Book();

        await _interviewRepository.AddAsync(interview, ct);

        // Auto-create scorecard for the interviewer
        var scorecard = new Scorecard
        {
            InterviewId = interview.Id,
            InterviewerId = request.InterviewerId,
            Strengths = string.Empty,
            Concerns = string.Empty
        };

        await _scorecardRepository.AddAsync(scorecard, ct);

        return Result.Success(interview.Id);
    }
}
