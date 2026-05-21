using RC.HyRe.Domain.Common;

namespace RC.HyRe.Domain.Entities;

/// <summary>
/// An in-app notification delivered to a specific platform user.
/// ReadAt being null means unread.
/// PayloadJson is free-form JSONB (varies by notification type), stored as a string
/// to avoid JsonDocument disposal issues with EF Core materialization.
/// </summary>
public class Notification : HiringBaseEntity
{
    public required string RecipientId { get; set; }

    public required string Type { get; set; }

    /// <summary>Raw JSON payload. Mapped to JSONB column via EF configuration.</summary>
    public string PayloadJson { get; set; } = "{}";

    public DateTimeOffset? ReadAt { get; set; }

    public bool IsRead => ReadAt.HasValue;

    public string? DeliveryChannel { get; set; }

    public string? DeliveryStatus { get; set; }

    public DateTimeOffset? DeliveredAt { get; set; }

    public string? FailureReason { get; set; }

    public void MarkRead()
    {
        if (!IsRead)
            ReadAt = DateTimeOffset.UtcNow;
    }
}
