using MediatR;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Notifications.EventHandlers;

public class OfferInitiatedEventHandler : INotificationHandler<OfferInitiatedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public OfferInitiatedEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Handle(OfferInitiatedEvent notification, CancellationToken cancellationToken)
    {
        var offer = await _context.Offers
            .Include(o => o.Application)
                .ThenInclude(a => a.Candidate)
            .Include(o => o.Application)
                .ThenInclude(a => a.Requisition)
            .FirstOrDefaultAsync(o => o.Id == notification.OfferId, cancellationToken);

        if (offer == null) return;

        // Notify the requisition owner (who acts as the approver)
        var approverId = offer.Application.Requisition.OwnerId;
        if (!string.IsNullOrEmpty(approverId))
        {
            var payload = new
            {
                OfferId = offer.Id,
                ApplicationId = offer.ApplicationId,
                CandidateName = offer.Application.Candidate.Name,
                RequisitionTitle = offer.Application.Requisition.Title,
                Salary = offer.Salary,
                Currency = offer.Currency,
                StartDate = offer.StartDate
            };

            await _notificationService.CreateNotificationAsync(
                approverId,
                "offer.approval_requested",
                payload,
                cancellationToken);
        }
    }
}
