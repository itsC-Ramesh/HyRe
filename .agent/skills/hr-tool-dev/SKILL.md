---
name: unified-hiring-tool-dev
description: >
  Project-specific skill for a developer building the unified hiring and onboarding tool.
  Use this skill whenever writing code, adding a command or query, designing API endpoints,
  creating EF Core migrations, building Angular components, wiring Hangfire workers, writing
  tests, or reviewing any code in this project. This skill reflects the ACTUAL codebase —
  Clean Architecture with CQRS/MediatR — not a hypothetical structure. Always consult before
  writing any new feature, handler, endpoint, migration, or Angular module, even for small tasks.
---

# Unified hiring tool — developer skill

This is your project bible. It reflects what is actually built. When the codebase and this
document conflict, check the codebase first — then update this file.

---

## Quick reference

| Thing you need | Go to |
|---|---|
| Which project a file belongs in | [Solution structure](#solution-structure) |
| How to add a new feature | [CQRS feature pattern](#cqrs-feature-pattern) |
| EF Core entities and relationships | [Data model](#data-model) |
| Event names and how to emit them | [Event catalogue](#event-catalogue) |
| Which role can do what | [RBAC](#rbac) |
| How layers communicate | [Layer rules](#layer-rules) |
| Naming conventions | [Conventions](#conventions) |
| Full tech stack | [Stack](#stack) |
| Build order and what's missing | [Phase map](#phase-map) |
| API design rules | [API design](#api-design) |

---

## Stack

Fixed. No alternatives without team discussion.

**Backend**
- Runtime: .NET 9
- Framework: ASP.NET Core 9 — minimal APIs via `IEndpointGroup` (see `Web/Infrastructure/`)
- Architecture: Clean Architecture (Domain → Application → Infrastructure → Web)
- CQRS: MediatR 12 — every feature is a Command or Query + Handler
- ORM: Entity Framework Core 9, Npgsql provider, code-first
- Database: PostgreSQL 16+
- Background jobs: Hangfire (PostgreSQL storage — already in the solution, wire in Phase 2)
- File storage: S3-compatible (AWS S3 or MinIO for local dev) via AWSSDK.S3
- Email: MailKit (SMTP; Mailhog locally; SendGrid relay in production)
- Auth: ASP.NET Core Identity (`ApplicationUser`) + JWT Bearer — `JwtTokenService` already implemented
- PDF generation: QuestPDF (no headless browser)
- Validation: FluentValidation — `ValidationBehaviour` already in the pipeline
- Mapping: Mapster (not AutoMapper)
- Pipeline behaviours already wired: `LoggingBehaviour`, `PerformanceBehaviour`, `ValidationBehaviour`, `AuthorizationBehaviour`, `UnhandledExceptionBehaviour`

**Frontend**
- Framework: Angular 21, standalone components, signals-based state
- Routing: Angular Router, lazy-loaded feature routes (one route per portal)
- State: Angular Signals — `signal()` / `computed()` / `effect()`; no NgRx
- HTTP: Angular `HttpClient` with typed DTOs
- UI: Angular Material 17+ (MDC-based)
- Forms: Angular Reactive Forms + custom validators
- Rich text: NgxEditor (template editor only)

**Infrastructure (local dev)**
- Docker Compose: postgres:16, minio, mailhog
- Secrets: `appsettings.Development.json` (git-ignored); `appsettings.Example.json` maintained
- AppHost: .NET Aspire `AppHost` project orchestrates the solution

---

## Solution structure

```
src/
├── AppHost/                          # .NET Aspire orchestration
├── ServiceDefaults/                  # Shared Aspire extensions
├── Shared/                           # Cross-cutting service registrations
│
├── Domain/                           # ← innermost ring, no dependencies
│   ├── Common/                       # BaseEntity, BaseAuditableEntity, BaseEvent, ValueObject
│   ├── Constants/                    # Permissions.cs, Roles.cs
│   ├── Entities/                     # All EF Core entities (see Data model)
│   ├── Enums/                        # All enums
│   └── Events/                       # Domain events (extend BaseEvent)
│
├── Application/                      # ← business logic, depends only on Domain
│   ├── Common/
│   │   ├── Behaviours/               # MediatR pipeline behaviours (all 5 already exist)
│   │   ├── Exceptions/               # ForbiddenAccessException, ValidationException
│   │   ├── Interfaces/               # IApplicationDbContext, IJwtTokenService, IUser, etc.
│   │   ├── Models/                   # Result<T>, PaginatedList<T>, LookupDto, AuthResult
│   │   └── Security/                 # AuthorizeAttribute (custom, not ASP.NET)
│   └── {Feature}/                    # One folder per domain feature (see CQRS pattern)
│       ├── Commands/
│       └── Queries/
│
├── Infrastructure/                   # ← implements Application interfaces
│   ├── Data/
│   │   ├── ApplicationDbContext.cs   # Implements IApplicationDbContext
│   │   ├── Configurations/           # IEntityTypeConfiguration per entity
│   │   ├── Interceptors/             # AuditableEntityInterceptor, DispatchDomainEventsInterceptor
│   │   ├── Migrations/
│   │   └── Repositories/            # Implement interfaces defined in Application
│   └── Identity/                    # ApplicationUser, IdentityService, JwtTokenService, AuditService
│
└── Web/                             # ← outermost ring, depends on all layers
    ├── Endpoints/                   # IEndpointGroup implementations, one file per feature group
    ├── Infrastructure/              # Middleware, OpenAPI transformers, WebApplicationExtensions
    └── ClientApp/                   # Angular 21 SPA (ng serve or built into wwwroot)
```

### What exists vs what needs to be built

**Already implemented:**
- All 10 domain entities and their EF configurations
- All enums and domain events (4 events)
- Auth feature: Register, Login, Logout, RefreshToken, AssignRole commands; GetCurrentUser query
- CQRS pipeline with all 5 behaviours
- `IApplicationDbContext`, `IIdentityService`, `IJwtTokenService`, `IUser`, `IAuditService`
- `Result<T>`, `PaginatedList<T>`, `AuthResult` models
- `ApplicationDbContext` with interceptors, initial migration
- `IdentityService`, `JwtTokenService`, `AuditService`
- Partial repositories: `CandidateRepository`, `ApplicationRepository`, `RequisitionRepository`
- Auth endpoint group (`Web/Endpoints/Auth.cs`)

**Not yet built (follow Phase map):**
- Commands/Queries for: Candidates, Requisitions, Applications, Interviews, Scorecards, Offers, Onboarding, Analytics
- Hangfire setup and event dispatch wiring
- File store service and template engine
- Notification workers
- Offer PDF generation
- Onboarding checklist module
- All Angular portal screens (ClientApp is a stub)

---

## CQRS feature pattern

Every feature in the `Application` layer follows this exact structure. There are no service classes — business logic lives in handlers.

### Folder layout

```
Application/
└── Candidates/
    ├── Commands/
    │   ├── CreateCandidate/
    │   │   ├── CreateCandidateCommand.cs
    │   │   └── CreateCandidateCommandHandler.cs
    │   └── UpdateCandidateStage/
    │       ├── UpdateCandidateStageCommand.cs
    │       └── UpdateCandidateStageCommandHandler.cs
    └── Queries/
        ├── GetCandidateById/
        │   ├── GetCandidateByIdQuery.cs
        │   ├── GetCandidateByIdQueryHandler.cs
        │   └── CandidateDto.cs          ← response DTO lives next to its query
        └── GetCandidatesPaginated/
            ├── GetCandidatesPaginatedQuery.cs
            ├── GetCandidatesPaginatedQueryHandler.cs
            └── CandidateSummaryDto.cs
```

### Command template

```csharp
// CreateCandidateCommand.cs
public record CreateCandidateCommand(
    string Name,
    string Email,
    string? Phone,
    SourceType Source,
    string? SourceDetail
) : IRequest<Result<Guid>>;

public class CreateCandidateCommandValidator : AbstractValidator<CreateCandidateCommand>
{
    public CreateCandidateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Source).IsInEnum();
    }
}

// CreateCandidateCommandHandler.cs
public class CreateCandidateCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateCandidateCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCandidateCommand request, CancellationToken ct)
    {
        var existing = await db.Candidates
            .AnyAsync(c => c.Email == request.Email, ct);

        if (existing)
            return Result.Failure<Guid>("A candidate with this email already exists.");

        var candidate = new Candidate
        {
            Name         = request.Name,
            Email        = request.Email,
            Phone        = request.Phone,
            Source       = request.Source,
            SourceDetail = request.SourceDetail,
        };

        candidate.AddDomainEvent(new CandidateCreatedEvent(candidate));
        db.Candidates.Add(candidate);
        await db.SaveChangesAsync(ct);

        return Result.Success(candidate.Id);
    }
}
```

### Query template

```csharp
// GetCandidateByIdQuery.cs
public record GetCandidateByIdQuery(Guid CandidateId) : IRequest<Result<CandidateDto>>;

// CandidateDto.cs — lives next to its query
public record CandidateDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    SourceType Source,
    string? SourceDetail,
    DateTime CreatedAt
);

// GetCandidateByIdQueryHandler.cs
public class GetCandidateByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCandidateByIdQuery, Result<CandidateDto>>
{
    public async Task<Result<CandidateDto>> Handle(GetCandidateByIdQuery request, CancellationToken ct)
    {
        var candidate = await db.Candidates
            .AsNoTracking()
            .Where(c => c.Id == request.CandidateId)
            .Select(c => new CandidateDto(c.Id, c.Name, c.Email, c.Phone, c.Source, c.SourceDetail, c.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return candidate is null
            ? Result.Failure<CandidateDto>("Candidate not found.")
            : Result.Success(candidate);
    }
}
```

### Endpoint group template

```csharp
// Web/Endpoints/Candidates.cs
public class CandidatesEndpoints : IEndpointGroup
{
    public void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/candidates")
            .RequireAuthorization()
            .WithTags("Candidates");

        group.MapGet("{id:guid}", GetById);
        group.MapPost("", Create);
    }

    private static async Task<IResult> GetById(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetCandidateByIdQuery(id), ct);
        return result.Succeeded
            ? Results.Ok(ApiResponse.Ok(result.Value))
            : Results.NotFound(ApiResponse.Fail("CANDIDATE_NOT_FOUND", result.Error));
    }

    private static async Task<IResult> Create(
        CreateCandidateCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? Results.Created($"/api/v1/candidates/{result.Value}", ApiResponse.Ok(result.Value))
            : Results.BadRequest(ApiResponse.Fail("CREATE_FAILED", result.Error));
    }
}
```

Register new endpoint groups in `Web/Infrastructure/WebApplicationExtensions.cs`.

### RBAC on commands

Use the custom `[Authorize]` attribute from `Application/Common/Security/` — **not** the ASP.NET one:

```csharp
[Authorize(Roles = Roles.HrAdmin + "," + Roles.HiringManager)]
public record CreateRequisitionCommand(...) : IRequest<Result<Guid>>;
```

`AuthorizationBehaviour` in the pipeline enforces this before the handler runs. For row-level checks (e.g. a hiring manager may only update their own requisition), inject `IUser` into the handler and verify `currentUser.Id`.

---

## Data model

All entities already exist in `Domain/Entities/` and are configured in `Infrastructure/Data/Configurations/`. Use these as the reference — do not add new properties without a migration.

### Entities and their key fields

**Candidate** — `CandidateConfiguration.cs`
```
Id (Guid, PK), Name, Email (unique index), Phone?,
Source (SourceType enum → stored as string), SourceDetail?,
ResumeDocumentId? (FK → Document),
CreatedAt, UpdatedAt (set by AuditableEntityInterceptor)
Nav: Applications[], ResumeDocument?
```

**Requisition** — `RequisitionConfiguration.cs`
```
Id (Guid, PK), Title, Department,
OwnerId (FK → ApplicationUser, hiring manager),
JdText, SalaryMin?, SalaryMax?, Headcount (default 1),
Status (RequisitionStatus enum → string), CreatedAt, UpdatedAt
Nav: Applications[], Owner
```

**JobApplication** (maps to `job_applications` table) — `ApplicationConfiguration.cs`
```
Id (Guid, PK), CandidateId (FK), RequisitionId (FK),
Stage (ApplicationStage enum → string), RejectionReason?,
CreatedAt, UpdatedAt
Nav: Candidate, Requisition, Interviews[], Offer?
```

> Note: the class is `JobApplication` in the domain; use this name in all C# code. The EF table name is `job_applications`.

**Interview** — `InterviewConfiguration.cs`
```
Id (Guid, PK), ApplicationId (FK → JobApplication),
InterviewerId (FK → ApplicationUser),
Type (InterviewType → string), ScheduledAt,
DurationMinutes (default 60), Status (InterviewStatus → string),
MeetingLink?, CreatedAt
Nav: Application, Interviewer, Scorecard?
```

**Scorecard** — `ScorecardConfiguration.cs`
```
Id (Guid, PK), InterviewId (FK, unique — one per interview),
InterviewerId (FK → ApplicationUser),
RatingTechnical (1–5), RatingCommunication (1–5),
RatingProblemSolving (1–5), RatingCultureFit (1–5),
Recommendation (ScorecardRecommendation → string),
Strengths, Concerns, Notes?,
SubmittedAt?, CreatedAt
Nav: Interview, Interviewer
```

**Offer** — `OfferConfiguration.cs`
```
Id (Guid, PK), ApplicationId (FK, unique — one per application),
Salary (int, paise), Currency (default "INR"),
StartDate (DateOnly), ContractType (→ string), ExpiryDate (DateOnly),
Status (OfferStatus → string), LetterDocumentId? (FK → Document),
SignedAt?, CreatedAt, UpdatedAt
Nav: Application, LetterDocument?
```

**Document** — `DocumentConfiguration.cs`
```
Id (Guid, PK), EntityType (string), EntityId (Guid),
FileKey (S3 key), DocumentType (DocumentType enum → string),
MimeType, SizeBytes, CreatedAt
```

**Notification** — `NotificationConfiguration.cs`
```
Id (Guid, PK), RecipientId (FK → ApplicationUser),
Type (string), Payload (JSON string), ReadAt?, CreatedAt
```

**AuditLogEntry** — `AuditLogEntryConfiguration.cs`
```
Id (Guid, PK), EntityType, EntityId (Guid),
Action (string — use EventNames constants), ActorId?,
Metadata (JSON string — never update, never delete rows), CreatedAt
```

**RefreshToken** — `RefreshTokenConfiguration.cs`
```
Id (Guid, PK), UserId (FK → ApplicationUser),
Token (string, unique index), ExpiresAt, RevokedAt?, CreatedAt
```

### Enum storage rule

All enums are stored as strings in PostgreSQL. This is already configured in each `IEntityTypeConfiguration` via `.HasConversion<string>()`. Never remove this — integer enum storage makes the DB unreadable and painful to debug.

### Adding a new property

1. Add property to the domain entity class
2. Add column config in the relevant `IEntityTypeConfiguration`
3. Run `dotnet ef migrations add DescribeTheChange --project Infrastructure --startup-project Web`
4. Never edit a migration after it has run in any shared environment

---

## Event catalogue

Domain events live in `Domain/Events/` and extend `BaseEvent`. These trigger side effects via `DispatchDomainEventsInterceptor`, which fires MediatR notifications after `SaveChangesAsync`.

### Existing domain events
```csharp
// Already in Domain/Events/
CandidateCreatedEvent
ApplicationStageChangedEvent
ScorecardSubmittedEvent
OfferAcceptedEvent
```

### Naming pattern for new events
```csharp
// Domain/Events/InterviewBookedEvent.cs
public class InterviewBookedEvent(Interview interview) : BaseEvent
{
    public Interview Interview { get; } = interview;
}
```

### How to emit a domain event (in a command handler)
```csharp
// Add the event to the entity before SaveChangesAsync
application.AddDomainEvent(new ApplicationStageChangedEvent(application, previousStage));
await db.SaveChangesAsync(ct);
// DispatchDomainEventsInterceptor fires the event automatically after save
```

### How to handle a domain event (notification handler)
```csharp
// Application/Applications/EventHandlers/ApplicationStageChangedEventHandler.cs
public class ApplicationStageChangedEventHandler(INotificationService notifications)
    : INotificationHandler<ApplicationStageChangedEvent>
{
    public async Task Handle(ApplicationStageChangedEvent notification, CancellationToken ct)
    {
        // Send email, create in-app notification, enqueue Hangfire job
    }
}
```

### Hangfire job names (use as constants — add to `Application/Common/`)
```csharp
public static class JobNames
{
    // Interview reminders
    public const string InterviewReminder24H    = "interview.reminder_24h";
    public const string InterviewReminder1H     = "interview.reminder_1h";

    // Scorecard overdue checks
    public const string ScorecardOverdueCheck   = "scorecard.overdue_check";

    // Offer expiry checks
    public const string OfferExpiryCheck        = "offer.expiry_check";

    // Approval escalation
    public const string ApprovalEscalation      = "approval.escalation";

    // Onboarding task reminders
    public const string OnboardingTaskReminder  = "onboarding.task_reminder";
}
```

Scheduled (recurring) jobs are registered in `Infrastructure/DependencyInjection.cs` using `RecurringJob.AddOrUpdate(...)`. Delayed jobs (e.g. interview reminder 24h out) are enqueued from event handlers using `BackgroundJob.Schedule(...)`.

---

## Layer rules

These are the dependency rules of Clean Architecture. Violations here cause subtle, hard-to-find bugs.

```
Domain      ← no dependencies (innermost)
Application ← depends only on Domain
Infrastructure ← depends on Application + Domain
Web         ← depends on all layers (outermost)
```

**Allowed:**
- Application layer references `Domain` entities and `IApplicationDbContext`
- Infrastructure implements `IApplicationDbContext`, `IIdentityService`, `IJwtTokenService`
- Web sends MediatR commands/queries via `ISender`; never calls services or repos directly

**Not allowed:**
- Domain referencing anything outside itself
- Application referencing `Infrastructure` types directly
- Web calling `ApplicationDbContext` or any repo directly — always go through MediatR
- Two feature folders importing each other's handlers or DTOs

**Cross-feature data:** If handler A needs data owned by feature B, query via `IApplicationDbContext` (shared DbContext interface) — do not inject B's handler or repository.

**File store and template engine** (to be built in Phase 2): Define interfaces in `Application/Common/Interfaces/` (`IFileStore`, `ITemplateEngine`). Implement in `Infrastructure/`. Inject in handlers via the interface.

---

## RBAC

### Roles (already in `Domain/Constants/Roles.cs`)
```csharp
public static class Roles
{
    public const string HrAdmin        = "HrAdmin";
    public const string HiringManager  = "HiringManager";
    public const string Interviewer    = "Interviewer";
    public const string Executive      = "Executive";
    public const string Candidate      = "Candidate";
}
```

### Permissions (already in `Domain/Constants/Permissions.cs`)
Extend this file as new features are added. Pattern: `"Feature.Action"`.

```csharp
public static class Permissions
{
    public static class Candidates
    {
        public const string View   = "Candidates.View";
        public const string Create = "Candidates.Create";
        public const string Edit   = "Candidates.Edit";
        public const string Delete = "Candidates.Delete";
    }
    public static class Requisitions
    {
        public const string View    = "Requisitions.View";
        public const string Create  = "Requisitions.Create";
        public const string Approve = "Requisitions.Approve";
    }
    // ... add new sections following same pattern
}
```

### Applying permissions to a command
```csharp
// Use the Application-layer [Authorize] attribute (not the ASP.NET one)
[Authorize(Roles = Roles.HrAdmin + "," + Roles.HiringManager,
           Policy = Permissions.Candidates.Create)]
public record CreateCandidateCommand(...) : IRequest<Result<Guid>>;
```

### RBAC permission table

| Action | HrAdmin | HiringManager | Interviewer | Executive | Candidate |
|---|---|---|---|---|---|
| View all candidates | ✅ | Own role only | ❌ | ❌ | Own profile |
| Create/edit candidates | ✅ | ❌ | ❌ | ❌ | ❌ |
| View all requisitions | ✅ | ❌ | ❌ | ✅ (read) | ❌ |
| Create requisitions | ✅ | ✅ | ❌ | ❌ | ❌ |
| Approve requisitions | ✅ | ❌ | ❌ | ❌ | ❌ |
| Move application stage | ✅ | Own role | ❌ | ❌ | ❌ |
| Submit scorecard | ✅ | ✅ | Assigned only | ❌ | ❌ |
| Read all scorecards | ✅ | Own role (after all in) | Own only | ❌ | ❌ |
| Create offer | ✅ | ❌ | ❌ | ❌ | ❌ |
| Approve offer | ✅ | Own role | ❌ | ❌ | ❌ |
| View analytics | ✅ | Own roles | ❌ | Aggregate | ❌ |
| Manage templates | ✅ | ❌ | ❌ | ❌ | ❌ |

### Row-level scoping in query handlers
```csharp
// Inject IUser to get the current user's Id and role
public class GetApplicationsQueryHandler(IApplicationDbContext db, IUser currentUser)
    : IRequestHandler<GetApplicationsQuery, Result<PaginatedList<ApplicationSummaryDto>>>
{
    public async Task<Result<PaginatedList<ApplicationSummaryDto>>> Handle(...)
    {
        var query = db.JobApplications.AsNoTracking();

        // Scope down for non-admin roles — always at the DB query level, never in-memory
        if (!currentUser.Roles.Contains(Roles.HrAdmin))
            query = query.Where(a => a.Requisition.OwnerId == Guid.Parse(currentUser.Id!));

        // ...paginate and project
    }
}
```

---

## API design

### Endpoint registration

All endpoints implement `IEndpointGroup` from `Web/Infrastructure/IEndpointGroup.cs` and are discovered automatically via `WebApplicationExtensions`. One file per feature group in `Web/Endpoints/`.

```csharp
// Web/Endpoints/Candidates.cs
public class CandidatesEndpoints : IEndpointGroup
{
    public void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/candidates")
            .RequireAuthorization()
            .WithTags("Candidates")
            .WithOpenApi();

        group.MapGet("{id:guid}", GetById).WithName("GetCandidateById");
        group.MapGet("", GetPaginated).WithName("GetCandidates");
        group.MapPost("", Create).WithName("CreateCandidate");
        group.MapPatch("{id:guid}", Update).WithName("UpdateCandidate");
        group.MapPost("{id:guid}/stage", ChangeStage).WithName("ChangeCandidateStage");
    }
    // ...handler methods
}
```

### REST conventions

- Base path: `/api/v1/`
- All ID path params: `{id:guid}` route constraint
- Pagination: `?page=1&pageSize=20` (default 20, max 100)
- Sorting: `?sortBy=createdAt&sortDir=desc`
- Timestamps: ISO 8601 UTC — `2026-05-16T10:30:00.000Z`
- Money: integers in paise (smallest INR unit)
- Dates without time: `DateOnly` serialised as `"2026-06-01"`

### Standard response envelope

Use `ApiResponse` (add to `Web/Infrastructure/` if not present):

```csharp
// Success — single
{ "success": true, "data": { ... } }

// Success — list
{ "success": true, "data": [...], "meta": { "page": 1, "pageSize": 20, "total": 143 } }

// Error
{ "success": false, "error": { "code": "CANDIDATE_NOT_FOUND", "message": "...", "details": {} } }
```

`ProblemDetailsExceptionHandler` (already exists in `Web/Infrastructure/`) catches unhandled exceptions. Domain validation failures come through `ValidationBehaviour` → `ValidationException` → problem details automatically.

### Route naming

```
GET    /api/v1/candidates                    paginated list
POST   /api/v1/candidates                    create
GET    /api/v1/candidates/{id}               get one
PATCH  /api/v1/candidates/{id}               partial update
DELETE /api/v1/candidates/{id}               delete

POST   /api/v1/applications/{id}/stage       stage transition
POST   /api/v1/interviews/{id}/scorecard     submit scorecard
POST   /api/v1/offers/{id}/approve           approve offer
POST   /api/v1/offers/{id}/sign              candidate signs
POST   /api/v1/offers/{id}/cancel            cancel
```

Verbs only on explicit lifecycle actions (`/stage`, `/approve`, `/sign`, `/cancel`). Everything else is a noun resource.

---

## Conventions

### C# naming

| Thing | Convention | Example |
|---|---|---|
| Classes, records, interfaces | PascalCase | `CreateCandidateCommand`, `IFileStore` |
| Methods | PascalCase | `Handle`, `MapEndpoints` |
| Private fields | `_camelCase` | `_db`, `_currentUser` |
| Local variables / params | camelCase | `candidateId`, `newStage` |
| Constants | PascalCase | `Roles.HrAdmin` |
| Enum members | PascalCase | `ApplicationStage.Applied` |
| DB table names | snake_case (EF config) | `job_applications`, `audit_log_entries` |
| DB column names | snake_case (Npgsql convention) | `created_at`, `owner_id` |
| API JSON fields | camelCase (`JsonSerializerOptions`) | `createdAt`, `requisitionId` |
| Event/job name strings | `entity.action` | `application.stage_changed` |
| Config keys | PascalCase, colon-separated | `Jwt:Secret`, `S3:BucketName` |

### File naming

| File | Convention | Example |
|---|---|---|
| Command / query | `{Verb}{Entity}Command.cs` | `CreateCandidateCommand.cs` |
| Handler | `{Command/Query}Handler.cs` | `CreateCandidateCommandHandler.cs` |
| DTO | `{Entity}Dto.cs` | `CandidateDto.cs` — lives next to its query |
| Domain event | `{Entity}{Action}Event.cs` | `ApplicationStageChangedEvent.cs` |
| Event handler | `{Event}Handler.cs` | `ApplicationStageChangedEventHandler.cs` |
| Endpoint group | `{Feature}s.cs` | `Candidates.cs` |
| EF configuration | `{Entity}Configuration.cs` | `CandidateConfiguration.cs` |
| Repository | `{Entity}Repository.cs` | `CandidateRepository.cs` |

### Angular naming

| Thing | Convention | Example |
|---|---|---|
| Component class | PascalCase | `CandidatePipelineComponent` |
| Component selector | `app-kebab-case` | `app-candidate-pipeline` |
| Service | PascalCase | `CandidatesService` |
| Signal | camelCase | `candidates = signal<CandidateDto[]>([])` |
| Files | kebab-case | `candidate-pipeline.component.ts` |
| Route paths | kebab-case | `/hr/pipeline`, `/candidate/status` |
| DTOs/models | PascalCase + `Dto` suffix | `CandidateDto`, `CreateCandidateRequest` |

### EF Core rules

- Migrations: `dotnet ef migrations add PascalCaseDescription --project Infrastructure --startup-project Web`
- Never edit a migration that has run in any shared environment — create a new one
- `AsNoTracking()` on every read-only query (queries, not commands)
- All list queries use `.Skip().Take()` — no unbounded results
- All enums use `.HasConversion<string>()` in configuration
- All `DateTime` values are UTC — always `DateTime.UtcNow`, never `DateTime.Now`
- `AuditLogEntry` rows are append-only — no updates, no deletes

### Error handling

Use the existing typed exceptions in `Application/Common/Exceptions/`:
```csharp
// Domain/validation failures — caught by ValidationBehaviour
throw new ValidationException(failures);

// Permission failures — caught by AuthorizationBehaviour
throw new ForbiddenAccessException();
```

For not-found and business rule failures, return `Result.Failure<T>("message")` from the handler — do not throw. The endpoint maps `result.Succeeded` to the appropriate HTTP status.

---

## Phase map

What to build next, in order. Never start a phase before its dependencies are done.

```
Foundation — DONE ✓
  ✓ Domain entities, enums, domain events
  ✓ CQRS pipeline (all 5 behaviours)
  ✓ Auth commands/queries (register, login, refresh, logout, assign role)
  ✓ EF Core DbContext + interceptors + initial migration
  ✓ ApplicationUser, IdentityService, JwtTokenService
  ✓ Partial repos: Candidate, Application, Requisition

Phase 1 — Core hiring loop (next priority)
  ├── Candidates: CRUD commands + queries + endpoint group
  ├── Requisitions: CRUD commands + approval command + endpoint group
  ├── Applications (JobApplication): create, stage-change command + pipeline query
  ├── Interviews: book, cancel, reschedule commands + availability query
  └── Scorecards: submit command + aggregate query

Phase 2 — Automation & stakeholder access
  ├── Requires: Phase 1 complete
  ├── IFileStore interface (Application) + S3 implementation (Infrastructure)
  ├── ITemplateEngine interface + DB-backed implementation
  ├── Hangfire setup in Infrastructure/DependencyInjection.cs
  ├── Domain event handlers → email + in-app notifications
  ├── Offer module: create, approve, send, sign commands + QuestPDF generation
  ├── Onboarding module: checklist creation on OfferAcceptedEvent
  └── Angular: HR portal + candidate status portal (lazy routes)

Phase 3 — Intelligence & integrations
  ├── Requires: Phase 2 complete + AuditLogEntry data accumulating
  ├── Analytics queries (read AuditLogEntry only, not operational tables)
  ├── Source tracking attribution
  ├── Global search (PostgreSQL full-text via EF Core)
  └── External integrations: Google Calendar sync, Slack webhooks, HRIS export
```

**Hard dependency rules:**
1. `IFileStore` must exist before the offer module (letter PDF stored via file store)
2. `ITemplateEngine` must have seed templates before offer or comms modules
3. Hangfire must be configured before any delayed/scheduled notification jobs
4. Offer module must be complete before onboarding (`OfferAcceptedEvent` triggers checklist)
5. Analytics reads from `AuditLogEntry` — needs Phase 1 + 2 data first

---

## Local development setup

```bash
# 1. Start infrastructure
docker-compose up -d
# postgres:16 on 5432, minio on 9000/9001, mailhog on 1025/8025

# 2. Apply migrations
cd src/Infrastructure
dotnet ef database update --startup-project ../Web

# 3. Run via Aspire (recommended — orchestrates all projects)
cd src/AppHost
dotnet run

# OR run Web project directly
cd src/Web
dotnet run

# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
# Hangfire dashboard: http://localhost:5000/hangfire  (Phase 2+)

# 4. Angular dev server (separate terminal)
cd src/Web/ClientApp
npm install
ng serve
# App: http://localhost:4200

# 5. Test emails:  http://localhost:8025  (Mailhog)
# 6. Test files:   http://localhost:9001  (MinIO console — admin/minioadmin)
```

### `appsettings.Development.json` (copy from `appsettings.Example.json`, git-ignored)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=hiring_tool;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "change-me-minimum-32-characters-long",
    "RefreshSecret": "also-change-me-minimum-32-chars",
    "Issuer": "HiringTool",
    "Audience": "HiringTool",
    "CandidateAudience": "HiringTool.Candidate",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "S3": {
    "Endpoint": "http://localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "hiring-tool",
    "ForcePathStyle": true
  },
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "FromAddress": "noreply@hiringtool.local",
    "FromName": "Hiring Tool"
  },
  "CandidatePortalUrl": "http://localhost:4200/portal"
}
```

---

## Common mistakes to avoid

1. **Putting business logic in endpoint handlers.** The endpoint only sends a MediatR command/query and maps the result to HTTP. All logic lives in the handler.
2. **Injecting `ApplicationDbContext` directly into an endpoint or handler.** Always use `IApplicationDbContext` (the interface) — it enables testing and respects layer boundaries.
3. **Calling `Infrastructure` types from `Application`.** If you find yourself referencing a concrete Infrastructure class from Application, you need an interface.
4. **Using `DateTime.Now` instead of `DateTime.UtcNow`.** All timestamps must be UTC throughout.
5. **Forgetting `AsNoTracking()` on query handlers.** Queries are read-only — tracking wastes memory and slows queries.
6. **Returning unbounded lists.** Every list query must use `PaginatedList<T>` with `.Skip().Take()`.
7. **Querying operational tables from analytics handlers.** Analytics reads only from `AuditLogEntry` — never from `JobApplications`, `Candidates`, etc.
8. **Throwing exceptions for business failures in handlers.** Use `Result.Failure<T>("message")`. Only throw for truly exceptional conditions (`ForbiddenAccessException`, `ValidationException`).
9. **Putting DTO classes in the wrong place.** Response DTOs live next to their query. Request DTOs (commands) are the command record itself. Never put DTOs in a shared `Dtos/` folder.
10. **Updating or deleting `AuditLogEntry` rows.** This table is append-only by design.
11. **Filtering in-memory after fetching all rows.** Always scope `Where()` clauses at the EF query level in the handler.
12. **Storing enums as integers.** Every enum property must have `.HasConversion<string>()` in its `IEntityTypeConfiguration`.
13. **Using `any` or untyped responses in Angular.** Every HTTP call is typed. Models mirror backend DTO shapes exactly.
14. **Skipping a migration after adding a property.** Adding a property to a domain entity without a migration will throw at runtime. Always run `dotnet ef migrations add` immediately.

---

*Version: 2.0 — May 2026*
*Project: Unified hiring tool*
*Stack: .NET 9 / ASP.NET Core / EF Core 9 / PostgreSQL 16 / Angular 21*
*Architecture: Clean Architecture + CQRS/MediatR*