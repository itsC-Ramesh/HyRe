using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Domain.Events;
using NUnit.Framework;
using Shouldly;

namespace RC.HyRe.Domain.UnitTests.Entities;

public class JobApplicationTests
{
    private static JobApplication CreateApplication(ApplicationStage stage = ApplicationStage.Applied)
    {
        return new JobApplication
        {
            Id = Guid.NewGuid(),
            CandidateId = Guid.NewGuid(),
            RequisitionId = Guid.NewGuid(),
            Stage = stage
        };
    }

    [TestCase(ApplicationStage.Applied, ApplicationStage.Screened)]
    [TestCase(ApplicationStage.Screened, ApplicationStage.Interview)]
    [TestCase(ApplicationStage.Interview, ApplicationStage.Offer)]
    [TestCase(ApplicationStage.Offer, ApplicationStage.Hired)]
    public void AdvanceStage_ValidTransition_Succeeds(ApplicationStage from, ApplicationStage to)
    {
        var app = CreateApplication(from);

        app.AdvanceStage(to);

        app.Stage.ShouldBe(to);
    }

    [TestCase(ApplicationStage.Applied, ApplicationStage.Offer)]
    [TestCase(ApplicationStage.Applied, ApplicationStage.Hired)]
    [TestCase(ApplicationStage.Screened, ApplicationStage.Applied)]
    [TestCase(ApplicationStage.Hired, ApplicationStage.Screened)]
    public void AdvanceStage_InvalidTransition_Throws(ApplicationStage from, ApplicationStage to)
    {
        var app = CreateApplication(from);

        Should.Throw<InvalidOperationException>(() => app.AdvanceStage(to));
    }

    [Test]
    public void Reject_FromAnyStage_Succeeds()
    {
        var app = CreateApplication(ApplicationStage.Interview);

        app.Reject("Not a fit");

        app.Stage.ShouldBe(ApplicationStage.Rejected);
        app.RejectionReason.ShouldBe("Not a fit");
    }

    [Test]
    public void AdvanceStage_RaisesDomainEvent()
    {
        var app = CreateApplication(ApplicationStage.Applied);
        app.ClearDomainEvents();

        app.AdvanceStage(ApplicationStage.Screened);

        app.DomainEvents.Count.ShouldBe(1);
        app.DomainEvents.ShouldContain(e => e is ApplicationStageChangedEvent);
    }
}
