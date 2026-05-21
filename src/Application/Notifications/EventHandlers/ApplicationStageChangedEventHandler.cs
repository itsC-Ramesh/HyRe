using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Notifications.EventHandlers;

public class ApplicationStageChangedEventHandler : INotificationHandler<ApplicationStageChangedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IBackgroundJobService _jobService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;

    public ApplicationStageChangedEventHandler(
        IApplicationDbContext context,
        IBackgroundJobService jobService,
        IEmailService emailService,
        INotificationService notificationService,
        IConfiguration configuration)
    {
        _context = context;
        _jobService = jobService;
        _emailService = emailService;
        _notificationService = notificationService;
        _configuration = configuration;
    }

    public async Task Handle(ApplicationStageChangedEvent notification, CancellationToken cancellationToken)
    {
        var application = await _context.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Requisition)
            .FirstOrDefaultAsync(a => a.Id == notification.ApplicationId, cancellationToken);

        if (application == null) return;

        if (!string.IsNullOrEmpty(application.Requisition.OwnerId))
        {
            var payload = new
            {
                ApplicationId = application.Id,
                CandidateName = application.Candidate.Name,
                RequisitionTitle = application.Requisition.Title,
                PreviousStage = notification.PreviousStage.ToString(),
                NewStage = notification.NewStage.ToString()
            };

            await _notificationService.CreateNotificationAsync(
                application.Requisition.OwnerId,
                NotificationTypes.StageChanged,
                payload,
                cancellationToken);
        }

        var portalUrl = _configuration["CandidatePortalUrl"] ?? "https://localhost:4200";
        string subject;
        string body;

        switch (notification.NewStage)
        {
            case ApplicationStage.Screened:
            case ApplicationStage.Interview:
                subject = $"Invitation to Interview - {application.Requisition.Title}";
                body = $@"Hi {application.Candidate.Name},

Great news! We would like to invite you to interview for the {application.Requisition.Title} role.

Please use the following link to select a convenient slot from our team's calendar:
{portalUrl}/portal/schedule/{application.Id}

Best regards,
The Hiring Team";
                break;

            case ApplicationStage.Rejected:
                subject = $"Update on your application for {application.Requisition.Title}";
                body = $@"Hi {application.Candidate.Name},

Thank you for taking the time to apply and speak with us about the {application.Requisition.Title} position.

Unfortunately, we have decided to proceed with other candidates whose experience more closely aligns with our current needs. We will keep your resume on file for future opportunities.

We wish you all the best in your search.

Sincerely,
The Hiring Team";
                break;

            case ApplicationStage.Offer:
                subject = $"Offer Details - {application.Requisition.Title}";
                body = $@"Hi {application.Candidate.Name},

We are thrilled to extend an offer for the {application.Requisition.Title} position!

We have generated your formal offer details. Please log in to your candidate portal to review and sign:
{portalUrl}/portal/offer/{application.Id}

Best regards,
The Hiring Team";
                break;

            default:
                // No candidate email for applied / hired in this handler
                return;
        }

        _jobService.Enqueue(() => _emailService.SendEmailAsync(application.Candidate.Email, subject, body, CancellationToken.None));
    }
}
