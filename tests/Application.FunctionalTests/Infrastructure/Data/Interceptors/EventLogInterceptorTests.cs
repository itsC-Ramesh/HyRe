using RC.HyRe.Application.FunctionalTests.Infrastructure;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Domain.Events;
using RC.HyRe.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace RC.HyRe.Application.FunctionalTests.Infrastructure.Data.Interceptors;

using static TestApp;

public class EventLogInterceptorTests : TestBase
{
    [Test]
    public async Task ShouldCreateEventLogWhenDomainEventIsAdded()
    {
        // Arrange
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var candidate = new Candidate
        {
            Name = "Test Candidate",
            Email = "test@example.com",
            Source = CandidateSource.Direct
        };

        // Act
        // By adding CandidateCreatedEvent, the EventLogInterceptor should intercept and create an EventLog
        candidate.AddDomainEvent(new CandidateCreatedEvent(candidate.Id, candidate.Email, null));

        context.Candidates.Add(candidate);
        await context.SaveChangesAsync();

        // Assert
        var eventLogs = context.EventLogs.ToList();
        
        Assert.That(eventLogs, Is.Not.Empty);
        Assert.That(eventLogs.Count, Is.EqualTo(1));

        var log = eventLogs.First();
        Assert.That(log.EntityType, Is.EqualTo("candidate"));
        Assert.That(log.EntityId, Is.EqualTo(candidate.Id));
        Assert.That(log.Action, Is.EqualTo("candidate.created"));
        Assert.That(log.PayloadJson, Does.Contain("test@example.com"));
    }
}
