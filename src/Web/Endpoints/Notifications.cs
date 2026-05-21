using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Web.Infrastructure;

namespace RC.HyRe.Web.Endpoints;

public class Notifications : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetNotifications, "")
            .RequireAuthorization();
        groupBuilder.MapPost(MarkAsRead, "{id}/read")
            .RequireAuthorization();
        groupBuilder.MapGet(GetUnreadCount, "unread-count")
            .RequireAuthorization();
    }

    public static async Task<IResult> GetNotifications(
        INotificationService notificationService,
        IUser user,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(user.Id))
        {
            return TypedResults.Unauthorized();
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await notificationService.GetUserNotificationsAsync(user.Id, page, pageSize, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_NOTIFICATIONS_FAILED", "Failed to retrieve notifications.", result.Errors));
    }

    public static async Task<IResult> MarkAsRead(
        INotificationService notificationService,
        IUser user,
        Guid id,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(user.Id))
        {
            return TypedResults.Unauthorized();
        }

        var result = await notificationService.MarkAsReadAsync(id, user.Id, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("MARK_READ_FAILED", "Failed to mark notification as read.", result.Errors));
    }

    public static async Task<IResult> GetUnreadCount(
        INotificationService notificationService,
        IUser user,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(user.Id))
        {
            return TypedResults.Unauthorized();
        }

        var result = await notificationService.GetUnreadCountAsync(user.Id, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_UNREAD_COUNT_FAILED", "Failed to retrieve unread count.", result.Errors));
    }
}
