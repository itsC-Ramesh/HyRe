using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Notifications.EventHandlers;

public class InterviewBookedEventHandler : INotificationHandler<InterviewBookedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IBackgroundJobService _jobService;

    public InterviewBookedEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IEmailService emailService,
        IBackgroundJobService jobService)
    {
        _context = context;
        _notificationService = notificationService;
        _emailService = emailService;
        _jobService = jobService;
    }

    public async Task Handle(InterviewBookedEvent notification, CancellationToken cancellationToken)
    {
        var interview = await _context.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a.Candidate)
            .Include(i => i.Application)
                .ThenInclude(a => a.Requisition)
            .FirstOrDefaultAsync(i => i.Id == notification.InterviewId, cancellationToken);

        if (interview == null) return;

        // 1. Create in-app notification for the interviewer
        var payload = new
        {
            InterviewId = interview.Id,
            ApplicationId = interview.ApplicationId,
            CandidateName = interview.Application.Candidate.Name,
            RequisitionTitle = interview.Application.Requisition.Title,
            ScheduledAt = interview.ScheduledAt,
            InterviewType = interview.Type.ToString()
        };

        await _notificationService.CreateNotificationAsync(
            interview.InterviewerId,
            "interview.booked",
            payload,
            cancellationToken);

        // 2. Send email to the candidate
        var subject = $"Interview Scheduled - {interview.Application.Requisition.Title}";
        var body = $@"Hi {interview.Application.Candidate.Name},

Your interview for the {interview.Application.Requisition.Title} position has been scheduled.

Date and Time: {interview.ScheduledAt:f}
Duration: {interview.DurationMin} minutes
Interview Type: {interview.Type}";

        if (!string.IsNullOrEmpty(interview.MeetingLink))
        {
            body += $@"

Meeting Link: {interview.MeetingLink}";
        }

        body += @"

Please make sure to be available at the scheduled time.

Best regards,
The Hiring Team";

        _jobService.Enqueue(() => _emailService.SendEmailAsync(interview.Application.Candidate.Email, subject, body, CancellationToken.None));
    }
}
