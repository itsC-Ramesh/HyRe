using System.Text.Json;
using MediatR;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Notifications.EventHandlers;

public class NoteCreatedEventHandler : INotificationHandler<NoteCreatedEvent>
{
    private readonly IApplicationDbContext _context;

    public NoteCreatedEventHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(NoteCreatedEvent notification, CancellationToken cancellationToken)
    {
        var eventLog = new RC.HyRe.Domain.Entities.EventLog
        {
            EntityType = notification.EntityType,
            EntityId = notification.EntityId,
            Action = "note.created",
            ActorId = null,
            PayloadJson = JsonSerializer.Serialize(new
            {
                notification.Content,
                notification.EntityType,
                notification.EntityId
            })
        };

        _context.EventLogs.Add(eventLog);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
