using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Notifications.EventHandlers;
using RC.HyRe.Application.UnitTests.Common;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.UnitTests.Notifications.EventHandlers;

public class CandidateCreatedEventHandlerTests
{
    private Mock<IApplicationDbContext> _context = null!;
    private Mock<IBackgroundJobService> _jobService = null!;
    private Mock<IEmailService> _emailService = null!;
    private CandidateCreatedEventHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _context = new Mock<IApplicationDbContext>();
        _jobService = new Mock<IBackgroundJobService>();
        _emailService = new Mock<IEmailService>();
        _handler = new CandidateCreatedEventHandler(_context.Object, _jobService.Object, _emailService.Object);
    }

    [Test]
    public async Task Handle_ShouldEnqueueWelcomeEmail_WhenCandidateExists()
    {
        // Arrange
        var candidateId = Guid.NewGuid();
        var candidateName = "Jane Doe";
        var candidateEmail = "jane.doe@example.com";
        
        var candidate = new Candidate
        {
            Id = candidateId,
            Name = candidateName,
            Email = candidateEmail
        };

        var dbSetMock = new List<Candidate> { candidate }.MockDbSet();
        _context.Setup(x => x.Candidates).Returns(dbSetMock.Object);

        var domainEvent = new CandidateCreatedEvent(candidateId, candidateName, candidateEmail);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _jobService.Verify(x => x.Enqueue(It.IsAny<Expression<Func<Task>>>()), Times.Once);
    }
}
