using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Notifications.Queries;

namespace RC.HyRe.Application.Common.Interfaces;

public interface INotificationService
{
    Task<Result<Guid>> CreateNotificationAsync(string recipientId, string type, object payload, CancellationToken ct = default);
    Task<Result<PaginatedList<NotificationDto>>> GetUserNotificationsAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task<Result> MarkAsReadAsync(Guid notificationId, string userId, CancellationToken ct = default);
    Task<Result<int>> GetUnreadCountAsync(string userId, CancellationToken ct = default);
    Task<Result> UpdateDeliveryStatusAsync(Guid notificationId, string status, string? failureReason = null, CancellationToken ct = default);
    Task<Result<PaginatedList<NotificationDto>>> GetPendingRetriesAsync(int page, int limit, CancellationToken ct = default);
}
