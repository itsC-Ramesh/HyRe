using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RC.HyRe.Domain.Common;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Infrastructure.Data.Interceptors;

public class EventLogInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AppendEventLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AppendEventLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AppendEventLogs(DbContext? context)
    {
        if (context == null) return;

        var entitiesWithEvents = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                var eventLog = MapToEventLog(domainEvent);
                if (eventLog != null)
                {
                    context.Add(eventLog);
                }
            }
        }
    }

    private EventLog? MapToEventLog(BaseEvent domainEvent)
    {
        return domainEvent switch
        {
            ApplicationStageChangedEvent e => new EventLog
            {
                EntityType = "application",
                EntityId = e.ApplicationId,
                Action = "stage.changed",
                ActorId = e.ActorId,
                PayloadJson = JsonSerializer.Serialize(new { previousStage = e.PreviousStage.ToString(), newStage = e.NewStage.ToString() })
            },
            InterviewBookedEvent e => new EventLog
            {
                EntityType = "interview",
                EntityId = e.InterviewId,
                Action = "interview.booked",
                ActorId = e.ActorId,
                PayloadJson = JsonSerializer.Serialize(new { interviewerId = e.InterviewerId })
            },
            ScorecardSubmittedEvent e => new EventLog
            {
                EntityType = "scorecard",
                EntityId = e.ScorecardId,
                Action = "scorecard.submitted",
                ActorId = e.ActorId,
                PayloadJson = "{}"
            },
            OfferInitiatedEvent e => new EventLog
            {
                EntityType = "offer",
                EntityId = e.OfferId,
                Action = "offer.initiated",
                ActorId = e.ActorId,
                PayloadJson = "{}"
            },
            OfferAcceptedEvent e => new EventLog
            {
                EntityType = "offer",
                EntityId = e.OfferId,
                Action = "offer.accepted",
                ActorId = e.ActorId,
                PayloadJson = "{}"
            },
            OfferDeclinedEvent e => new EventLog
            {
                EntityType = "offer",
                EntityId = e.OfferId,
                Action = "offer.declined",
                ActorId = e.ActorId,
                PayloadJson = "{}"
            },
            CandidateCreatedEvent e => new EventLog
            {
                EntityType = "candidate",
                EntityId = e.CandidateId,
                Action = "candidate.created",
                ActorId = e.ActorId,
                PayloadJson = JsonSerializer.Serialize(new { email = e.Email })
            },
            InterviewRescheduledEvent e => new EventLog
            {
                EntityType = "interview",
                EntityId = e.InterviewId,
                Action = "interview.rescheduled",
                ActorId = null,
                PayloadJson = JsonSerializer.Serialize(new { previousTime = e.PreviousTime, newTime = e.NewTime })
            },
            InterviewCancelledEvent e => new EventLog
            {
                EntityType = "interview",
                EntityId = e.InterviewId,
                Action = "interview.cancelled",
                ActorId = null,
                PayloadJson = JsonSerializer.Serialize(new { reason = e.Reason })
            },
            InterviewNoShowEvent e => new EventLog
            {
                EntityType = "interview",
                EntityId = e.InterviewId,
                Action = "interview.no_show",
                ActorId = null,
                PayloadJson = "{}"
            },
            InterviewCompletedEvent e => new EventLog
            {
                EntityType = "interview",
                EntityId = e.InterviewId,
                Action = "interview.completed",
                ActorId = null,
                PayloadJson = "{}"
            },
            _ => null
        };
    }
}
