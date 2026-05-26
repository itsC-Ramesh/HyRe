using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
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
    string? MeetingLink,
    List<string>? PanelMemberIds = null
) : IRequest<Result<Guid>>;

public class ScheduleInterviewHandler : IRequestHandler<ScheduleInterview, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public ScheduleInterviewHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(ScheduleInterview request, CancellationToken ct)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        if (application is null)
            return Result.Failure<Guid>("Application not found.");

        // Check for scheduling conflicts with main interviewer
        var requestedEnd = request.ScheduledAt.AddMinutes(request.DurationMin);
        var hasConflict = await _context.Interviews
            .AnyAsync(i =>
                i.InterviewerId == request.InterviewerId &&
                i.Status != InterviewStatus.Cancelled &&
                i.ScheduledAt < requestedEnd &&
                i.ScheduledAt.AddMinutes(i.DurationMin) > request.ScheduledAt,
            ct);

        if (hasConflict)
            return Result.Failure<Guid>("Interviewer has a conflicting interview at this time.");

        // Check panel members for conflicts
        if (request.PanelMemberIds?.Count > 0)
        {
            foreach (var panelId in request.PanelMemberIds)
            {
                if (panelId == request.InterviewerId) continue;

                var panelConflict = await _context.Interviews
                    .AnyAsync(i =>
                        i.InterviewerId == panelId &&
                        i.Status != InterviewStatus.Cancelled &&
                        i.ScheduledAt < requestedEnd &&
                        i.ScheduledAt.AddMinutes(i.DurationMin) > request.ScheduledAt,
                    ct);

                if (panelConflict)
                    return Result.Failure<Guid>($"Panel member {panelId} has a conflicting interview.");
            }
        }

        var interview = new Interview
        {
            ApplicationId = request.ApplicationId,
            InterviewerId = request.InterviewerId,
            Type = request.Type,
            ScheduledAt = request.ScheduledAt,
            DurationMin = request.DurationMin,
            MeetingLink = request.MeetingLink,
            PanelMemberIds = request.PanelMemberIds ?? new(),
            Status = InterviewStatus.Scheduled
        };

        interview.Book();

        _context.Interviews.Add(interview);

        // Mark matching availability slot as booked
        var matchingSlot = await _context.InterviewerAvailabilities
            .FirstOrDefaultAsync(a =>
                a.InterviewerId == request.InterviewerId &&
                !a.IsBooked &&
                a.StartTime <= request.ScheduledAt &&
                a.EndTime >= request.ScheduledAt.AddMinutes(request.DurationMin),
            ct);

        if (matchingSlot != null)
            matchingSlot.IsBooked = true;

        // Auto-create scorecard for the main interviewer
        var scorecard = new Scorecard
        {
            InterviewId = interview.Id,
            InterviewerId = request.InterviewerId,
            Strengths = string.Empty,
            Concerns = string.Empty
        };

        _context.Scorecards.Add(scorecard);

        // Auto-create scorecard for each panel member
        if (request.PanelMemberIds != null)
        {
            foreach (var panelMemberId in request.PanelMemberIds)
            {
                // Skip if same as main interviewer
                if (panelMemberId == request.InterviewerId) continue;
                
                _context.Scorecards.Add(new Scorecard
                {
                    InterviewId = interview.Id,
                    InterviewerId = panelMemberId,
                    Strengths = string.Empty,
                    Concerns = string.Empty
                });
            }
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success(interview.Id);
    }
}
