# Phase 1: Core Hiring Loop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the 5 core modules (Requisitions, Candidates, Pipeline, Interviews, Scorecards) end-to-end with MediatR CQRS, FluentValidation, and Minimal API endpoints.

**Architecture:** MediatR command/query pattern. Thin endpoints delegate to `ISender`. Domain events auto-dispatch via `DispatchDomainEventsInterceptor` on `SaveChanges`. RBAC via `[Authorize]` attribute processed by `AuthorizationBehaviour`.

**Tech Stack:** .NET 9, EF Core + PostgreSQL, MediatR, FluentValidation, Minimal APIs

**Spec:** `docs/superpowers/specs/2026-05-22-phase1-core-hiring-loop-design.md`

---

## File Structure

```
src/Application/Requisitions/
├── Commands/CreateRequisition.cs
├── Commands/UpdateRequisition.cs
├── Commands/SubmitForApproval.cs
├── Commands/ApproveRequisition.cs
├── Commands/RejectRequisition.cs
├── Commands/HoldRequisition.cs
├── Commands/CloseRequisition.cs
├── Commands/RequisitionCommandValidator.cs
├── Queries/GetRequisitionById.cs
├── Queries/GetRequisitions.cs
└── Queries/RequisitionDto.cs

src/Application/Candidates/
├── Commands/CreateCandidate.cs
├── Commands/UpdateCandidate.cs
├── Commands/ApplyToRequisition.cs
├── Commands/CandidateCommandValidator.cs
├── Queries/GetCandidateById.cs
├── Queries/GetCandidates.cs
└── Queries/CandidateDto.cs

src/Application/Pipeline/
├── Commands/AdvanceApplicationStage.cs
├── Commands/RejectApplication.cs
├── Commands/BulkAdvanceStage.cs
├── Commands/PipelineCommandValidator.cs
├── Queries/GetPipelineByRequisition.cs
├── Queries/GetApplicationById.cs
└── Queries/PipelineDto.cs

src/Application/Interviews/
├── Commands/ScheduleInterview.cs
├── Commands/RescheduleInterview.cs
├── Commands/CancelInterview.cs
├── Commands/MarkNoShow.cs
├── Commands/MarkCompleted.cs
├── Commands/InterviewCommandValidator.cs
├── Queries/GetInterviewsByApplication.cs
├── Queries/GetInterviewsByInterviewer.cs
└── Queries/InterviewDto.cs

src/Application/Scorecards/
├── Commands/SubmitScorecard.cs
├── Commands/ScorecardCommandValidator.cs
├── Queries/GetScorecardByInterview.cs
├── Queries/GetScorecardsByInterviewer.cs
├── Queries/GetScorecardsByApplication.cs
└── Queries/ScorecardDto.cs

src/Web/Endpoints/Requisitions.cs
src/Web/Endpoints/Candidates.cs
src/Web/Endpoints/Pipeline.cs
src/Web/Endpoints/Interviews.cs
src/Web/Endpoints/Scorecards.cs
```

---

## Task 1: Requisitions — Commands

**Files:**

- Create: `src/Application/Requisitions/Commands/CreateRequisition.cs`
- Create: `src/Application/Requisitions/Commands/UpdateRequisition.cs`
- Create: `src/Application/Requisitions/Commands/SubmitForApproval.cs`
- Create: `src/Application/Requisitions/Commands/ApproveRequisition.cs`
- Create: `src/Application/Requisitions/Commands/RejectRequisition.cs`
- Create: `src/Application/Requisitions/Commands/HoldRequisition.cs`
- Create: `src/Application/Requisitions/Commands/CloseRequisition.cs`
- Create: `src/Application/Requisitions/Commands/RequisitionCommandValidator.cs`
- Modify: `src/Application/Common/Interfaces/Repositories/IRequisitionRepository.cs`
- Modify: `src/Infrastructure/Data/Repositories/RequisitionRepository.cs`

### Step 1: Add AddAsync/UpdateAsync to IRequisitionRepository

```csharp
// Add to IRequisitionRepository interface:
Task AddAsync(Requisition requisition, CancellationToken ct = default);
Task UpdateAsync(Requisition requisition, CancellationToken ct = default);
```

- [ ] **Step 1a: Modify `src/Application/Common/Interfaces/Repositories/IRequisitionRepository.cs`**

Add these two methods after the existing `GetPagedAsync` method:

```csharp
Task AddAsync(Requisition requisition, CancellationToken ct = default);
Task UpdateAsync(Requisition requisition, CancellationToken ct = default);
```

- [ ] **Step 1b: Implement in `src/Infrastructure/Data/Repositories/RequisitionRepository.cs`**

Add at the end of the class:

```csharp
public async Task AddAsync(Requisition requisition, CancellationToken ct = default)
{
    _context.Requisitions.Add(requisition);
    await _context.SaveChangesAsync(ct);
}

public async Task UpdateAsync(Requisition requisition, CancellationToken ct = default)
{
    _context.Requisitions.Update(requisition);
    await _context.SaveChangesAsync(ct);
}
```

### Step 2: Create CreateRequisition command

- [ ] **Step 2: Create `src/Application/Requisitions/Commands/CreateRequisition.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Permissions = Permissions.RequisitionsCreate)]
public record CreateRequisition(
    string Title,
    string Department,
    string JdText,
    int? SalaryMin,
    int? SalaryMax,
    int Headcount
) : IRequest<Result<Guid>>;

public class CreateRequisitionHandler : IRequestHandler<CreateRequisition, Result<Guid>>
{
    private readonly IRequisitionRepository _repository;
    private readonly IUser _user;

    public CreateRequisitionHandler(IRequisitionRepository repository, IUser user)
    {
        _repository = repository;
        _user = user;
    }

    public async Task<Result<Guid>> Handle(CreateRequisition request, CancellationToken ct)
    {
        var requisition = new Requisition
        {
            Title = request.Title,
            Department = request.Department,
            OwnerId = _user.Id!,
            JdText = request.JdText,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            Headcount = request.Headcount,
            Status = RequisitionStatus.Draft
        };

        await _repository.AddAsync(requisition, ct);
        return Result.Success(requisition.Id);
    }
}
```

### Step 3: Create UpdateRequisition command

- [ ] **Step 3: Create `src/Application/Requisitions/Commands/UpdateRequisition.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Permissions = Permissions.RequisitionsUpdate)]
public record UpdateRequisition(
    Guid Id,
    string Title,
    string Department,
    string JdText,
    int? SalaryMin,
    int? SalaryMax,
    int Headcount
) : IRequest<Result>;

public class UpdateRequisitionHandler : IRequestHandler<UpdateRequisition, Result>
{
    private readonly IRequisitionRepository _repository;

    public UpdateRequisitionHandler(IRequisitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateRequisition request, CancellationToken ct)
    {
        var requisition = await _repository.GetByIdAsync(request.Id, ct);
        if (requisition is null)
            return Result.Failure("Requisition not found.");

        if (requisition.Status != RequisitionStatus.Draft)
            return Result.Failure("Only draft requisitions can be edited.");

        requisition.Title = request.Title;
        requisition.Department = request.Department;
        requisition.JdText = request.JdText;
        requisition.SalaryMin = request.SalaryMin;
        requisition.SalaryMax = request.SalaryMax;
        requisition.Headcount = request.Headcount;

        await _repository.UpdateAsync(requisition, ct);
        return Result.Success();
    }
}
```

### Step 4: Create status transition commands

- [ ] **Step 4a: Create `src/Application/Requisitions/Commands/SubmitForApproval.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Permissions = Permissions.RequisitionsUpdate)]
public record SubmitForApproval(Guid Id) : IRequest<Result>;

public class SubmitForApprovalHandler : IRequestHandler<SubmitForApproval, Result>
{
    private readonly IRequisitionRepository _repository;

    public SubmitForApprovalHandler(IRequisitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SubmitForApproval request, CancellationToken ct)
    {
        var requisition = await _repository.GetByIdAsync(request.Id, ct);
        if (requisition is null)
            return Result.Failure("Requisition not found.");

        if (requisition.Status != RequisitionStatus.Draft)
            return Result.Failure("Only draft requisitions can be submitted for approval.");

        requisition.Status = RequisitionStatus.PendingApproval;
        await _repository.UpdateAsync(requisition, ct);
        return Result.Success();
    }
}
```

- [ ] **Step 4b: Create `src/Application/Requisitions/Commands/ApproveRequisition.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Roles = Roles.HrAdmin)]
public record ApproveRequisition(Guid Id) : IRequest<Result>;

public class ApproveRequisitionHandler : IRequestHandler<ApproveRequisition, Result>
{
    private readonly IRequisitionRepository _repository;

    public ApproveRequisitionHandler(IRequisitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ApproveRequisition request, CancellationToken ct)
    {
        var requisition = await _repository.GetByIdAsync(request.Id, ct);
        if (requisition is null)
            return Result.Failure("Requisition not found.");

        if (requisition.Status != RequisitionStatus.PendingApproval)
            return Result.Failure("Only pending requisitions can be approved.");

        requisition.Status = RequisitionStatus.Open;
        await _repository.UpdateAsync(requisition, ct);
        return Result.Success();
    }
}
```

- [ ] **Step 4c: Create `src/Application/Requisitions/Commands/RejectRequisition.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Roles = Roles.HrAdmin)]
public record RejectRequisition(Guid Id, string Reason) : IRequest<Result>;

public class RejectRequisitionHandler : IRequestHandler<RejectRequisition, Result>
{
    private readonly IRequisitionRepository _repository;

    public RejectRequisitionHandler(IRequisitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RejectRequisition request, CancellationToken ct)
    {
        var requisition = await _repository.GetByIdAsync(request.Id, ct);
        if (requisition is null)
            return Result.Failure("Requisition not found.");

        if (requisition.Status != RequisitionStatus.PendingApproval)
            return Result.Failure("Only pending requisitions can be rejected.");

        requisition.Status = RequisitionStatus.Draft;
        await _repository.UpdateAsync(requisition, ct);
        return Result.Success();
    }
}
```

- [ ] **Step 4d: Create `src/Application/Requisitions/Commands/HoldRequisition.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Permissions = Permissions.RequisitionsUpdate)]
public record HoldRequisition(Guid Id) : IRequest<Result>;

public class HoldRequisitionHandler : IRequestHandler<HoldRequisition, Result>
{
    private readonly IRequisitionRepository _repository;

    public HoldRequisitionHandler(IRequisitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(HoldRequisition request, CancellationToken ct)
    {
        var requisition = await _repository.GetByIdAsync(request.Id, ct);
        if (requisition is null)
            return Result.Failure("Requisition not found.");

        if (requisition.Status != RequisitionStatus.Open)
            return Result.Failure("Only open requisitions can be put on hold.");

        requisition.Status = RequisitionStatus.OnHold;
        await _repository.UpdateAsync(requisition, ct);
        return Result.Success();
    }
}
```

- [ ] **Step 4e: Create `src/Application/Requisitions/Commands/CloseRequisition.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Commands;

[Authorize(Permissions = Permissions.RequisitionsUpdate)]
public record CloseRequisition(Guid Id) : IRequest<Result>;

public class CloseRequisitionHandler : IRequestHandler<CloseRequisition, Result>
{
    private readonly IRequisitionRepository _repository;

    public CloseRequisitionHandler(IRequisitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(CloseRequisition request, CancellationToken ct)
    {
        var requisition = await _repository.GetByIdAsync(request.Id, ct);
        if (requisition is null)
            return Result.Failure("Requisition not found.");

        if (requisition.Status is not (RequisitionStatus.Open or RequisitionStatus.OnHold))
            return Result.Failure("Only open or on-hold requisitions can be closed.");

        requisition.Status = RequisitionStatus.Closed;
        await _repository.UpdateAsync(requisition, ct);
        return Result.Success();
    }
}
```

### Step 5: Create shared validator

- [ ] **Step 5: Create `src/Application/Requisitions/Commands/RequisitionCommandValidator.cs`**

```csharp
namespace RC.HyRe.Application.Requisitions.Commands;

public class CreateRequisitionValidator : AbstractValidator<CreateRequisition>
{
    public CreateRequisitionValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.JdText).NotEmpty();
        RuleFor(x => x.Headcount).GreaterThan(0);
        RuleFor(x => x.SalaryMin).GreaterThanOrEqualTo(0).When(x => x.SalaryMin.HasValue);
        RuleFor(x => x.SalaryMax).GreaterThanOrEqualTo(0).When(x => x.SalaryMax.HasValue);
        RuleFor(x => x.SalaryMax).GreaterThanOrEqualTo(x => x.SalaryMin)
            .When(x => x.SalaryMin.HasValue && x.SalaryMax.HasValue);
    }
}

public class UpdateRequisitionValidator : AbstractValidator<UpdateRequisition>
{
    public UpdateRequisitionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.JdText).NotEmpty();
        RuleFor(x => x.Headcount).GreaterThan(0);
    }
}

public class RejectRequisitionValidator : AbstractValidator<RejectRequisition>
{
    public RejectRequisitionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
```

### Step 6: Commit

- [ ] **Step 6: Commit**

```bash
git add src/Application/Requisitions/ src/Application/Common/Interfaces/Repositories/IRequisitionRepository.cs src/Infrastructure/Data/Repositories/RequisitionRepository.cs
git commit -m "feat(requisitions): add CQRS commands for requisition lifecycle"
```

---

## Task 2: Requisitions — Queries & Endpoints

**Files:**

- Create: `src/Application/Requisitions/Queries/RequisitionDto.cs`
- Create: `src/Application/Requisitions/Queries/GetRequisitionById.cs`
- Create: `src/Application/Requisitions/Queries/GetRequisitions.cs`
- Create: `src/Web/Endpoints/Requisitions.cs`

### Step 1: Create DTOs

- [ ] **Step 1: Create `src/Application/Requisitions/Queries/RequisitionDto.cs`**

```csharp
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Queries;

public record RequisitionDto(
    Guid Id,
    string Title,
    string Department,
    string OwnerId,
    string JdText,
    int? SalaryMin,
    int? SalaryMax,
    int Headcount,
    RequisitionStatus Status,
    Dictionary<ApplicationStage, int> ApplicationCountByStage,
    DateTimeOffset Created,
    DateTimeOffset LastModified
);
```

### Step 2: Create GetRequisitionById query

- [ ] **Step 2: Create `src/Application/Requisitions/Queries/GetRequisitionById.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Queries;

[Authorize(Permissions = Permissions.RequisitionsRead)]
public record GetRequisitionById(Guid Id) : IRequest<Result<RequisitionDto>>;

public class GetRequisitionByIdHandler : IRequestHandler<GetRequisitionById, Result<RequisitionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRequisitionByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<RequisitionDto>> Handle(GetRequisitionById request, CancellationToken ct)
    {
        var requisition = await _context.Requisitions
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct);

        if (requisition is null)
            return Result.Failure<RequisitionDto>("Requisition not found.");

        var stageCounts = await _context.Applications
            .Where(a => a.RequisitionId == request.Id)
            .GroupBy(a => a.Stage)
            .Select(g => new { Stage = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Stage, x => x.Count, ct);

        var dto = new RequisitionDto(
            requisition.Id,
            requisition.Title,
            requisition.Department,
            requisition.OwnerId,
            requisition.JdText,
            requisition.SalaryMin,
            requisition.SalaryMax,
            requisition.Headcount,
            requisition.Status,
            stageCounts,
            requisition.Created,
            requisition.LastModified);

        return Result.Success(dto);
    }
}
```

### Step 3: Create GetRequisitions query

- [ ] **Step 3: Create `src/Application/Requisitions/Queries/GetRequisitions.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Requisitions.Queries;

[Authorize(Permissions = Permissions.RequisitionsRead)]
public record GetRequisitions(
    RequisitionStatus? StatusFilter,
    string? DepartmentFilter,
    int Page,
    int Limit
) : IRequest<Result<PaginatedList<RequisitionDto>>>;

public class GetRequisitionsHandler : IRequestHandler<GetRequisitions, Result<PaginatedList<RequisitionDto>>>
{
    private readonly IRequisitionRepository _repository;
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetRequisitionsHandler(IRequisitionRepository repository, IApplicationDbContext context, IUser user)
    {
        _repository = repository;
        _context = context;
        _user = user;
    }

    public async Task<Result<PaginatedList<RequisitionDto>>> Handle(GetRequisitions request, CancellationToken ct)
    {
        var paged = await _repository.GetPagedAsync(
            request.StatusFilter,
            request.DepartmentFilter,
            _user.Id!,
            _user.Roles!.First(),
            request.Page,
            request.Limit,
            ct);

        // Batch-fetch application counts for all requisitions in this page
        var reqIds = paged.Items.Select(r => r.Id).ToList();
        var appCounts = await _context.Applications
            .Where(a => reqIds.Contains(a.RequisitionId))
            .GroupBy(a => new { a.RequisitionId, a.Stage })
            .Select(g => new { g.Key.RequisitionId, g.Key.Stage, Count = g.Count() })
            .ToListAsync(ct);

        var dtos = paged.Items.Select(r =>
        {
            var counts = appCounts
                .Where(ac => ac.RequisitionId == r.Id)
                .ToDictionary(ac => ac.Stage, ac => ac.Count);

            return new RequisitionDto(
                r.Id, r.Title, r.Department, r.OwnerId, r.JdText,
                r.SalaryMin, r.SalaryMax, r.Headcount, r.Status,
                counts, r.Created, r.LastModified);
        }).ToList();

        return Result.Success(new PaginatedList<RequisitionDto>(
            dtos, paged.TotalCount, paged.Page, paged.Limit));
    }
}
```

**Note:** The `PaginatedList<T>` constructor is private. Use the `CreateAsync` factory instead. Refactor this handler to build the DTO list after fetching the paginated requisitions:

```csharp
// Alternative approach — project after pagination
var paged = await _repository.GetPagedAsync(...);
var reqIds = paged.Items.Select(r => r.Id).ToList();
var appCounts = await _context.Applications
    .Where(a => reqIds.Contains(a.RequisitionId))
    .GroupBy(a => new { a.RequisitionId, a.Stage })
    .Select(g => new { g.Key.RequisitionId, g.Key.Stage, Count = g.Count() })
    .ToListAsync(ct);

var dtos = paged.Items.Select(r =>
{
    var counts = appCounts
        .Where(ac => ac.RequisitionId == r.Id)
        .ToDictionary(ac => ac.Stage, ac => ac.Count);
    return new RequisitionDto(
        r.Id, r.Title, r.Department, r.OwnerId, r.JdText,
        r.SalaryMin, r.SalaryMax, r.Headcount, r.Status,
        counts, r.Created, r.LastModified);
}).ToList();

// PaginatedList.CreateAsync works on IQueryable; for in-memory projection,
// wrap manually or return the paged metadata alongside the DTO list.
return Result.Success(new PaginatedList<RequisitionDto>(
    dtos, paged.TotalCount, paged.Page, paged.Limit));
```

Since `PaginatedList<T>` has a private constructor, add a public static factory method for in-memory construction. Modify `src/Application/Common/Models/PaginatedList.cs`:

```csharp
// Add this static method to PaginatedList<T>:
public static PaginatedList<T> Create(IReadOnlyList<T> items, int totalCount, int page, int limit)
{
    return new PaginatedList<T>(items, totalCount, page, limit);
}
```

Then use `PaginatedList<RequisitionDto>.Create(dtos, paged.TotalCount, paged.Page, paged.Limit)` in the handler.

### Step 4: Create endpoints

- [ ] **Step 4: Create `src/Web/Endpoints/Requisitions.cs`**

```csharp
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Requisitions.Commands;
using RC.HyRe.Application.Requisitions.Queries;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Web.Endpoints;

public class Requisitions : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Create, "");
        groupBuilder.MapGet(GetAll, "");
        groupBuilder.MapGet(GetById, "{id}");
        groupBuilder.MapPut(Update, "{id}");
        groupBuilder.MapPost(Submit, "{id}/submit");
        groupBuilder.MapPost(Approve, "{id}/approve");
        groupBuilder.MapPost(Reject, "{id}/reject");
        groupBuilder.MapPost(Hold, "{id}/hold");
        groupBuilder.MapPost(Close, "{id}/close");
    }

    public static async Task<IResult> Create(ISender sender, CreateRequisition command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("CREATE_REQUISITION_FAILED", "Failed to create requisition.", result.Errors));
    }

    public static async Task<IResult> GetAll(
        ISender sender,
        RequisitionStatus? status,
        string? department,
        int page = 1,
        int limit = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var result = await sender.Send(new GetRequisitions(status, department, page, limit), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_REQUISITIONS_FAILED", "Failed to retrieve requisitions.", result.Errors));
    }

    public static async Task<IResult> GetById(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetRequisitionById(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.NotFound(ApiResponse.Fail("REQUISITION_NOT_FOUND", "Requisition not found.", result.Errors));
    }

    public static async Task<IResult> Update(ISender sender, Guid id, UpdateRequisitionBody body, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateRequisition(id, body.Title, body.Department, body.JdText, body.SalaryMin, body.SalaryMax, body.Headcount), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("UPDATE_REQUISITION_FAILED", "Failed to update requisition.", result.Errors));
    }

    public static async Task<IResult> Submit(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new SubmitForApproval(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("SUBMIT_FAILED", "Failed to submit requisition.", result.Errors));
    }

    public static async Task<IResult> Approve(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ApproveRequisition(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("APPROVE_FAILED", "Failed to approve requisition.", result.Errors));
    }

    public static async Task<IResult> Reject(ISender sender, Guid id, RejectRequisitionBody body, CancellationToken ct)
    {
        var result = await sender.Send(new RejectRequisition(id, body.Reason), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("REJECT_FAILED", "Failed to reject requisition.", result.Errors));
    }

    public static async Task<IResult> Hold(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new HoldRequisition(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("HOLD_FAILED", "Failed to hold requisition.", result.Errors));
    }

    public static async Task<IResult> Close(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new CloseRequisition(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("CLOSE_FAILED", "Failed to close requisition.", result.Errors));
    }
}

public record UpdateRequisitionBody(string Title, string Department, string JdText, int? SalaryMin, int? SalaryMax, int Headcount);
public record RejectRequisitionBody(string Reason);
```

### Step 5: Commit

- [ ] **Step 5: Commit**

```bash
git add src/Application/Requisitions/Queries/ src/Web/Endpoints/Requisitions.cs src/Application/Common/Models/PaginatedList.cs
git commit -m "feat(requisitions): add queries and API endpoints"
```

---

## Task 3: Candidates — Commands, Queries & Endpoints

**Files:**

- Create: `src/Application/Candidates/Commands/CreateCandidate.cs`
- Create: `src/Application/Candidates/Commands/UpdateCandidate.cs`
- Create: `src/Application/Candidates/Commands/ApplyToRequisition.cs`
- Create: `src/Application/Candidates/Commands/CandidateCommandValidator.cs`
- Create: `src/Application/Candidates/Queries/CandidateDto.cs`
- Create: `src/Application/Candidates/Queries/GetCandidateById.cs`
- Create: `src/Application/Candidates/Queries/GetCandidates.cs`
- Create: `src/Web/Endpoints/Candidates.cs`
- Modify: `src/Application/Common/Interfaces/Repositories/ICandidateRepository.cs`
- Modify: `src/Infrastructure/Data/Repositories/CandidateRepository.cs`
- Modify: `src/Application/Common/Interfaces/Repositories/IApplicationRepository.cs`
- Modify: `src/Infrastructure/Data/Repositories/ApplicationRepository.cs`

### Step 1: Add repository methods

- [ ] **Step 1a: Add to `ICandidateRepository`**

```csharp
// Add to interface:
Task AddAsync(Candidate candidate, CancellationToken ct = default);
Task UpdateAsync(Candidate candidate, CancellationToken ct = default);
```

- [ ] **Step 1b: Add to `CandidateRepository`**

```csharp
public async Task AddAsync(Candidate candidate, CancellationToken ct = default)
{
    _context.Candidates.Add(candidate);
    await _context.SaveChangesAsync(ct);
}

public async Task UpdateAsync(Candidate candidate, CancellationToken ct = default)
{
    _context.Candidates.Update(candidate);
    await _context.SaveChangesAsync(ct);
}
```

- [ ] **Step 1c: Add to `IApplicationRepository`**

```csharp
// Add to interface:
Task AddAsync(JobApplication application, CancellationToken ct = default);
```

- [ ] **Step 1d: Add to `ApplicationRepository`**

```csharp
public async Task AddAsync(JobApplication application, CancellationToken ct = default)
{
    _context.Applications.Add(application);
    await _context.SaveChangesAsync(ct);
}
```

### Step 2: Create commands

- [ ] **Step 2a: Create `src/Application/Candidates/Commands/CreateCandidate.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;
using RC.HyRe.Domain.Events;

namespace RC.HyRe.Application.Candidates.Commands;

[Authorize(Permissions = Permissions.CandidatesCreate)]
public record CreateCandidate(
    string Name,
    string Email,
    string? Phone,
    CandidateSource Source,
    string? SourceDetail
) : IRequest<Result<Guid>>;

public class CreateCandidateHandler : IRequestHandler<CreateCandidate, Result<Guid>>
{
    private readonly ICandidateRepository _repository;

    public CreateCandidateHandler(ICandidateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateCandidate request, CancellationToken ct)
    {
        var exists = await _repository.ExistsWithEmailAsync(request.Email, ct);
        if (exists)
            return Result.Failure<Guid>("A candidate with this email already exists.");

        var candidate = new Candidate
        {
            Name = request.Name,
            Email = request.Email.ToLowerInvariant(),
            Phone = request.Phone,
            Source = request.Source,
            SourceDetail = request.SourceDetail
        };

        candidate.AddDomainEvent(new CandidateCreatedEvent(candidate.Id, candidate.Email, null));

        await _repository.AddAsync(candidate, ct);
        return Result.Success(candidate.Id);
    }
}
```

- [ ] **Step 2b: Create `src/Application/Candidates/Commands/UpdateCandidate.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Candidates.Commands;

[Authorize(Permissions = Permissions.CandidatesUpdate)]
public record UpdateCandidate(
    Guid Id,
    string Name,
    string? Phone,
    CandidateSource Source,
    string? SourceDetail
) : IRequest<Result>;

public class UpdateCandidateHandler : IRequestHandler<UpdateCandidate, Result>
{
    private readonly ICandidateRepository _repository;

    public UpdateCandidateHandler(ICandidateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateCandidate request, CancellationToken ct)
    {
        var candidate = await _repository.GetByIdAsync(request.Id, ct);
        if (candidate is null)
            return Result.Failure("Candidate not found.");

        candidate.Name = request.Name;
        candidate.Phone = request.Phone;
        candidate.Source = request.Source;
        candidate.SourceDetail = request.SourceDetail;

        await _repository.UpdateAsync(candidate, ct);
        return Result.Success();
    }
}
```

- [ ] **Step 2c: Create `src/Application/Candidates/Commands/ApplyToRequisition.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Candidates.Commands;

[Authorize(Permissions = Permissions.CandidatesCreate)]
public record ApplyToRequisition(Guid CandidateId, Guid RequisitionId) : IRequest<Result<Guid>>;

public class ApplyToRequisitionHandler : IRequestHandler<ApplyToRequisition, Result<Guid>>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly IApplicationRepository _applicationRepository;

    public ApplyToRequisitionHandler(
        ICandidateRepository candidateRepository,
        IRequisitionRepository requisitionRepository,
        IApplicationRepository applicationRepository)
    {
        _candidateRepository = candidateRepository;
        _requisitionRepository = requisitionRepository;
        _applicationRepository = applicationRepository;
    }

    public async Task<Result<Guid>> Handle(ApplyToRequisition request, CancellationToken ct)
    {
        var candidate = await _candidateRepository.GetByIdAsync(request.CandidateId, ct);
        if (candidate is null)
            return Result.Failure<Guid>("Candidate not found.");

        var requisition = await _requisitionRepository.GetByIdAsync(request.RequisitionId, ct);
        if (requisition is null)
            return Result.Failure<Guid>("Requisition not found.");

        if (requisition.Status != RequisitionStatus.Open)
            return Result.Failure<Guid>("Can only apply to open requisitions.");

        var duplicate = await _applicationRepository.ExistsDuplicateAsync(request.CandidateId, request.RequisitionId, ct);
        if (duplicate)
            return Result.Failure<Guid>("Candidate already applied to this requisition.");

        var application = new JobApplication
        {
            CandidateId = request.CandidateId,
            RequisitionId = request.RequisitionId,
            Stage = ApplicationStage.Applied
        };

        await _applicationRepository.AddAsync(application, ct);
        return Result.Success(application.Id);
    }
}
```

### Step 3: Create validators

- [ ] **Step 3: Create `src/Application/Candidates/Commands/CandidateCommandValidator.cs`**

```csharp
namespace RC.HyRe.Application.Candidates.Commands;

public class CreateCandidateValidator : AbstractValidator<CreateCandidate>
{
    public CreateCandidateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}

public class UpdateCandidateValidator : AbstractValidator<UpdateCandidate>
{
    public UpdateCandidateValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}

public class ApplyToRequisitionValidator : AbstractValidator<ApplyToRequisition>
{
    public ApplyToRequisitionValidator()
    {
        RuleFor(x => x.CandidateId).NotEmpty();
        RuleFor(x => x.RequisitionId).NotEmpty();
    }
}
```

### Step 4: Create queries

- [ ] **Step 4a: Create `src/Application/Candidates/Queries/CandidateDto.cs`**

```csharp
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Candidates.Queries;

public record CandidateDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    CandidateSource Source,
    string? SourceDetail,
    Guid? ResumeDocId,
    List<CandidateApplicationSummary> Applications,
    DateTimeOffset Created
);

public record CandidateApplicationSummary(
    Guid ApplicationId,
    Guid RequisitionId,
    string RequisitionTitle,
    ApplicationStage Stage,
    DateTimeOffset Created
);
```

- [ ] **Step 4b: Create `src/Application/Candidates/Queries/GetCandidateById.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;

namespace RC.HyRe.Application.Candidates.Queries;

[Authorize(Permissions = Permissions.CandidatesRead)]
public record GetCandidateById(Guid Id) : IRequest<Result<CandidateDto>>;

public class GetCandidateByIdHandler : IRequestHandler<GetCandidateById, Result<CandidateDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCandidateByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CandidateDto>> Handle(GetCandidateById request, CancellationToken ct)
    {
        var candidate = await _context.Candidates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (candidate is null)
            return Result.Failure<CandidateDto>("Candidate not found.");

        var applications = await _context.Applications
            .Where(a => a.CandidateId == request.Id)
            .Include(a => a.Requisition)
            .OrderByDescending(a => a.Created)
            .Select(a => new CandidateApplicationSummary(
                a.Id, a.RequisitionId, a.Requisition.Title, a.Stage, a.Created))
            .ToListAsync(ct);

        var dto = new CandidateDto(
            candidate.Id,
            candidate.Name,
            candidate.Email,
            candidate.Phone,
            candidate.Source,
            candidate.SourceDetail,
            candidate.ResumeDocId,
            applications,
            candidate.Created);

        return Result.Success(dto);
    }
}
```

- [ ] **Step 4c: Create `src/Application/Candidates/Queries/GetCandidates.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;

namespace RC.HyRe.Application.Candidates.Queries;

[Authorize(Permissions = Permissions.CandidatesRead)]
public record GetCandidates(
    string? NameFilter,
    int Page,
    int Limit
) : IRequest<Result<PaginatedList<CandidateDto>>>;

public class GetCandidatesHandler : IRequestHandler<GetCandidates, Result<PaginatedList<CandidateDto>>>
{
    private readonly ICandidateRepository _repository;
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetCandidatesHandler(ICandidateRepository repository, IApplicationDbContext context, IUser user)
    {
        _repository = repository;
        _context = context;
        _user = user;
    }

    public async Task<Result<PaginatedList<CandidateDto>>> Handle(GetCandidates request, CancellationToken ct)
    {
        var paged = await _repository.GetPagedAsync(
            request.NameFilter,
            _user.Id!,
            _user.Roles!.First(),
            request.Page,
            request.Limit,
            ct);

        var candidateIds = paged.Items.Select(c => c.Id).ToList();

        var applications = await _context.Applications
            .Where(a => candidateIds.Contains(a.CandidateId))
            .Include(a => a.Requisition)
            .Select(a => new { a.CandidateId, a.Id, a.RequisitionId, a.Requisition.Title, a.Stage, a.Created })
            .ToListAsync(ct);

        var dtos = paged.Items.Select(c =>
        {
            var apps = applications
                .Where(a => a.CandidateId == c.Id)
                .Select(a => new CandidateApplicationSummary(a.Id, a.RequisitionId, a.Title, a.Stage, a.Created))
                .ToList();

            return new CandidateDto(
                c.Id, c.Name, c.Email, c.Phone, c.Source, c.SourceDetail,
                c.ResumeDocId, apps, c.Created);
        }).ToList();

        return Result.Success(PaginatedList<CandidateDto>.Create(dtos, paged.TotalCount, paged.Page, paged.Limit));
    }
}
```

### Step 5: Create endpoints

- [ ] **Step 5: Create `src/Web/Endpoints/Candidates.cs`**

```csharp
using RC.HyRe.Application.Candidates.Commands;
using RC.HyRe.Application.Candidates.Queries;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Web.Endpoints;

public class Candidates : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Create, "");
        groupBuilder.MapGet(GetAll, "");
        groupBuilder.MapGet(GetById, "{id}");
        groupBuilder.MapPut(Update, "{id}");
        groupBuilder.MapPost(Apply, "{id}/apply");
    }

    public static async Task<IResult> Create(ISender sender, CreateCandidate command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("CREATE_CANDIDATE_FAILED", "Failed to create candidate.", result.Errors));
    }

    public static async Task<IResult> GetAll(
        ISender sender,
        string? name,
        int page = 1,
        int limit = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var result = await sender.Send(new GetCandidates(name, page, limit), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_CANDIDATES_FAILED", "Failed to retrieve candidates.", result.Errors));
    }

    public static async Task<IResult> GetById(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCandidateById(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.NotFound(ApiResponse.Fail("CANDIDATE_NOT_FOUND", "Candidate not found.", result.Errors));
    }

    public static async Task<IResult> Update(ISender sender, Guid id, UpdateCandidateBody body, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateCandidate(id, body.Name, body.Phone, body.Source, body.SourceDetail), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("UPDATE_CANDIDATE_FAILED", "Failed to update candidate.", result.Errors));
    }

    public static async Task<IResult> Apply(ISender sender, Guid id, ApplyBody body, CancellationToken ct)
    {
        var result = await sender.Send(new ApplyToRequisition(id, body.RequisitionId), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("APPLY_FAILED", "Failed to apply candidate to requisition.", result.Errors));
    }
}

public record UpdateCandidateBody(string Name, string? Phone, RC.HyRe.Domain.Enums.CandidateSource Source, string? SourceDetail);
public record ApplyBody(Guid RequisitionId);
```

### Step 6: Commit

- [ ] **Step 6: Commit**

```bash
git add src/Application/Candidates/ src/Web/Endpoints/Candidates.cs src/Application/Common/Interfaces/Repositories/ICandidateRepository.cs src/Infrastructure/Data/Repositories/CandidateRepository.cs src/Application/Common/Interfaces/Repositories/IApplicationRepository.cs src/Infrastructure/Data/Repositories/ApplicationRepository.cs
git commit -m "feat(candidates): add CRUD commands, queries, and API endpoints"
```

---

## Task 4: Pipeline — Commands, Queries & Endpoints

**Files:**

- Create: `src/Application/Pipeline/Commands/AdvanceApplicationStage.cs`
- Create: `src/Application/Pipeline/Commands/RejectApplication.cs`
- Create: `src/Application/Pipeline/Commands/BulkAdvanceStage.cs`
- Create: `src/Application/Pipeline/Commands/PipelineCommandValidator.cs`
- Create: `src/Application/Pipeline/Queries/PipelineDto.cs`
- Create: `src/Application/Pipeline/Queries/GetPipelineByRequisition.cs`
- Create: `src/Application/Pipeline/Queries/GetApplicationById.cs`
- Create: `src/Web/Endpoints/Pipeline.cs`

### Step 1: Create commands

- [ ] **Step 1a: Create `src/Application/Pipeline/Commands/AdvanceApplicationStage.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Pipeline.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record AdvanceApplicationStage(Guid ApplicationId, ApplicationStage NewStage) : IRequest<Result>;

public class AdvanceApplicationStageHandler : IRequestHandler<AdvanceApplicationStage, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public AdvanceApplicationStageHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result> Handle(AdvanceApplicationStage request, CancellationToken ct)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        if (application is null)
            return Result.Failure("Application not found.");

        if (request.NewStage == ApplicationStage.Rejected)
            return Result.Failure("Use the Reject command to reject an application.");

        application.AdvanceStage(request.NewStage, _user.Id);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

- [ ] **Step 1b: Create `src/Application/Pipeline/Commands/RejectApplication.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Pipeline.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record RejectApplication(Guid ApplicationId, string? Reason) : IRequest<Result>;

public class RejectApplicationHandler : IRequestHandler<RejectApplication, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public RejectApplicationHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result> Handle(RejectApplication request, CancellationToken ct)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        if (application is null)
            return Result.Failure("Application not found.");

        application.Reject(request.Reason, _user.Id);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

- [ ] **Step 1c: Create `src/Application/Pipeline/Commands/BulkAdvanceStage.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Pipeline.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record BulkAdvanceStage(List<Guid> ApplicationIds, ApplicationStage NewStage) : IRequest<Result<int>>;

public class BulkAdvanceStageHandler : IRequestHandler<BulkAdvanceStage, Result<int>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public BulkAdvanceStageHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<int>> Handle(BulkAdvanceStage request, CancellationToken ct)
    {
        if (request.NewStage == ApplicationStage.Rejected)
            return Result.Failure<int>("Use RejectApplication for rejections.");

        var applications = await _context.Applications
            .Where(a => request.ApplicationIds.Contains(a.Id))
            .ToListAsync(ct);

        if (applications.Count != request.ApplicationIds.Count)
            return Result.Failure<int>("One or more applications not found.");

        foreach (var app in applications)
        {
            app.AdvanceStage(request.NewStage, _user.Id);
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success(applications.Count);
    }
}
```

### Step 2: Create validators

- [ ] **Step 2: Create `src/Application/Pipeline/Commands/PipelineCommandValidator.cs`**

```csharp
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Pipeline.Commands;

public class AdvanceApplicationStageValidator : AbstractValidator<AdvanceApplicationStage>
{
    public AdvanceApplicationStageValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.NewStage).IsInEnum();
        RuleFor(x => x.NewStage).NotEqual(ApplicationStage.Rejected)
            .WithMessage("Use the Reject command to reject an application.");
    }
}

public class RejectApplicationValidator : AbstractValidator<RejectApplication>
{
    public RejectApplicationValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}

public class BulkAdvanceStageValidator : AbstractValidator<BulkAdvanceStage>
{
    public BulkAdvanceStageValidator()
    {
        RuleFor(x => x.ApplicationIds).NotEmpty();
        RuleFor(x => x.NewStage).IsInEnum();
        RuleFor(x => x.NewStage).NotEqual(ApplicationStage.Rejected);
    }
}
```

### Step 3: Create queries

- [ ] **Step 3a: Create `src/Application/Pipeline/Queries/PipelineDto.cs`**

```csharp
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Pipeline.Queries;

public record PipelineDto(
    Guid RequisitionId,
    string RequisitionTitle,
    List<PipelineStageGroup> Stages
);

public record PipelineStageGroup(
    ApplicationStage Stage,
    List<PipelineApplicationCard> Applications
);

public record PipelineApplicationCard(
    Guid ApplicationId,
    Guid CandidateId,
    string CandidateName,
    string CandidateEmail,
    ApplicationStage Stage,
    int DaysInStage,
    DateTimeOffset Created
);

public record ApplicationDetailDto(
    Guid Id,
    Guid CandidateId,
    string CandidateName,
    string CandidateEmail,
    Guid RequisitionId,
    string RequisitionTitle,
    ApplicationStage Stage,
    string? RejectionReason,
    List<ApplicationInterviewSummary> Interviews,
    DateTimeOffset Created
);

public record ApplicationInterviewSummary(
    Guid InterviewId,
    string InterviewerId,
    InterviewType Type,
    DateTimeOffset ScheduledAt,
    InterviewStatus Status,
    bool HasScorecard
);
```

- [ ] **Step 3b: Create `src/Application/Pipeline/Queries/GetPipelineByRequisition.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Pipeline.Queries;

[Authorize(Permissions = Permissions.PipelineRead)]
public record GetPipelineByRequisition(Guid RequisitionId) : IRequest<Result<PipelineDto>>;

public class GetPipelineByRequisitionHandler : IRequestHandler<GetPipelineByRequisition, Result<PipelineDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPipelineByRequisitionHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PipelineDto>> Handle(GetPipelineByRequisition request, CancellationToken ct)
    {
        var requisition = await _context.Requisitions
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RequisitionId, ct);

        if (requisition is null)
            return Result.Failure<PipelineDto>("Requisition not found.");

        var applications = await _context.Applications
            .AsNoTracking()
            .Where(a => a.RequisitionId == request.RequisitionId && a.Stage != ApplicationStage.Rejected)
            .Include(a => a.Candidate)
            .OrderByDescending(a => a.Created)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var stages = Enum.GetValues<ApplicationStage>()
            .Where(s => s != ApplicationStage.Rejected)
            .Select(stage =>
            {
                var cards = applications
                    .Where(a => a.Stage == stage)
                    .Select(a => new PipelineApplicationCard(
                        a.Id,
                        a.CandidateId,
                        a.Candidate.Name,
                        a.Candidate.Email,
                        a.Stage,
                        (int)(now - a.LastModified).TotalDays,
                        a.Created))
                    .ToList();

                return new PipelineStageGroup(stage, cards);
            })
            .ToList();

        return Result.Success(new PipelineDto(requisition.Id, requisition.Title, stages));
    }
}
```

- [ ] **Step 3c: Create `src/Application/Pipeline/Queries/GetApplicationById.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Pipeline.Queries;

[Authorize(Permissions = Permissions.PipelineRead)]
public record GetApplicationById(Guid Id) : IRequest<Result<ApplicationDetailDto>>;

public class GetApplicationByIdHandler : IRequestHandler<GetApplicationById, Result<ApplicationDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetApplicationByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ApplicationDetailDto>> Handle(GetApplicationById request, CancellationToken ct)
    {
        var application = await _context.Applications
            .AsNoTracking()
            .Include(a => a.Candidate)
            .Include(a => a.Requisition)
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct);

        if (application is null)
            return Result.Failure<ApplicationDetailDto>("Application not found.");

        var interviews = await _context.Interviews
            .AsNoTracking()
            .Where(i => i.ApplicationId == request.Id)
            .Include(i => i.Scorecard)
            .OrderByDescending(i => i.ScheduledAt)
            .Select(i => new ApplicationInterviewSummary(
                i.Id,
                i.InterviewerId,
                i.Type,
                i.ScheduledAt,
                i.Status,
                i.Scorecard != null))
            .ToListAsync(ct);

        var dto = new ApplicationDetailDto(
            application.Id,
            application.CandidateId,
            application.Candidate.Name,
            application.Candidate.Email,
            application.RequisitionId,
            application.Requisition.Title,
            application.Stage,
            application.RejectionReason,
            interviews,
            application.Created);

        return Result.Success(dto);
    }
}
```

### Step 4: Create endpoints

- [ ] **Step 4: Create `src/Web/Endpoints/Pipeline.cs`**

```csharp
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Pipeline.Commands;
using RC.HyRe.Application.Pipeline.Queries;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Web.Endpoints;

public class Pipeline : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetByRequisition, "requisition/{requisitionId}");
        groupBuilder.MapGet(GetApplication, "applications/{id}");
        groupBuilder.MapPost(Advance, "applications/{id}/advance");
        groupBuilder.MapPost(Reject, "applications/{id}/reject");
        groupBuilder.MapPost(BulkAdvance, "applications/bulk-advance");
    }

    public static async Task<IResult> GetByRequisition(ISender sender, Guid requisitionId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPipelineByRequisition(requisitionId), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.NotFound(ApiResponse.Fail("PIPELINE_NOT_FOUND", "Pipeline not found.", result.Errors));
    }

    public static async Task<IResult> GetApplication(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetApplicationById(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.NotFound(ApiResponse.Fail("APPLICATION_NOT_FOUND", "Application not found.", result.Errors));
    }

    public static async Task<IResult> Advance(ISender sender, Guid id, AdvanceBody body, CancellationToken ct)
    {
        var result = await sender.Send(new AdvanceApplicationStage(id, body.NewStage), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("ADVANCE_FAILED", "Failed to advance stage.", result.Errors));
    }

    public static async Task<IResult> Reject(ISender sender, Guid id, PipelineRejectBody body, CancellationToken ct)
    {
        var result = await sender.Send(new RejectApplication(id, body.Reason), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("REJECT_FAILED", "Failed to reject application.", result.Errors));
    }

    public static async Task<IResult> BulkAdvance(ISender sender, BulkAdvanceBody body, CancellationToken ct)
    {
        var result = await sender.Send(new BulkAdvanceStage(body.ApplicationIds, body.NewStage), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("BULK_ADVANCE_FAILED", "Failed to advance applications.", result.Errors));
    }
}

public record AdvanceBody(ApplicationStage NewStage);
public record PipelineRejectBody(string? Reason);
public record BulkAdvanceBody(List<Guid> ApplicationIds, ApplicationStage NewStage);
```

### Step 5: Commit

- [ ] **Step 5: Commit**

```bash
git add src/Application/Pipeline/ src/Web/Endpoints/Pipeline.cs
git commit -m "feat(pipeline): add stage management commands, queries, and endpoints"
```

---

## Task 5: Interviews — Commands, Queries & Endpoints

**Files:**

- Create: `src/Application/Interviews/Commands/ScheduleInterview.cs`
- Create: `src/Application/Interviews/Commands/RescheduleInterview.cs`
- Create: `src/Application/Interviews/Commands/CancelInterview.cs`
- Create: `src/Application/Interviews/Commands/MarkNoShow.cs`
- Create: `src/Application/Interviews/Commands/MarkCompleted.cs`
- Create: `src/Application/Interviews/Commands/InterviewCommandValidator.cs`
- Create: `src/Application/Interviews/Queries/InterviewDto.cs`
- Create: `src/Application/Interviews/Queries/GetInterviewsByApplication.cs`
- Create: `src/Application/Interviews/Queries/GetInterviewsByInterviewer.cs`
- Create: `src/Web/Endpoints/Interviews.cs`
- Modify: `src/Application/Common/Interfaces/Repositories/IInterviewRepository.cs`
- Modify: `src/Infrastructure/Data/Repositories/InterviewRepository.cs`
- Modify: `src/Application/Common/Interfaces/Repositories/IScorecardRepository.cs`
- Modify: `src/Infrastructure/Data/Repositories/ScorecardRepository.cs`

### Step 1: Add repository methods

- [ ] **Step 1a: Add to `IInterviewRepository`**

```csharp
// Add to interface:
Task AddAsync(Interview interview, CancellationToken ct = default);
Task UpdateAsync(Interview interview, CancellationToken ct = default);
```

- [ ] **Step 1b: Add to `InterviewRepository`**

```csharp
public async Task AddAsync(Interview interview, CancellationToken ct = default)
{
    _context.Interviews.Add(interview);
    await _context.SaveChangesAsync(ct);
}

public async Task UpdateAsync(Interview interview, CancellationToken ct = default)
{
    _context.Interviews.Update(interview);
    await _context.SaveChangesAsync(ct);
}
```

- [ ] **Step 1c: Add to `IScorecardRepository`**

```csharp
// Add to interface:
Task AddAsync(Scorecard scorecard, CancellationToken ct = default);
```

- [ ] **Step 1d: Add to `ScorecardRepository`**

```csharp
public async Task AddAsync(Scorecard scorecard, CancellationToken ct = default)
{
    _context.Scorecards.Add(scorecard);
    await _context.SaveChangesAsync(ct);
}
```

### Step 2: Create commands

- [ ] **Step 2a: Create `src/Application/Interviews/Commands/ScheduleInterview.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record ScheduleInterview(
    Guid ApplicationId,
    string InterviewerId,
    InterviewType Type,
    DateTimeOffset ScheduledAt,
    int DurationMin,
    string? MeetingLink
) : IRequest<Result<Guid>>;

public class ScheduleInterviewHandler : IRequestHandler<ScheduleInterview, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IInterviewRepository _interviewRepository;
    private readonly IScorecardRepository _scorecardRepository;

    public ScheduleInterviewHandler(
        IApplicationDbContext context,
        IInterviewRepository interviewRepository,
        IScorecardRepository scorecardRepository)
    {
        _context = context;
        _interviewRepository = interviewRepository;
        _scorecardRepository = scorecardRepository;
    }

    public async Task<Result<Guid>> Handle(ScheduleInterview request, CancellationToken ct)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        if (application is null)
            return Result.Failure<Guid>("Application not found.");

        var interview = new Interview
        {
            ApplicationId = request.ApplicationId,
            InterviewerId = request.InterviewerId,
            Type = request.Type,
            ScheduledAt = request.ScheduledAt,
            DurationMin = request.DurationMin,
            MeetingLink = request.MeetingLink,
            Status = InterviewStatus.Scheduled
        };

        interview.Book();

        await _interviewRepository.AddAsync(interview, ct);

        // Auto-create scorecard for the interviewer
        var scorecard = new Scorecard
        {
            InterviewId = interview.Id,
            InterviewerId = request.InterviewerId,
            Strengths = string.Empty,
            Concerns = string.Empty
        };

        await _scorecardRepository.AddAsync(scorecard, ct);

        return Result.Success(interview.Id);
    }
}
```

- [ ] **Step 2b: Create `src/Application/Interviews/Commands/RescheduleInterview.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record RescheduleInterview(Guid InterviewId, DateTimeOffset NewScheduledAt) : IRequest<Result>;

public class RescheduleInterviewHandler : IRequestHandler<RescheduleInterview, Result>
{
    private readonly IInterviewRepository _repository;

    public RescheduleInterviewHandler(IInterviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RescheduleInterview request, CancellationToken ct)
    {
        var interview = await _repository.GetByIdAsync(request.InterviewId, ct);
        if (interview is null)
            return Result.Failure("Interview not found.");

        if (interview.Status != InterviewStatus.Scheduled)
            return Result.Failure("Only scheduled interviews can be rescheduled.");

        interview.ScheduledAt = request.NewScheduledAt;
        await _repository.UpdateAsync(interview, ct);
        return Result.Success();
    }
}
```

- [ ] **Step 2c: Create `src/Application/Interviews/Commands/CancelInterview.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record CancelInterview(Guid InterviewId, string? Reason) : IRequest<Result>;

public class CancelInterviewHandler : IRequestHandler<CancelInterview, Result>
{
    private readonly IInterviewRepository _repository;

    public CancelInterviewHandler(IInterviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(CancelInterview request, CancellationToken ct)
    {
        var interview = await _repository.GetByIdAsync(request.InterviewId, ct);
        if (interview is null)
            return Result.Failure("Interview not found.");

        if (interview.Status != InterviewStatus.Scheduled)
            return Result.Failure("Only scheduled interviews can be cancelled.");

        interview.Status = InterviewStatus.Cancelled;
        await _repository.UpdateAsync(interview, ct);
        return Result.Success();
    }
}
```

- [ ] **Step 2d: Create `src/Application/Interviews/Commands/MarkNoShow.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record MarkNoShow(Guid InterviewId) : IRequest<Result>;

public class MarkNoShowHandler : IRequestHandler<MarkNoShow, Result>
{
    private readonly IInterviewRepository _repository;

    public MarkNoShowHandler(IInterviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(MarkNoShow request, CancellationToken ct)
    {
        var interview = await _repository.GetByIdAsync(request.InterviewId, ct);
        if (interview is null)
            return Result.Failure("Interview not found.");

        if (interview.Status != InterviewStatus.Scheduled)
            return Result.Failure("Only scheduled interviews can be marked as no-show.");

        interview.Status = InterviewStatus.NoShow;
        await _repository.UpdateAsync(interview, ct);
        return Result.Success();
    }
}
```

- [ ] **Step 2e: Create `src/Application/Interviews/Commands/MarkCompleted.cs`**

```csharp
using RC.HyRe.Application.Common.Interfaces.Repositories;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Commands;

[Authorize(Permissions = Permissions.PipelineUpdate)]
public record MarkCompleted(Guid InterviewId) : IRequest<Result>;

public class MarkCompletedHandler : IRequestHandler<MarkCompleted, Result>
{
    private readonly IInterviewRepository _repository;

    public MarkCompletedHandler(IInterviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(MarkCompleted request, CancellationToken ct)
    {
        var interview = await _repository.GetByIdAsync(request.InterviewId, ct);
        if (interview is null)
            return Result.Failure("Interview not found.");

        if (interview.Status != InterviewStatus.Scheduled)
            return Result.Failure("Only scheduled interviews can be marked as completed.");

        interview.Status = InterviewStatus.Completed;
        await _repository.UpdateAsync(interview, ct);
        return Result.Success();
    }
}
```

### Step 3: Create validators

- [ ] **Step 3: Create `src/Application/Interviews/Commands/InterviewCommandValidator.cs`**

```csharp
namespace RC.HyRe.Application.Interviews.Commands;

public class ScheduleInterviewValidator : AbstractValidator<ScheduleInterview>
{
    public ScheduleInterviewValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.InterviewerId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ScheduledAt).GreaterThan(DateTimeOffset.UtcNow);
        RuleFor(x => x.DurationMin).InclusiveBetween(15, 480);
    }
}

public class RescheduleInterviewValidator : AbstractValidator<RescheduleInterview>
{
    public RescheduleInterviewValidator()
    {
        RuleFor(x => x.InterviewId).NotEmpty();
        RuleFor(x => x.NewScheduledAt).GreaterThan(DateTimeOffset.UtcNow);
    }
}
```

### Step 4: Create queries

- [ ] **Step 4a: Create `src/Application/Interviews/Queries/InterviewDto.cs`**

```csharp
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Queries;

public record InterviewDto(
    Guid Id,
    Guid ApplicationId,
    string CandidateName,
    string RequisitionTitle,
    string InterviewerId,
    InterviewType Type,
    DateTimeOffset ScheduledAt,
    int DurationMin,
    InterviewStatus Status,
    string? MeetingLink,
    bool HasScorecard
);
```

- [ ] **Step 4b: Create `src/Application/Interviews/Queries/GetInterviewsByApplication.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Interviews.Queries;

[Authorize(Permissions = Permissions.PipelineRead)]
public record GetInterviewsByApplication(Guid ApplicationId, int Page, int Limit)
    : IRequest<Result<PaginatedList<InterviewDto>>>;

public class GetInterviewsByApplicationHandler
    : IRequestHandler<GetInterviewsByApplication, Result<PaginatedList<InterviewDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetInterviewsByApplicationHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<InterviewDto>>> Handle(
        GetInterviewsByApplication request, CancellationToken ct)
    {
        var query = _context.Interviews
            .AsNoTracking()
            .Where(i => i.ApplicationId == request.ApplicationId)
            .Include(i => i.Application).ThenInclude(a => a.Candidate)
            .Include(i => i.Application).ThenInclude(a => a.Requisition)
            .Include(i => i.Scorecard)
            .OrderByDescending(i => i.ScheduledAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .Select(i => new InterviewDto(
                i.Id,
                i.ApplicationId,
                i.Application.Candidate.Name,
                i.Application.Requisition.Title,
                i.InterviewerId,
                i.Type,
                i.ScheduledAt,
                i.DurationMin,
                i.Status,
                i.MeetingLink,
                i.Scorecard != null))
            .ToListAsync(ct);

        return Result.Success(PaginatedList<InterviewDto>.Create(items, totalCount, request.Page, request.Limit));
    }
}
```

- [ ] **Step 4c: Create `src/Application/Interviews/Queries/GetInterviewsByInterviewer.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Interviews.Queries;

[Authorize(Roles = $"{Roles.Interviewer},{Roles.HiringManager},{Roles.HrAdmin}")]
public record GetInterviewsByInterviewer(
    InterviewStatus? StatusFilter,
    int Page,
    int Limit
) : IRequest<Result<PaginatedList<InterviewDto>>>;

public class GetInterviewsByInterviewerHandler
    : IRequestHandler<GetInterviewsByInterviewer, Result<PaginatedList<InterviewDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetInterviewsByInterviewerHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<PaginatedList<InterviewDto>>> Handle(
        GetInterviewsByInterviewer request, CancellationToken ct)
    {
        var query = _context.Interviews
            .AsNoTracking()
            .Where(i => i.InterviewerId == _user.Id);

        if (request.StatusFilter.HasValue)
            query = query.Where(i => i.Status == request.StatusFilter.Value);

        query = query
            .Include(i => i.Application).ThenInclude(a => a.Candidate)
            .Include(i => i.Application).ThenInclude(a => a.Requisition)
            .Include(i => i.Scorecard)
            .OrderByDescending(i => i.ScheduledAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .Select(i => new InterviewDto(
                i.Id,
                i.ApplicationId,
                i.Application.Candidate.Name,
                i.Application.Requisition.Title,
                i.InterviewerId,
                i.Type,
                i.ScheduledAt,
                i.DurationMin,
                i.Status,
                i.MeetingLink,
                i.Scorecard != null))
            .ToListAsync(ct);

        return Result.Success(PaginatedList<InterviewDto>.Create(items, totalCount, request.Page, request.Limit));
    }
}
```

### Step 5: Create endpoints

- [ ] **Step 5: Create `src/Web/Endpoints/Interviews.cs`**

```csharp
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Interviews.Commands;
using RC.HyRe.Application.Interviews.Queries;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Web.Endpoints;

public class Interviews : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Schedule, "");
        groupBuilder.MapGet(GetByApplication, "application/{applicationId}");
        groupBuilder.MapGet(GetMy, "my");
        groupBuilder.MapPut(Reschedule, "{id}/reschedule");
        groupBuilder.MapPost(Cancel, "{id}/cancel");
        groupBuilder.MapPost(NoShow, "{id}/no-show");
        groupBuilder.MapPost(Complete, "{id}/complete");
    }

    public static async Task<IResult> Schedule(ISender sender, ScheduleInterview command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("SCHEDULE_FAILED", "Failed to schedule interview.", result.Errors));
    }

    public static async Task<IResult> GetByApplication(
        ISender sender, Guid applicationId, int page = 1, int limit = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var result = await sender.Send(new GetInterviewsByApplication(applicationId, page, limit), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_INTERVIEWS_FAILED", "Failed to retrieve interviews.", result.Errors));
    }

    public static async Task<IResult> GetMy(
        ISender sender, InterviewStatus? status, int page = 1, int limit = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var result = await sender.Send(new GetInterviewsByInterviewer(status, page, limit), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_INTERVIEWS_FAILED", "Failed to retrieve interviews.", result.Errors));
    }

    public static async Task<IResult> Reschedule(ISender sender, Guid id, RescheduleBody body, CancellationToken ct)
    {
        var result = await sender.Send(new RescheduleInterview(id, body.NewScheduledAt), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("RESCHEDULE_FAILED", "Failed to reschedule interview.", result.Errors));
    }

    public static async Task<IResult> Cancel(ISender sender, Guid id, CancelBody body, CancellationToken ct)
    {
        var result = await sender.Send(new CancelInterview(id, body.Reason), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("CANCEL_FAILED", "Failed to cancel interview.", result.Errors));
    }

    public static async Task<IResult> NoShow(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkNoShow(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("NO_SHOW_FAILED", "Failed to mark no-show.", result.Errors));
    }

    public static async Task<IResult> Complete(ISender sender, Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkCompleted(id), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("COMPLETE_FAILED", "Failed to mark completed.", result.Errors));
    }
}

public record RescheduleBody(DateTimeOffset NewScheduledAt);
public record CancelBody(string? Reason);
```

### Step 6: Commit

- [ ] **Step 6: Commit**

```bash
git add src/Application/Interviews/ src/Web/Endpoints/Interviews.cs src/Application/Common/Interfaces/Repositories/IInterviewRepository.cs src/Infrastructure/Data/Repositories/InterviewRepository.cs src/Application/Common/Interfaces/Repositories/IScorecardRepository.cs src/Infrastructure/Data/Repositories/ScorecardRepository.cs
git commit -m "feat(interviews): add scheduling commands, queries, and endpoints"
```

---

## Task 6: Scorecards — Commands, Queries & Endpoints

**Files:**

- Create: `src/Application/Scorecards/Commands/SubmitScorecard.cs`
- Create: `src/Application/Scorecards/Commands/ScorecardCommandValidator.cs`
- Create: `src/Application/Scorecards/Queries/ScorecardDto.cs`
- Create: `src/Application/Scorecards/Queries/GetScorecardByInterview.cs`
- Create: `src/Application/Scorecards/Queries/GetScorecardsByInterviewer.cs`
- Create: `src/Application/Scorecards/Queries/GetScorecardsByApplication.cs`
- Create: `src/Web/Endpoints/Scorecards.cs`

### Step 1: Create command

- [ ] **Step 1: Create `src/Application/Scorecards/Commands/SubmitScorecard.cs`**

```csharp
using System.Text.Json;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Scorecards.Commands;

[Authorize(Permissions = Permissions.ScorecardsUpdate)]
public record SubmitScorecard(
    Guid Id,
    Dictionary<string, int> Ratings,
    string Recommendation,
    string Strengths,
    string Concerns,
    string? Notes
) : IRequest<Result>;

public class SubmitScorecardHandler : IRequestHandler<SubmitScorecard, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SubmitScorecardHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result> Handle(SubmitScorecard request, CancellationToken ct)
    {
        var scorecard = await _context.Scorecards
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (scorecard is null)
            return Result.Failure("Scorecard not found.");

        if (scorecard.InterviewerId != _user.Id)
            return Result.Failure("You can only submit your own scorecard.");

        if (scorecard.SubmittedAt.HasValue)
            return Result.Failure("Scorecard has already been submitted.");

        scorecard.Ratings = JsonDocument.Parse(JsonSerializer.Serialize(request.Ratings));
        scorecard.Recommendation = Enum.Parse<Domain.Enums.ScorecardRecommendation>(request.Recommendation);
        scorecard.Strengths = request.Strengths;
        scorecard.Concerns = request.Concerns;
        scorecard.Notes = request.Notes;

        scorecard.Submit(_user.Id);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

### Step 2: Create validator

- [ ] **Step 2: Create `src/Application/Scorecards/Commands/ScorecardCommandValidator.cs`**

```csharp
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Scorecards.Commands;

public class SubmitScorecardValidator : AbstractValidator<SubmitScorecard>
{
    private static readonly string[] RequiredDimensions = ["technical", "communication", "problemSolving", "cultureFit"];

    public SubmitScorecardValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Ratings).NotEmpty();
        RuleFor(x => x.Ratings).Must(r => RequiredDimensions.All(d => r.ContainsKey(d)))
            .WithMessage("Ratings must include: technical, communication, problemSolving, cultureFit.");
        RuleFor(x => x.Ratings).Must(r => r.Values.All(v => v is >= 1 and <= 5))
            .WithMessage("All ratings must be between 1 and 5.");
        RuleFor(x => x.Recommendation).NotEmpty()
            .Must(r => Enum.TryParse<ScorecardRecommendation>(r, out _))
            .WithMessage("Recommendation must be one of: StrongYes, Yes, No, StrongNo.");
        RuleFor(x => x.Strengths).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Concerns).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
```

### Step 3: Create queries

- [ ] **Step 3a: Create `src/Application/Scorecards/Queries/ScorecardDto.cs`**

```csharp
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Scorecards.Queries;

public record ScorecardDto(
    Guid Id,
    Guid InterviewId,
    string InterviewerId,
    Dictionary<string, int> Ratings,
    ScorecardRecommendation Recommendation,
    string Strengths,
    string Concerns,
    string? Notes,
    DateTimeOffset? SubmittedAt,
    bool IsSubmitted
);
```

- [ ] **Step 3b: Create `src/Application/Scorecards/Queries/GetScorecardByInterview.cs`**

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Scorecards.Queries;

[Authorize(Permissions = Permissions.ScorecardsRead)]
public record GetScorecardByInterview(Guid InterviewId) : IRequest<Result<ScorecardDto>>;

public class GetScorecardByInterviewHandler : IRequestHandler<GetScorecardByInterview, Result<ScorecardDto>>
{
    private readonly IApplicationDbContext _context;

    public GetScorecardByInterviewHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ScorecardDto>> Handle(GetScorecardByInterview request, CancellationToken ct)
    {
        var scorecard = await _context.Scorecards
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.InterviewId == request.InterviewId, ct);

        if (scorecard is null)
            return Result.Failure<ScorecardDto>("Scorecard not found.");

        return Result.Success(MapToDto(scorecard));
    }

    private static ScorecardDto MapToDto(Domain.Entities.Scorecard scorecard)
    {
        var ratings = new Dictionary<string, int>();
        if (scorecard.Ratings.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in scorecard.Ratings.RootElement.EnumerateObject())
            {
                if (prop.Value.TryGetInt32(out var val))
                    ratings[prop.Name] = val;
            }
        }

        return new ScorecardDto(
            scorecard.Id,
            scorecard.InterviewId,
            scorecard.InterviewerId,
            ratings,
            scorecard.Recommendation,
            scorecard.Strengths,
            scorecard.Concerns,
            scorecard.Notes,
            scorecard.SubmittedAt,
            scorecard.SubmittedAt.HasValue);
    }
}
```

- [ ] **Step 3c: Create `src/Application/Scorecards/Queries/GetScorecardsByInterviewer.cs`**

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;

namespace RC.HyRe.Application.Scorecards.Queries;

[Authorize(Roles = $"{Roles.Interviewer},{Roles.HiringManager},{Roles.HrAdmin}")]
public record GetScorecardsByInterviewer(int Page, int Limit)
    : IRequest<Result<PaginatedList<ScorecardDto>>>;

public class GetScorecardsByInterviewerHandler
    : IRequestHandler<GetScorecardsByInterviewer, Result<PaginatedList<ScorecardDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetScorecardsByInterviewerHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<PaginatedList<ScorecardDto>>> Handle(
        GetScorecardsByInterviewer request, CancellationToken ct)
    {
        var query = _context.Scorecards
            .AsNoTracking()
            .Where(s => s.InterviewerId == _user.Id)
            .OrderByDescending(s => s.Created);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result.Success(PaginatedList<ScorecardDto>.Create(dtos, totalCount, request.Page, request.Limit));
    }

    private static ScorecardDto MapToDto(Domain.Entities.Scorecard scorecard)
    {
        var ratings = new Dictionary<string, int>();
        if (scorecard.Ratings.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in scorecard.Ratings.RootElement.EnumerateObject())
            {
                if (prop.Value.TryGetInt32(out var val))
                    ratings[prop.Name] = val;
            }
        }

        return new ScorecardDto(
            scorecard.Id,
            scorecard.InterviewId,
            scorecard.InterviewerId,
            ratings,
            scorecard.Recommendation,
            scorecard.Strengths,
            scorecard.Concerns,
            scorecard.Notes,
            scorecard.SubmittedAt,
            scorecard.SubmittedAt.HasValue);
    }
}
```

- [ ] **Step 3d: Create `src/Application/Scorecards/Queries/GetScorecardsByApplication.cs`**

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Common.Security;
using RC.HyRe.Domain.Constants;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Scorecards.Queries;

[Authorize(Permissions = Permissions.ScorecardsRead)]
public record GetScorecardsByApplication(Guid ApplicationId) : IRequest<Result<List<ScorecardDto>>>;

public class GetScorecardsByApplicationHandler
    : IRequestHandler<GetScorecardsByApplication, Result<List<ScorecardDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetScorecardsByApplicationHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<List<ScorecardDto>>> Handle(
        GetScorecardsByApplication request, CancellationToken ct)
    {
        // Check if all scorecards are submitted (blind review enforcement)
        var allSubmitted = await _context.Interviews
            .Where(i => i.ApplicationId == request.ApplicationId
                     && i.Status == InterviewStatus.Completed)
            .AllAsync(i => i.Scorecard != null && i.Scorecard.SubmittedAt.HasValue, ct);

        // Interviewers can only see aggregate after all submitted
        if (!allSubmitted && !_user.Roles!.Contains(Roles.HrAdmin) && !_user.Roles.Contains(Roles.HiringManager))
        {
            // Return only the current user's scorecard
            var ownScorecard = await _context.Scorecards
                .AsNoTracking()
                .Where(s => s.InterviewerId == _user.Id
                         && s.Interview.ApplicationId == request.ApplicationId)
                .ToListAsync(ct);

            return Result.Success(ownScorecard.Select(MapToDto).ToList());
        }

        var scorecards = await _context.Scorecards
            .AsNoTracking()
            .Where(s => s.Interview.ApplicationId == request.ApplicationId)
            .ToListAsync(ct);

        return Result.Success(scorecards.Select(MapToDto).ToList());
    }

    private static ScorecardDto MapToDto(Domain.Entities.Scorecard scorecard)
    {
        var ratings = new Dictionary<string, int>();
        if (scorecard.Ratings.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in scorecard.Ratings.RootElement.EnumerateObject())
            {
                if (prop.Value.TryGetInt32(out var val))
                    ratings[prop.Name] = val;
            }
        }

        return new ScorecardDto(
            scorecard.Id,
            scorecard.InterviewId,
            scorecard.InterviewerId,
            ratings,
            scorecard.Recommendation,
            scorecard.Strengths,
            scorecard.Concerns,
            scorecard.Notes,
            scorecard.SubmittedAt,
            scorecard.SubmittedAt.HasValue);
    }
}
```

### Step 4: Create endpoints

- [ ] **Step 4: Create `src/Web/Endpoints/Scorecards.cs`**

```csharp
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Scorecards.Commands;
using RC.HyRe.Application.Scorecards.Queries;

namespace RC.HyRe.Web.Endpoints;

public class Scorecards : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetByInterview, "interview/{interviewId}");
        groupBuilder.MapGet(GetMy, "my");
        groupBuilder.MapGet(GetByApplication, "application/{applicationId}");
        groupBuilder.MapPost(Submit, "{id}/submit");
    }

    public static async Task<IResult> GetByInterview(ISender sender, Guid interviewId, CancellationToken ct)
    {
        var result = await sender.Send(new GetScorecardByInterview(interviewId), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.NotFound(ApiResponse.Fail("SCORECARD_NOT_FOUND", "Scorecard not found.", result.Errors));
    }

    public static async Task<IResult> GetMy(
        ISender sender, int page = 1, int limit = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, 100);

        var result = await sender.Send(new GetScorecardsByInterviewer(page, limit), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_SCORECARDS_FAILED", "Failed to retrieve scorecards.", result.Errors));
    }

    public static async Task<IResult> GetByApplication(ISender sender, Guid applicationId, CancellationToken ct)
    {
        var result = await sender.Send(new GetScorecardsByApplication(applicationId), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_SCORECARDS_FAILED", "Failed to retrieve scorecards.", result.Errors));
    }

    public static async Task<IResult> Submit(ISender sender, Guid id, SubmitScorecardBody body, CancellationToken ct)
    {
        var command = new SubmitScorecard(id, body.Ratings, body.Recommendation, body.Strengths, body.Concerns, body.Notes);
        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("SUBMIT_FAILED", "Failed to submit scorecard.", result.Errors));
    }
}

public record SubmitScorecardBody(
    Dictionary<string, int> Ratings,
    string Recommendation,
    string Strengths,
    string Concerns,
    string? Notes
);
```

### Step 5: Commit

- [ ] **Step 5: Commit**

```bash
git add src/Application/Scorecards/ src/Web/Endpoints/Scorecards.cs
git commit -m "feat(scorecards): add submit command, queries, and endpoints"
```

---

## Task 7: Final wiring — PaginatedList.Create + build verification

**Files:**

- Modify: `src/Application/Common/Models/PaginatedList.cs`

### Step 1: Add PaginatedList.Create factory method

- [ ] **Step 1: Add static `Create` method to `PaginatedList<T>`**

Add this method inside the `PaginatedList<T>` class, after the `CreateAsync` method:

```csharp
/// <summary>
/// Creates a PaginatedList from an already-materialised in-memory list.
/// Use when the data has been projected or fetched in a separate query.
/// </summary>
public static PaginatedList<T> Create(IReadOnlyList<T> items, int totalCount, int page, int limit)
{
    return new PaginatedList<T>(items, totalCount, page, limit);
}
```

### Step 2: Verify build

- [ ] **Step 2: Build the solution**

```bash
dotnet build RC.HyRe.slnx
```

Expected: Build succeeds with 0 errors.

### Step 3: Commit

- [ ] **Step 3: Commit**

```bash
git add src/Application/Common/Models/PaginatedList.cs
git commit -m "feat: add PaginatedList.Create factory for in-memory projection"
```

---

*Plan written: 2026-05-22*
