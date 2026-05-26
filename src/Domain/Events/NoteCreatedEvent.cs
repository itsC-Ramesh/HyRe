using RC.HyRe.Domain.Common;

namespace RC.HyRe.Domain.Events;

public class NoteCreatedEvent(string entityType, Guid entityId, string content) : BaseEvent
{
    public string EntityType { get; } = entityType;
    public Guid EntityId { get; } = entityId;
    public string Content { get; } = content;
}
