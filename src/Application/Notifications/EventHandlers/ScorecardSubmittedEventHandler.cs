using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Notifications.EventHandlers;

public class ScorecardSubmittedEventHandler : INotificationHandler<ScorecardSubmittedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IIdentityService _identityService;

    public ScorecardSubmittedEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService,
        IIdentityService identityService)
    {
        _context = context;
        _notificationService = notificationService;
        _identityService = identityService;
    }

    public async Task Handle(ScorecardSubmittedEvent notification, CancellationToken cancellationToken)
    {
        var interview = await _context.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a.Candidate)
            .Include(i => i.Application)
                .ThenInclude(a => a.Requisition)
            .FirstOrDefaultAsync(i => i.Id == notification.InterviewId, cancellationToken);

        if (interview == null) return;

        var ownerId = interview.Application.Requisition.OwnerId;
        if (!string.IsNullOrEmpty(ownerId))
        {
            var interviewerName = await _identityService.GetUserNameAsync(interview.InterviewerId) ?? "An interviewer";

            var payload = new
            {
                ScorecardId = notification.ScorecardId,
                InterviewId = notification.InterviewId,
                CandidateName = interview.Application.Candidate.Name,
                RequisitionTitle = interview.Application.Requisition.Title,
                InterviewerName = interviewerName
            };

            await _notificationService.CreateNotificationAsync(
                ownerId,
                NotificationTypes.ScorecardSubmitted,
                payload,
                cancellationToken);
        }
    }
}
