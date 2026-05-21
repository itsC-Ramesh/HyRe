using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Notifications.EventHandlers;

public class OfferAcceptedEventHandler : INotificationHandler<OfferAcceptedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public OfferAcceptedEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(OfferAcceptedEvent notification, CancellationToken cancellationToken)
    {
        var application = await _context.Applications
            .Include(a => a.Candidate)
            .Include(a => a.Requisition)
            .FirstOrDefaultAsync(a => a.Id == notification.ApplicationId, cancellationToken);

        if (application == null) return;

        var ownerId = application.Requisition.OwnerId;
        if (!string.IsNullOrEmpty(ownerId))
        {
            var payload = new
            {
                OfferId = notification.OfferId,
                ApplicationId = notification.ApplicationId,
                CandidateName = application.Candidate.Name,
                RequisitionTitle = application.Requisition.Title
            };

            await _notificationService.CreateNotificationAsync(
                ownerId,
                NotificationTypes.OfferAccepted,
                payload,
                cancellationToken);
        }
    }
}
