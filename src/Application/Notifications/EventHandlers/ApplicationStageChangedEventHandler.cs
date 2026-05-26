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
    private readonly ITemplateEngine _templateEngine;

    public ApplicationStageChangedEventHandler(
        IApplicationDbContext context,
        IBackgroundJobService jobService,
        IEmailService emailService,
        INotificationService notificationService,
        IConfiguration configuration,
        ITemplateEngine templateEngine)
    {
        _context = context;
        _jobService = jobService;
        _emailService = emailService;
        _notificationService = notificationService;
        _configuration = configuration;
        _templateEngine = templateEngine;
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
        var variables = new Dictionary<string, string>
        {
            { "CandidateName", application.Candidate.Name },
            { "RequisitionTitle", application.Requisition.Title },
            { "ScheduleLink", $"{portalUrl}/portal/schedule/{application.Id}" },
            { "PortalUrl", $"{portalUrl}/portal/offer/{application.Id}" }
        };

        TemplateCategory? category = notification.NewStage switch
        {
            ApplicationStage.Screened or ApplicationStage.Interview => TemplateCategory.InterviewInvite,
            ApplicationStage.Rejected => TemplateCategory.Rejection,
            ApplicationStage.Offer => TemplateCategory.OfferLetter,
            _ => null
        };

        if (!category.HasValue)
            return;

        var templateResult = await _templateEngine.RenderAsync(category.Value, variables, cancellationToken);

        _jobService.Enqueue(() => _emailService.SendEmailAsync(application.Candidate.Email, templateResult.Subject, templateResult.Body, CancellationToken.None));
    }
}
