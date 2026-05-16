using System.Text.Json;
using RC.HyRe.Domain.Common;

namespace RC.HyRe.Domain.Entities;

/// <summary>
/// An in-app notification delivered to a specific platform user.
/// ReadAt being null means unread.
/// Payload is free-form JSONB (varies by notification type).
/// </summary>
public class Notification : HiringBaseEntity
{
    /// <summary>FK → AspNetUsers.Id (string, matches IdentityUser.Id).</summary>
    public required string RecipientId { get; set; }

    /// <summary>Notification type key, e.g. "scorecard.submitted", "offer.approved".</summary>
    public required string Type { get; set; }

    /// <summary>Free-form payload JSONB. Shape depends on Type.</summary>
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");

    public DateTimeOffset? ReadAt { get; set; }

    public bool IsRead => ReadAt.HasValue;

    public void MarkRead()
    {
        if (!IsRead)
            ReadAt = DateTimeOffset.UtcNow;
    }
}
