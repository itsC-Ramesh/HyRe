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
    private readonly ITemplateEngine _templateEngine;

    public CandidateCreatedEventHandler(
        IApplicationDbContext context,
        IBackgroundJobService jobService,
        IEmailService emailService,
        ITemplateEngine templateEngine)
    {
        _context = context;
        _jobService = jobService;
        _emailService = emailService;
        _templateEngine = templateEngine;
    }

    public async Task Handle(CandidateCreatedEvent notification, CancellationToken cancellationToken)
    {
        var candidate = await _context.Candidates
            .FirstOrDefaultAsync(c => c.Id == notification.CandidateId, cancellationToken);

        if (candidate == null) return;

        var variables = new Dictionary<string, string>
        {
            { "CandidateName", candidate.Name },
            { "RequisitionTitle", "Any Position" }
        };

        var templateResult = await _templateEngine.RenderAsync(RC.HyRe.Domain.Enums.TemplateCategory.ApplicationAck, variables, cancellationToken);

        _jobService.Enqueue(() => _emailService.SendEmailAsync(candidate.Email, templateResult.Subject, templateResult.Body, CancellationToken.None));
    }
}
