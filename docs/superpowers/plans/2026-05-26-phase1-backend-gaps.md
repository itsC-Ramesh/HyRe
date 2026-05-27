# Phase 1 Backend Gaps — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 8 confirmed backend gaps in the Phase 1 hiring platform — 2 critical (double-booking, IsBooked flag) and 6 important (clone bug, missing event, stage validation, aggregate scorecards, draft save, NoteCreatedEvent handler).

**Architecture:** Each task is a self-contained fix that can be built and tested independently. Tasks 1-4 are small entity/handler fixes. Tasks 5-8 add new commands/queries. All follow the existing MediatR CQRS pattern with FluentValidation.

**Tech Stack:** C#, .NET 8, MediatR, EF Core (PostgreSQL), FluentValidation, xUnit

---

## File Structure

| Action | File | Purpose |
|--------|------|---------|
| Modify | `src/Application/Requisitions/Commands/CloneRequisition/CloneRequisitionCommandHandler.cs` | Fix missing Headcount copy |
| Modify | `src/Domain/Entities/JobApplication.cs` | Add stage transition validation |
| Modify | `src/Domain/Entities/Interview.cs` | Add Complete() method |
| Create | `src/Domain/Events/InterviewCompletedEvent.cs` | New domain event |
| Modify | `src/Application/Interviews/Commands/MarkCompleted.cs` | Use Complete() method |
| Modify | `src/Application/Interviews/Commands/ScheduleInterview.cs` | Add overlap detection + IsBooked |
| Create | `src/Application/Scorecards/Commands/SaveScorecardDraft.cs` | Draft save command |
| Create | `src/Application/Scorecards/Queries/GetScorecardSummary.cs` | Aggregate view query |
| Create | `src/Application/Notifications/EventHandlers/NoteCreatedEventHandler.cs` | Handle NoteCreatedEvent |
| Modify | `src/Infrastructure/Data/Interceptors/EventLogInterceptor.cs` | Add InterviewCompleted mapping |
| Modify | `src/Web/Endpoints/Scorecards.cs` | Add draft + summary endpoints |

---

### Task 1: Fix CloneRequisition missing Headcount

**Files:**
- Modify: `src/Application/Requisitions/Commands/CloneRequisition/CloneRequisitionCommandHandler.cs:21-30`

- [ ] **Step 1: Fix the clone handler to copy Headcount**

In `CloneRequisitionCommandHandler.cs`, the `new Requisition` block (line 21-30) is missing `Headcount`. Add it:

```csharp
var clone = new Requisition
{
    Title = $"{original.Title} (Copy)",
    Department = original.Department,
    JdText = original.JdText,
    SalaryMin = original.SalaryMin,
    SalaryMax = original.SalaryMax,
    Headcount = original.Headcount,  // ADD THIS LINE
    Status = RequisitionStatus.Draft,
    OwnerId = currentUser.Id ?? string.Empty
};
```

- [ ] **Step 2: Verify build passes**

Run: `dotnet build src/Application/Application.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/Application/Requisitions/Commands/CloneRequisition/CloneRequisitionCommandHandler.cs
git commit -m "fix(requisitions): copy Headcount in CloneRequisition"
```

---

### Task 2: Add stage transition validation to JobApplication

**Files:**
- Modify: `src/Domain/Entities/JobApplication.cs:32-37`

- [ ] **Step 1: Write the failing test**

Create `tests/Domain.UnitTests/Entities/JobApplicationTests.cs`:

```csharp
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using Xunit;

namespace Domain.UnitTests.Entities;

public class JobApplicationTests
{
    private JobApplication CreateApplication(ApplicationStage stage = ApplicationStage.Applied)
    {
        return new JobApplication
        {
            Id = Guid.NewGuid(),
            CandidateId = Guid.NewGuid(),
            RequisitionId = Guid.NewGuid(),
            Stage = stage
        };
    }

    [Theory]
    [InlineData(ApplicationStage.Applied, ApplicationStage.Screened)]
    [InlineData(ApplicationStage.Screened, ApplicationStage.Interview)]
    [InlineData(ApplicationStage.Interview, ApplicationStage.Offer)]
    [InlineData(ApplicationStage.Offer, ApplicationStage.Hired)]
    public void AdvanceStage_ValidTransition_Succeeds(ApplicationStage from, ApplicationStage to)
    {
        var app = CreateApplication(from);
        app.AdvanceStage(to);
        Assert.Equal(to, app.Stage);
    }

    [Theory]
    [InlineData(ApplicationStage.Applied, ApplicationStage.Offer)]
    [InlineData(ApplicationStage.Applied, ApplicationStage.Hired)]
    [InlineData(ApplicationStage.Screened, ApplicationStage.Applied)]
    [InlineData(ApplicationStage.Hired, ApplicationStage.Screened)]
    public void AdvanceStage_InvalidTransition_Throws(ApplicationStage from, ApplicationStage to)
    {
        var app = CreateApplication(from);
        Assert.Throws<InvalidOperationException>(() => app.AdvanceStage(to));
    }

    [Fact]
    public void Reject_FromAnyStage_Succeeds()
    {
        var app = CreateApplication(ApplicationStage.Interview);
        app.Reject("Not a fit");
        Assert.Equal(ApplicationStage.Rejected, app.Stage);
        Assert.Equal("Not a fit", app.RejectionReason);
    }

    [Fact]
    public void AdvanceStage_RaisesDomainEvent()
    {
        var app = CreateApplication(ApplicationStage.Applied);
        app.DomainEvents.Clear();
        app.AdvanceStage(ApplicationStage.Screened);
        Assert.Single(app.DomainEvents);
        Assert.IsType<ApplicationStageChangedEvent>(app.DomainEvents.First());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Domain.UnitTests/ --filter "JobApplicationTests"`
Expected: FAIL — no transition validation exists yet, invalid transitions pass.

- [ ] **Step 3: Add validation to JobApplication.AdvanceStage**

In `src/Domain/Entities/JobApplication.cs`, replace the `AdvanceStage` method:

```csharp
private static readonly Dictionary<ApplicationStage, ApplicationStage[]> AllowedTransitions = new()
{
    [ApplicationStage.Applied] = new[] { ApplicationStage.Screened, ApplicationStage.Rejected },
    [ApplicationStage.Screened] = new[] { ApplicationStage.Interview, ApplicationStage.Rejected },
    [ApplicationStage.Interview] = new[] { ApplicationStage.Offer, ApplicationStage.Rejected },
    [ApplicationStage.Offer] = new[] { ApplicationStage.Hired, ApplicationStage.Rejected },
    [ApplicationStage.Hired] = Array.Empty<ApplicationStage>(),
    [ApplicationStage.Rejected] = Array.Empty<ApplicationStage>(),
};

public void AdvanceStage(ApplicationStage newStage, string? actorId = null)
{
    if (newStage == ApplicationStage.Rejected)
    {
        // Reject() calls AdvanceStage — allow it through the guard
    }
    else if (!AllowedTransitions.ContainsKey(Stage) || !AllowedTransitions[Stage].Contains(newStage))
    {
        throw new InvalidOperationException(
            $"Cannot advance from {Stage} to {newStage}.");
    }

    var previousStage = Stage;
    Stage = newStage;
    AddDomainEvent(new ApplicationStageChangedEvent(Id, previousStage, newStage, actorId));
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Domain.UnitTests/ --filter "JobApplicationTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Domain/Entities/JobApplication.cs tests/Domain.UnitTests/Entities/JobApplicationTests.cs
git commit -m "feat(pipeline): enforce allowed stage transitions in JobApplication"
```

---

### Task 3: Add InterviewCompletedEvent and Complete() method

**Files:**
- Create: `src/Domain/Events/InterviewCompletedEvent.cs`
- Modify: `src/Domain/Entities/Interview.cs:40-44`
- Modify: `src/Application/Interviews/Commands/MarkCompleted.cs:31`
- Modify: `src/Infrastructure/Data/Interceptors/EventLogInterceptor.cs:131-133`

- [ ] **Step 1: Create InterviewCompletedEvent**

Create `src/Domain/Events/InterviewCompletedEvent.cs`:

```csharp
using RC.HyRe.Domain.Common;

namespace RC.HyRe.Domain.Events;

public class InterviewCompletedEvent : BaseEvent
{
    public Guid InterviewId { get; }
    public Guid ApplicationId { get; }

    public InterviewCompletedEvent(Guid interviewId, Guid applicationId)
    {
        InterviewId = interviewId;
        ApplicationId = applicationId;
    }
}
```

- [ ] **Step 2: Add Complete() method to Interview entity**

In `src/Domain/Entities/Interview.cs`, add after the `Book()` method:

```csharp
public void Complete()
{
    Status = InterviewStatus.Completed;
    AddDomainEvent(new InterviewCompletedEvent(Id, ApplicationId));
}
```

Also add `using RC.HyRe.Domain.Events;` if not already present (it is — used by Book()).

- [ ] **Step 3: Update MarkCompletedHandler to use Complete()**

In `src/Application/Interviews/Commands/MarkCompleted.cs`, replace line 31:

```csharp
// OLD:
interview.Status = InterviewStatus.Completed;

// NEW:
interview.Complete();
```

- [ ] **Step 4: Add InterviewCompleted to EventLogInterceptor**

In `src/Infrastructure/Data/Interceptors/EventLogInterceptor.cs`, add before the `_ => null` line:

```csharp
InterviewCompletedEvent e => new EventLog
{
    EntityType = "interview",
    EntityId = e.InterviewId,
    Action = "interview.completed",
    ActorId = null,
    PayloadJson = "{}"
},
```

- [ ] **Step 5: Verify build passes**

Run: `dotnet build src/Web/Web.csproj`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add src/Domain/Events/InterviewCompletedEvent.cs src/Domain/Entities/Interview.cs src/Application/Interviews/Commands/MarkCompleted.cs src/Infrastructure/Data/Interceptors/EventLogInterceptor.cs
git commit -m "feat(interviews): add InterviewCompletedEvent and Complete() method"
```

---

### Task 4: Fix ScheduleInterview — overlap detection + IsBooked flag

**Files:**
- Modify: `src/Application/Interviews/Commands/ScheduleInterview.cs:31-87`

- [ ] **Step 1: Write the failing test**

Create `tests/Application.UnitTests/Interviews/ScheduleInterviewTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Interviews.Commands;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using Moq;
using Xunit;

namespace Application.UnitTests.Interviews;

public class ScheduleInterviewTests
{
    private readonly Mock<IApplicationDbContext> _context = new();
    private readonly Mock<IUser> _user = new();

    private ScheduleInterviewHandler CreateHandler()
    {
        return new ScheduleInterviewHandler(_context.Object);
    }

    [Fact]
    public async Task Handle_OverlapExists_ReturnsFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var interviewerId = "interviewer-1";
        var scheduledAt = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

        var existingInterviews = new List<Interview>
        {
            new()
            {
                Id = Guid.NewGuid(),
                InterviewerId = interviewerId,
                ScheduledAt = scheduledAt.AddMinutes(-30),
                DurationMin = 60,
                Status = InterviewStatus.Scheduled
            }
        }.AsQueryable();

        var mockSet = new Mock<DbSet<Interview>>();
        mockSet.As<IQueryable<Interview>>().Setup(m => m.Provider).Returns(existingInterviews.Provider);
        mockSet.As<IQueryable<Interview>>().Setup(m => m.Expression).Returns(existingInterviews.Expression);
        mockSet.As<IQueryable<Interview>>().Setup(m => m.ElementType).Returns(existingInterviews.ElementType);
        mockSet.As<IQueryable<Interview>>().Setup(m => m.GetEnumerator()).Returns(existingInterviews.GetEnumerator());

        _context.Setup(c => c.Interviews).Returns(mockSet.Object);

        var applications = new List<JobApplication>
        {
            new() { Id = applicationId, RequisitionId = Guid.NewGuid(), Stage = ApplicationStage.Interview }
        }.AsQueryable();

        var mockAppSet = new Mock<DbSet<JobApplication>>();
        mockAppSet.As<IQueryable<JobApplication>>().Setup(m => m.Provider).Returns(applications.Provider);
        mockAppSet.As<IQueryable<JobApplication>>().Setup(m => m.Expression).Returns(applications.Expression);
        mockAppSet.As<IQueryable<JobApplication>>().Setup(m => m.ElementType).Returns(applications.ElementType);
        mockAppSet.As<IQueryable<JobApplication>>().Setup(m => m.GetEnumerator()).Returns(applications.GetEnumerator());

        _context.Setup(c => c.Applications).Returns(mockAppSet.Object);

        var handler = CreateHandler();
        var command = new ScheduleInterview(
            applicationId, interviewerId, InterviewType.Video,
            scheduledAt, 60, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("conflicting", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Application.UnitTests/ --filter "ScheduleInterviewTests"`
Expected: FAIL — no overlap detection exists.

- [ ] **Step 3: Add overlap detection and IsBooked to ScheduleInterviewHandler**

In `src/Application/Interviews/Commands/ScheduleInterview.cs`, add overlap detection before creating the interview (after the application null check):

```csharp
// After the application null check (line 37), add:

// Check for scheduling conflicts with main interviewer
var requestedEnd = request.ScheduledAt.AddMinutes(request.DurationMin);
var hasConflict = await _context.Interviews
    .AnyAsync(i =>
        i.InterviewerId == request.InterviewerId &&
        i.Status != InterviewStatus.Cancelled &&
        i.ScheduledAt < requestedEnd &&
        i.ScheduledAt.AddMinutes(i.DurationMin) > request.ScheduledAt,
    ct);

if (hasConflict)
    return Result.Failure<Guid>("Interviewer has a conflicting interview at this time.");

// Check panel members for conflicts
if (request.PanelMemberIds?.Count > 0)
{
    foreach (var panelId in request.PanelMemberIds)
    {
        if (panelId == request.InterviewerId) continue;

        var panelConflict = await _context.Interviews
            .AnyAsync(i =>
                i.InterviewerId == panelId &&
                i.Status != InterviewStatus.Cancelled &&
                i.ScheduledAt < requestedEnd &&
                i.ScheduledAt.AddMinutes(i.DurationMin) > request.ScheduledAt,
            ct);

        if (panelConflict)
            return Result.Failure<Guid>($"Panel member {panelId} has a conflicting interview.");
    }
}
```

Then add IsBooked update after adding the interview (after `_context.Interviews.Add(interview);`):

```csharp
_context.Interviews.Add(interview);

// Mark matching availability slot as booked
var matchingSlot = await _context.InterviewerAvailabilities
    .FirstOrDefaultAsync(a =>
        a.InterviewerId == request.InterviewerId &&
        !a.IsBooked &&
        a.StartTime <= request.ScheduledAt.TimeOfDay &&
        a.EndTime >= request.ScheduledAt.TimeOfDay.Add(TimeSpan.FromMinutes(request.DurationMin)),
    ct);

if (matchingSlot != null)
    matchingSlot.IsBooked = true;
```

Add `using Microsoft.EntityFrameworkCore;` at the top if not present (it is).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Application.UnitTests/ --filter "ScheduleInterviewTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Application/Interviews/Commands/ScheduleInterview.cs tests/Application.UnitTests/Interviews/ScheduleInterviewTests.cs
git commit -m "fix(interviews): add overlap detection and IsBooked flag in ScheduleInterview"
```

---

### Task 5: Add scorecard draft save command

**Files:**
- Create: `src/Application/Scorecards/Commands/SaveScorecardDraft.cs`
- Modify: `src/Web/Endpoints/Scorecards.cs:14, 47-58`

- [ ] **Step 1: Write the failing test**

Create `tests/Application.UnitTests/Scorecards/SaveScorecardDraftTests.cs`:

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Scorecards.Commands;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using Moq;
using Xunit;

namespace Application.UnitTests.Scorecards;

public class SaveScorecardDraftTests
{
    private readonly Mock<IApplicationDbContext> _context = new();
    private readonly Mock<IUser> _user = new();
    private readonly Guid _userId = Guid.NewGuid();

    public SaveScorecardDraftTests()
    {
        _user.Setup(u => u.Id).Returns(_userId.ToString());
    }

    [Fact]
    public async Task Handle_PartialRatings_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var scorecardId = Guid.NewGuid();
        var scorecards = new List<Scorecard>
        {
            new()
            {
                Id = scorecardId,
                InterviewId = Guid.NewGuid(),
                InterviewerId = _userId.ToString(),
                SubmittedAt = null,
                Strengths = "",
                Concerns = ""
            }
        }.AsQueryable();

        var mockSet = new Mock<DbSet<Scorecard>>();
        mockSet.As<IQueryable<Scorecard>>().Setup(m => m.Provider).Returns(scorecards.Provider);
        mockSet.As<IQueryable<Scorecard>>().Setup(m => m.Expression).Returns(scorecards.Expression);
        mockSet.As<IQueryable<Scorecard>>().Setup(m => m.ElementType).Returns(scorecards.ElementType);
        mockSet.As<IQueryable<Scorecard>>().Setup(m => m.GetEnumerator()).Returns(scorecards.GetEnumerator());

        _context.Setup(c => c.Scorecards).Returns(mockSet.Object);

        var handler = new SaveScorecardDraftHandler(_context.Object, _user.Object);
        var command = new SaveScorecardDraft(scorecardId, new() { ["technical"] = 4 }, null, null, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        _context.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadySubmitted_ReturnsFailure()
    {
        var scorecardId = Guid.NewGuid();
        var scorecards = new List<Scorecard>
        {
            new()
            {
                Id = scorecardId,
                InterviewerId = _userId.ToString(),
                SubmittedAt = DateTimeOffset.UtcNow
            }
        }.AsQueryable();

        var mockSet = new Mock<DbSet<Scorecard>>();
        mockSet.As<IQueryable<Scorecard>>().Setup(m => m.Provider).Returns(scorecards.Provider);
        mockSet.As<IQueryable<Scorecard>>().Setup(m => m.Expression).Returns(scorecards.Expression);
        mockSet.As<IQueryable<Scorecard>>().Setup(m => m.ElementType).Returns(scorecards.ElementType);
        mockSet.As<IQueryable<Scorecard>>().Setup(m => m.GetEnumerator()).Returns(scorecards.GetEnumerator());

        _context.Setup(c => c.Scorecards).Returns(mockSet.Object);

        var handler = new SaveScorecardDraftHandler(_context.Object, _user.Object);
        var command = new SaveScorecardDraft(scorecardId, null, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Application.UnitTests/ --filter "SaveScorecardDraftTests"`
Expected: FAIL — SaveScorecardDraft class does not exist.

- [ ] **Step 3: Create SaveScorecardDraft command and handler**

Create `src/Application/Scorecards/Commands/SaveScorecardDraft.cs`:

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Scorecards.Commands;

[Authorize(Permissions = Permissions.ScorecardsUpdate)]
public record SaveScorecardDraft(
    Guid Id,
    Dictionary<string, int>? Ratings,
    string? Recommendation,
    string? Strengths,
    string? Concerns,
    string? Notes
) : IRequest<Result>;

public class SaveScorecardDraftHandler : IRequestHandler<SaveScorecardDraft, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SaveScorecardDraftHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result> Handle(SaveScorecardDraft request, CancellationToken ct)
    {
        var scorecard = await _context.Scorecards
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (scorecard is null)
            return Result.Failure("Scorecard not found.");

        if (scorecard.InterviewerId != _user.Id)
            return Result.Failure("You can only edit your own scorecard.");

        if (scorecard.SubmittedAt.HasValue)
            return Result.Failure("Cannot edit a submitted scorecard.");

        if (request.Ratings is not null)
            scorecard.Ratings = JsonDocument.Parse(JsonSerializer.Serialize(request.Ratings));

        if (request.Recommendation is not null)
            scorecard.Recommendation = Enum.Parse<ScorecardRecommendation>(request.Recommendation);

        if (request.Strengths is not null)
            scorecard.Strengths = request.Strengths;

        if (request.Concerns is not null)
            scorecard.Concerns = request.Concerns;

        if (request.Notes is not null)
            scorecard.Notes = request.Notes;

        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
```

- [ ] **Step 4: Add draft endpoint to Scorecards**

In `src/Web/Endpoints/Scorecards.cs`, add the route in `Map()`:

```csharp
groupBuilder.MapPut(Draft, "{id}/draft").RequireAuthorization();
```

Add the handler method:

```csharp
public static async Task<IResult> Draft(
    ISender sender, Guid id, SaveDraftBody body, CancellationToken ct)
{
    var command = new SaveScorecardDraft(
        id, body.Ratings, body.Recommendation,
        body.Strengths, body.Concerns, body.Notes);

    var result = await sender.Send(command, ct);
    return result.Succeeded
        ? TypedResults.Ok(ApiResponse.Ok())
        : TypedResults.BadRequest(ApiResponse.Fail("DRAFT_FAILED", "Failed to save draft.", result.Errors));
}
```

Add the body record at the bottom:

```csharp
public record SaveDraftBody(
    Dictionary<string, int>? Ratings,
    string? Recommendation,
    string? Strengths,
    string? Concerns,
    string? Notes
);
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/Application.UnitTests/ --filter "SaveScorecardDraftTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/Application/Scorecards/Commands/SaveScorecardDraft.cs src/Web/Endpoints/Scorecards.cs tests/Application.UnitTests/Scorecards/SaveScorecardDraftTests.cs
git commit -m "feat(scorecards): add SaveScorecardDraft command and endpoint"
```

---

### Task 6: Add scorecard aggregate/summary view

**Files:**
- Create: `src/Application/Scorecards/Queries/GetScorecardSummary.cs`
- Modify: `src/Web/Endpoints/Scorecards.cs:9-14`

- [ ] **Step 1: Write the failing test**

Create `tests/Application.UnitTests/Scorecards/GetScorecardSummaryTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Scorecards.Queries;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using Moq;
using Xunit;

namespace Application.UnitTests.Scorecards;

public class GetScorecardSummaryTests
{
    private readonly Mock<IApplicationDbContext> _context = new();
    private readonly Mock<IUser> _user = new();

    private void SetupPrivilegedUser()
    {
        _user.Setup(u => u.Id).Returns("user-1");
        _user.Setup(u => u.Roles).Returns(new[] { Roles.HrAdmin });
    }

    [Fact]
    public async Task Handle_WithSubmittedScorecards_ReturnsAggregates()
    {
        // Arrange
        SetupPrivilegedUser();
        var applicationId = Guid.NewGuid();
        var interviewerId1 = "interviewer-1";
        var interviewerId2 = "interviewer-2";

        var interviews = new List<Interview>
        {
            new() { Id = Guid.NewGuid(), ApplicationId = applicationId, InterviewerId = interviewerId1, Status = InterviewStatus.Completed },
            new() { Id = Guid.NewGuid(), ApplicationId = applicationId, InterviewerId = interviewerId2, Status = InterviewStatus.Completed }
        }.AsQueryable();

        var mockInterviewSet = new Mock<DbSet<Interview>>();
        mockInterviewSet.As<IQueryable<Interview>>().Setup(m => m.Provider).Returns(interviews.Provider);
        mockInterviewSet.As<IQueryable<Interview>>().Setup(m => m.Expression).Returns(interviews.Expression);
        mockInterviewSet.As<IQueryable<Interview>>().Setup(m => m.ElementType).Returns(interviews.ElementType);
        mockInterviewSet.As<IQueryable<Interview>>().Setup(m => m.GetEnumerator()).Returns(interviews.GetEnumerator());
        _context.Setup(c => c.Interviews).Returns(mockInterviewSet.Object);

        var scorecards = new List<Scorecard>
        {
            new()
            {
                Id = Guid.NewGuid(),
                InterviewId = interviews.ElementAt(0).Id,
                InterviewerId = interviewerId1,
                Ratings = System.Text.Json.JsonDocument.Parse("{\"technical\":4,\"communication\":5,\"problemSolving\":3,\"cultureFit\":4}"),
                Recommendation = ScorecardRecommendation.Yes,
                Strengths = "Good",
                Concerns = "None",
                SubmittedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                InterviewId = interviews.ElementAt(1).Id,
                InterviewerId = interviewerId2,
                Ratings = System.Text.Json.JsonDocument.Parse("{\"technical\":3,\"communication\":4,\"problemSolving\":4,\"cultureFit\":3}"),
                Recommendation = ScorecardRecommendation.StrongYes,
                Strengths = "Great",
                Concerns = "Minor",
                SubmittedAt = DateTimeOffset.UtcNow
            }
        }.AsQueryable();

        var mockScorecardSet = new Mock<DbSet<Scorecard>>();
        mockScorecardSet.As<IQueryable<Scorecard>>().Setup(m => m.Provider).Returns(scorecards.Provider);
        mockScorecardSet.As<IQueryable<Scorecard>>().Setup(m => m.Expression).Returns(scorecards.Expression);
        mockScorecardSet.As<IQueryable<Scorecard>>().Setup(m => m.ElementType).Returns(scorecards.ElementType);
        mockScorecardSet.As<IQueryable<Scorecard>>().Setup(m => m.GetEnumerator()).Returns(scorecards.GetEnumerator());
        _context.Setup(c => c.Scorecards).Returns(mockScorecardSet.Object);

        var handler = new GetScorecardSummaryHandler(_context.Object, _user.Object);
        var query = new GetScorecardSummary(applicationId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value.TotalInterviews);
        Assert.Equal(2, result.Value.SubmittedCount);
        Assert.Equal(0, result.Value.PendingCount);
        Assert.Equal(3.5, result.Value.AverageRatings["technical"], 1);
        Assert.Equal(4.5, result.Value.AverageRatings["communication"], 1);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Application.UnitTests/ --filter "GetScorecardSummaryTests"`
Expected: FAIL — GetScorecardSummary class does not exist.

- [ ] **Step 3: Create GetScorecardSummary query**

Create `src/Application/Scorecards/Queries/GetScorecardSummary.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Scorecards.Queries;

[Authorize(Permissions = Permissions.ScorecardsRead)]
public record GetScorecardSummary(Guid ApplicationId)
    : IRequest<Result<ScorecardSummaryDto>>;

public class GetScorecardSummaryHandler
    : IRequestHandler<GetScorecardSummary, Result<ScorecardSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetScorecardSummaryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<ScorecardSummaryDto>> Handle(
        GetScorecardSummary request, CancellationToken ct)
    {
        var interviews = await _context.Interviews
            .AsNoTracking()
            .Where(i => i.ApplicationId == request.ApplicationId)
            .ToListAsync(ct);

        var interviewIds = interviews.Select(i => i.Id).ToList();

        var scorecards = await _context.Scorecards
            .AsNoTracking()
            .Where(s => interviewIds.Contains(s.InterviewId))
            .ToListAsync(ct);

        var submittedCount = scorecards.Count(s => s.SubmittedAt.HasValue);
        var pendingCount = scorecards.Count - submittedCount;

        // Compute average ratings
        var submittedScorecards = scorecards.Where(s => s.SubmittedAt.HasValue).ToList();
        var averageRatings = new Dictionary<string, double>();

        if (submittedScorecards.Any())
        {
            var dimensions = new[] { "technical", "communication", "problemSolving", "cultureFit" };
            foreach (var dim in dimensions)
            {
                var values = submittedScorecards
                    .Where(s => s.Ratings != null)
                    .Select(s =>
                    {
                        if (s.Ratings!.RootElement.TryGetProperty(dim, out var val))
                            return (double)val.GetInt32();
                        return 0.0;
                    })
                    .ToList();

                averageRatings[dim] = values.Any() ? values.Average() : 0;
            }
        }

        // Recommendation breakdown
        var recommendationBreakdown = submittedScorecards
            .GroupBy(s => s.Recommendation.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return Result.Success(new ScorecardSummaryDto(
            TotalInterviews: interviews.Count,
            SubmittedCount: submittedCount,
            PendingCount: pendingCount,
            AverageRatings: averageRatings,
            RecommendationBreakdown: recommendationBreakdown,
            Scorecards: submittedScorecards.Select(s => new ScorecardSummaryItemDto(
                s.Id,
                s.InterviewId,
                s.InterviewerId,
                s.Recommendation.ToString(),
                s.SubmittedAt
            )).ToList()
        ));
    }
}

public record ScorecardSummaryDto(
    int TotalInterviews,
    int SubmittedCount,
    int PendingCount,
    Dictionary<string, double> AverageRatings,
    Dictionary<string, int> RecommendationBreakdown,
    List<ScorecardSummaryItemDto> Scorecards
);

public record ScorecardSummaryItemDto(
    Guid Id,
    Guid InterviewId,
    string InterviewerId,
    string Recommendation,
    DateTimeOffset? SubmittedAt
);
```

- [ ] **Step 4: Add summary endpoint to Scorecards**

In `src/Web/Endpoints/Scorecards.cs`, add in `Map()`:

```csharp
groupBuilder.MapGet(GetSummary, "application/{applicationId}/summary").RequireAuthorization();
```

Add the handler:

```csharp
public static async Task<IResult> GetSummary(
    ISender sender, Guid applicationId, CancellationToken ct)
{
    var result = await sender.Send(new GetScorecardSummary(applicationId), ct);
    return result.Succeeded
        ? TypedResults.Ok(ApiResponse.Ok(result.Value))
        : TypedResults.BadRequest(ApiResponse.Fail("SUMMARY_FAILED", "Failed to retrieve summary.", result.Errors));
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/Application.UnitTests/ --filter "GetScorecardSummaryTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/Application/Scorecards/Queries/GetScorecardSummary.cs src/Web/Endpoints/Scorecards.cs tests/Application.UnitTests/Scorecards/GetScorecardSummaryTests.cs
git commit -m "feat(scorecards): add aggregate summary view endpoint"
```

---

### Task 7: Add NoteCreatedEventHandler

**Files:**
- Create: `src/Application/Notifications/EventHandlers/NoteCreatedEventHandler.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/Application.UnitTests/Notifications/NoteCreatedEventHandlerTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Notification.EventHandlers;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Events;
using Moq;
using Xunit;

namespace Application.UnitTests.Notifications;

public class NoteCreatedEventHandlerTests
{
    private readonly Mock<IApplicationDbContext> _context = new();

    [Fact]
    public async Task Handle_NoteCreated_LogsEvent()
    {
        // Arrange
        var eventLogs = new List<EventLog>();
        var mockSet = new Mock<DbSet<EventLog>>();

        mockSet.Setup(m => m.Add(It.IsAny<EventLog>()))
            .Callback<EventLog>(e => eventLogs.Add(e));

        _context.Setup(c => c.EventLogs).Returns(mockSet.Object);
        _context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new NoteCreatedEventHandler(_context.Object);
        var domainEvent = new NoteCreatedEvent(
            Guid.NewGuid(), "Test note", "candidate", Guid.NewGuid());

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        Assert.Single(eventLogs);
        Assert.Equal("note.created", eventLogs[0].Action);
        Assert.Equal("note", eventLogs[0].EntityType);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Application.UnitTests/ --filter "NoteCreatedEventHandlerTests"`
Expected: FAIL — NoteCreatedEventHandler does not exist.

- [ ] **Step 3: Create NoteCreatedEventHandler**

Create `src/Application/Notifications/EventHandlers/NoteCreatedEventHandler.cs`:

```csharp
using System.Text.Json;
using MediatR;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Notification.EventHandlers;

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
            EntityType = "note",
            EntityId = notification.NoteId,
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Application.UnitTests/ --filter "NoteCreatedEventHandlerTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Application/Notifications/EventHandlers/NoteCreatedEventHandler.cs tests/Application.UnitTests/Notifications/NoteCreatedEventHandlerTests.cs
git commit -m "feat(notifications): add NoteCreatedEventHandler for event log persistence"
```

---

### Task 8: Verify build and run full test suite

- [ ] **Step 1: Build entire solution**

Run: `dotnet build`
Expected: Build succeeded with no errors.

- [ ] **Step 2: Run all unit tests**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 3: Final commit if any fixes needed**

```bash
git add -A
git commit -m "fix: address build/test issues from Phase 1 backend gaps"
```

(Only if fixes were needed — skip if everything passed clean.)

---

*End of backend gaps plan*
