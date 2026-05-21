using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Notifications.EventHandlers;

public class CandidateCreatedEventHandler : INotificationHandler<CandidateCreatedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IBackgroundJobService _jobService;
    private readonly IEmailService _emailService;

    public CandidateCreatedEventHandler(
        IApplicationDbContext context,
        IBackgroundJobService jobService,
        IEmailService emailService)
    {
        _context = context;
        _jobService = jobService;
        _emailService = emailService;
    }

    public async Task Handle(CandidateCreatedEvent notification, CancellationToken cancellationToken)
    {
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.Id == notification.CandidateId, cancellationToken);

        if (candidate == null) return;

        var emailSubject = "Application Received - HyRe";
        var emailBody = $@"Hi {candidate.Name},

Thank you for applying to our open position! We have successfully received your application.

Our hiring team will review your profile and get back to you shortly.

Best regards,
The Hiring Team";

        _jobService.Enqueue(() => _emailService.SendEmailAsync(candidate.Email, emailSubject, emailBody, CancellationToken.None));
    }
}
