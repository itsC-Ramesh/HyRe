using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Notifications.Queries;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;

namespace RC.HyRe.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IApplicationDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<Guid>> CreateNotificationAsync(string recipientId, string type, object payload, CancellationToken ct = default)
    {
        var jsonString = JsonSerializer.Serialize(payload);

        var notification = new Notification
        {
            RecipientId = recipientId,
            Type = type,
            PayloadJson = jsonString
        };

        _context.Notifications.Add(notification);
        // Do NOT call SaveChangesAsync here — when called from domain event handlers,
        // the outer SaveChanges will persist this as part of the same transaction.
        await _context.SaveChangesAsync(ct);

        return Result.Success(notification.Id);
    }

    public async Task<Result<PaginatedList<NotificationDto>>> GetUserNotificationsAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == userId)
            .OrderByDescending(n => n.Created);

        var projected = ProjectToDto(query);
        var list = await PaginatedList<NotificationDto>.CreateAsync(projected, page, pageSize, ct);

        return Result.Success(list);
    }

    public async Task<Result> MarkAsReadAsync(Guid notificationId, string userId, CancellationToken ct = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId, ct);

        if (notification == null)
        {
            return Result.Failure("Notification not found.");
        }

        notification.MarkRead();
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<int>> GetUnreadCountAsync(string userId, CancellationToken ct = default)
    {
        var count = await _context.Notifications
            .CountAsync(n => n.RecipientId == userId && n.ReadAt == null, ct);

        return Result.Success(count);
    }

    public async Task<Result> UpdateDeliveryStatusAsync(Guid notificationId, string status, string? failureReason = null, CancellationToken ct = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, ct);

        if (notification == null)
        {
            return Result.Failure("Notification not found.");
        }

        notification.DeliveryStatus = status;
        notification.FailureReason = failureReason;

        if (status == DeliveryStatuses.Sent)
        {
            notification.DeliveredAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<PaginatedList<NotificationDto>>> GetPendingRetriesAsync(int page, int limit, CancellationToken ct = default)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.DeliveryStatus == DeliveryStatuses.Failed)
            .OrderByDescending(n => n.Created);

        var projected = ProjectToDto(query);
        var list = await PaginatedList<NotificationDto>.CreateAsync(projected, page, limit, ct);

        return Result.Success(list);
    }

    private static IQueryable<NotificationDto> ProjectToDto(IQueryable<Notification> query) =>
        query.Select(n => new NotificationDto(
            n.Id,
            n.Type,
            n.PayloadJson,
            n.ReadAt,
            n.Created,
            n.DeliveryChannel,
            n.DeliveryStatus));
}
