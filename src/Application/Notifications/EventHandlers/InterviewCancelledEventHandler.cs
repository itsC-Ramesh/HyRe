using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Notifications.EventHandlers;

public class InterviewCancelledEventHandler : INotificationHandler<InterviewCancelledEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IBackgroundJobService _jobService;
    private readonly ITemplateEngine _templateEngine;

    public InterviewCancelledEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IEmailService emailService,
        IBackgroundJobService jobService,
        ITemplateEngine templateEngine)
    {
        _context = context;
        _notificationService = notificationService;
        _emailService = emailService;
        _jobService = jobService;
        _templateEngine = templateEngine;
    }

    public async Task Handle(InterviewCancelledEvent notification, CancellationToken cancellationToken)
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
            RequisitionTitle = interview.Application.Requisition.Title
        };

        await _notificationService.CreateNotificationAsync(
            interview.InterviewerId,
            "interview.cancelled",
            payload,
            cancellationToken);

        // 2. Send email to the candidate
        var variables = new Dictionary<string, string>
        {
            { "CandidateName", interview.Application.Candidate.Name },
            { "RequisitionTitle", interview.Application.Requisition.Title }
        };

        var templateResult = await _templateEngine.RenderAsync(RC.HyRe.Domain.Enums.TemplateCategory.InterviewCancelled, variables, cancellationToken);

        _jobService.Enqueue(() => _emailService.SendEmailAsync(interview.Application.Candidate.Email, templateResult.Subject, templateResult.Body, CancellationToken.None));
    }
}
