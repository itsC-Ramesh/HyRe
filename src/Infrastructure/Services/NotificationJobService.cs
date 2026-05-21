using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Infrastructure.Services;

/// <summary>
/// Stateless service providing Hangfire recurring job methods for the notification engine.
/// All methods are designed to be idempotent to prevent duplicate notifications.
/// </summary>
public class NotificationJobService
{
    private readonly IInterviewRepository _interviewRepo;
    private readonly IScorecardRepository _scorecardRepo;
    private readonly IOfferRepository _offerRepo;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IIdentityService _identityService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<NotificationJobService> _logger;

    public NotificationJobService(
        IInterviewRepository interviewRepo,
        IScorecardRepository scorecardRepo,
        IOfferRepository offerRepo,
        INotificationService notificationService,
        IEmailService emailService,
        IIdentityService identityService,
        IApplicationDbContext context,
        ILogger<NotificationJobService> logger)
    {
        _interviewRepo = interviewRepo;
        _scorecardRepo = scorecardRepo;
        _offerRepo = offerRepo;
        _notificationService = notificationService;
        _emailService = emailService;
        _identityService = identityService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Sends reminders for interviews scheduled within the next 25 hours.
    /// Idempotent: checks for existing reminder notifications before creating duplicates.
    /// </summary>
    public async Task SendInterviewRemindersAsync(CancellationToken ct = default)
    {
        var upcomingInterviews = await _interviewRepo.GetUpcomingAsync(TimeSpan.FromHours(25), ct);

        foreach (var interview in upcomingInterviews)
        {
            // Check if a reminder notification already exists for this interview
            var existingReminder = await _context.Notifications
                .AnyAsync(n => n.Type == NotificationTypes.InterviewReminder
                    && n.PayloadJson.Contains(interview.Id.ToString()), ct);

            if (existingReminder)
            {
                _logger.LogDebug("Reminder already sent for interview {InterviewId}, skipping", interview.Id);
                continue;
            }

            // Load application details for the email
            var application = await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Requisition)
                .FirstOrDefaultAsync(a => a.Id == interview.ApplicationId, ct);

            if (application == null) continue;

            // Create in-app notification for the interviewer
            var payload = new
            {
                InterviewId = interview.Id,
                ApplicationId = interview.ApplicationId,
                CandidateName = application.Candidate.Name,
                RequisitionTitle = application.Requisition.Title,
                ScheduledAt = interview.ScheduledAt,
                MeetingLink = interview.MeetingLink
            };

            await _notificationService.CreateNotificationAsync(
                interview.InterviewerId,
                NotificationTypes.InterviewReminder,
                payload,
                ct);

            var interviewerName = await _identityService.GetUserNameAsync(interview.InterviewerId) ?? "Interviewer";
            var subject = $"Interview Reminder - {application.Requisition.Title}";
            var body = $@"Hi {interviewerName},

This is a reminder that you have an interview scheduled tomorrow.

Candidate: {application.Candidate.Name}
Position: {application.Requisition.Title}
Date and Time: {interview.ScheduledAt:f}
Duration: {interview.DurationMin} minutes
Type: {interview.Type}";

            if (!string.IsNullOrEmpty(interview.MeetingLink))
            {
                body += $@"

Meeting Link: {interview.MeetingLink}";
            }

            body += @"

Please make sure to review the candidate's profile and prepare your questions.

Best regards,
The Hiring Team";

            try
            {
                var interviewerEmail = await _identityService.GetUserEmailAsync(interview.InterviewerId);
                if (!string.IsNullOrEmpty(interviewerEmail))
                {
                    await _emailService.SendEmailAsync(interviewerEmail, subject, body, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send interview reminder email for interview {InterviewId}", interview.Id);
            }
        }
    }

    /// <summary>
    /// Sends reminders for overdue scorecards (past interviews without submitted scorecards).
    /// Idempotent: checks for existing overdue notifications before creating duplicates.
    /// </summary>
    public async Task SendOverdueScorecardRemindersAsync(CancellationToken ct = default)
    {
        var pastDueInterviews = await _interviewRepo.GetPastDueForScorecardAsync(ct);

        foreach (var interview in pastDueInterviews)
        {
            // Check if an overdue notification already exists for this interview
            var existingNotification = await _context.Notifications
                .AnyAsync(n => n.Type == NotificationTypes.ScorecardOverdue
                    && n.PayloadJson.Contains(interview.Id.ToString()), ct);

            if (existingNotification)
            {
                _logger.LogDebug("Overdue notification already sent for interview {InterviewId}, skipping", interview.Id);
                continue;
            }

            // Load application details
            var application = await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Requisition)
                .FirstOrDefaultAsync(a => a.Id == interview.ApplicationId, ct);

            if (application == null) continue;

            // Create in-app notification for the interviewer
            var payload = new
            {
                InterviewId = interview.Id,
                ApplicationId = interview.ApplicationId,
                CandidateName = application.Candidate.Name,
                RequisitionTitle = application.Requisition.Title,
                ScheduledAt = interview.ScheduledAt
            };

            await _notificationService.CreateNotificationAsync(
                interview.InterviewerId,
                NotificationTypes.ScorecardOverdue,
                payload,
                ct);

            var interviewerName = await _identityService.GetUserNameAsync(interview.InterviewerId) ?? "Interviewer";
            var subject = $"Overdue Scorecard - {application.Requisition.Title}";
            var body = $@"Hi {interviewerName},

Your scorecard for the following interview is overdue and needs to be submitted:

Candidate: {application.Candidate.Name}
Position: {application.Requisition.Title}
Interview Date: {interview.ScheduledAt:f}

Please submit your scorecard as soon as possible so the hiring process can continue.

Best regards,
The Hiring Team";

            try
            {
                var interviewerEmail = await _identityService.GetUserEmailAsync(interview.InterviewerId);
                if (!string.IsNullOrEmpty(interviewerEmail))
                {
                    await _emailService.SendEmailAsync(interviewerEmail, subject, body, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send overdue scorecard email for interview {InterviewId}", interview.Id);
            }
        }
    }

    /// <summary>
    /// Escalates stale offer approvals that have been pending for more than 48 hours.
    /// Idempotent: checks for existing escalation notifications before creating duplicates.
    /// </summary>
    public async Task EscalateStaleApprovalsAsync(CancellationToken ct = default)
    {
        var staleApprovals = await _offerRepo.GetPendingApprovalAsync(TimeSpan.FromHours(48), ct);

        foreach (var offer in staleApprovals)
        {
            // Check if an escalation notification already exists for this offer
            var existingNotification = await _context.Notifications
                .AnyAsync(n => n.Type == NotificationTypes.ApprovalEscalation
                    && n.PayloadJson.Contains(offer.Id.ToString()), ct);

            if (existingNotification)
            {
                _logger.LogDebug("Escalation notification already sent for offer {OfferId}, skipping", offer.Id);
                continue;
            }

            // Load application details
            var application = await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Requisition)
                .FirstOrDefaultAsync(a => a.Id == offer.ApplicationId, ct);

            if (application == null) continue;

            var approverId = application.Requisition.OwnerId;
            if (string.IsNullOrEmpty(approverId)) continue;

            // Create escalation notification
            var payload = new
            {
                OfferId = offer.Id,
                ApplicationId = offer.ApplicationId,
                CandidateName = application.Candidate.Name,
                RequisitionTitle = application.Requisition.Title,
                CreatedAt = offer.Created
            };

            await _notificationService.CreateNotificationAsync(
                approverId,
                NotificationTypes.ApprovalEscalation,
                payload,
                ct);

            _logger.LogInformation("Escalation notification created for offer {OfferId} pending since {CreatedAt}", offer.Id, offer.Created);
        }
    }

    /// <summary>
    /// Retries sending failed email notifications.
    /// Queries notifications with DeliveryStatus == "failed" and retries sending.
    /// </summary>
    public async Task RetryFailedNotificationsAsync(CancellationToken ct = default)
    {
        var failedNotifications = await _context.Notifications
            .Where(n => n.DeliveryStatus == DeliveryStatuses.Failed)
            .OrderBy(n => n.Created)
            .Take(50) // Process in batches to avoid overwhelming the email service
            .ToListAsync(ct);

        foreach (var notification in failedNotifications)
        {
            // Load recipient info for the email
            var recipientEmail = await _identityService.GetUserEmailAsync(notification.RecipientId);
            if (string.IsNullOrEmpty(recipientEmail))
            {
                _logger.LogWarning("Cannot retry notification {NotificationId}: recipient not found", notification.Id);
                continue;
            }

            // Reconstruct email content based on notification type
            var subject = notification.Type switch
            {
                NotificationTypes.InterviewBooked => "Interview Scheduled",
                NotificationTypes.InterviewReminder => "Interview Reminder",
                NotificationTypes.ScorecardOverdue => "Overdue Scorecard",
                NotificationTypes.OfferApprovalRequested => "Offer Approval Required",
                _ => $"Notification: {notification.Type}"
            };

            var body = $"You have a new notification of type: {notification.Type}. Please check your dashboard for details.";

            try
            {
                await _emailService.SendEmailAsync(recipientEmail, subject, body, ct);
                await _notificationService.UpdateDeliveryStatusAsync(notification.Id, DeliveryStatuses.Sent, null, ct);
                _logger.LogInformation("Successfully retried notification {NotificationId}", notification.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retry notification {NotificationId}", notification.Id);
                await _notificationService.UpdateDeliveryStatusAsync(notification.Id, DeliveryStatuses.Failed, ex.Message, ct);
            }
        }
    }
}
