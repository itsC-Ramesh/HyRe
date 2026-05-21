namespace RC.HyRe.Application.Notifications.Queries;

public record NotificationDto(
    Guid Id,
    string Type,
    string PayloadJson,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt,
    string? DeliveryChannel = null,
    string? DeliveryStatus = null
);
