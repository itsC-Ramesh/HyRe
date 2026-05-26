using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Notifications.EventHandlers;

public class InterviewNoShowEventHandler : INotificationHandler<InterviewNoShowEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public InterviewNoShowEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(InterviewNoShowEvent notification, CancellationToken cancellationToken)
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
            "interview.noshow",
            payload,
            cancellationToken);

        if (!string.IsNullOrEmpty(interview.Application.Requisition.OwnerId))
        {
            await _notificationService.CreateNotificationAsync(
                interview.Application.Requisition.OwnerId,
                "interview.noshow",
                payload,
                cancellationToken);
        }
    }
}
