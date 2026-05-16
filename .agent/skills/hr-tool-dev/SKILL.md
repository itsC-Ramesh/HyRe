---
name: unified-hiring-tool-dev
description: >
  Project-specific skill for a developer building the unified hiring and onboarding tool.
  Use this skill whenever writing code, designing APIs, making architecture decisions, 
  creating database migrations, building UI components, writing tests, or reviewing any
  code related to this project. This skill contains the canonical data model, event names,
  module boundaries, RBAC rules, naming conventions, and integration contracts that must
  be followed consistently across the entire codebase. Always consult this skill before
  writing any new module, endpoint, migration, or service — even if the task seems simple.
---

# Unified hiring tool — developer skill

This is your project bible. Every decision in this file is final unless explicitly revised.
Read the relevant section before writing any code. When in doubt, refer here first.

---

## Quick reference

| Thing you need | Go to |
|---|---|
| Database table names and fields | [Data model](#data-model) |
| Event names for the notification engine | [Event catalogue](#event-catalogue) |
| Which role can do what | [RBAC matrix](#rbac-matrix) |
| How modules talk to each other | [Integration contracts](#integration-contracts) |
| Naming conventions | [Conventions](#conventions) |
| Tech stack choices | [Stack](#stack) |
| Build order and dependencies | [Phase map](#phase-map) |
| API design rules | [API design](#api-design) |

---

## Stack

These are fixed. Do not introduce alternatives without team discussion.

**Backend**
- Runtime: .NET 9
- Framework: ASP.NET Core (Clean Architecture / Minimal APIs)
- ORM: EF Core (Entity Framework Core)
- Database: PostgreSQL (Npgsql)
- Messaging / MediatR: MediatR for in-process domain events and CQRS
- File storage: S3-compatible (AWS S3 or MinIO for local dev)
- Auth: ASP.NET Identity + JWT (Bearer tokens)
- Validation: FluentValidation in Application layer

**Frontend**
- Framework: Angular 21+ (Standalone components, Signals)
- Styling: Pico.css (v2) for lightweight, semantic CSS
- Icons: Lucide Angular
- State management: RxJS + Angular Signals
- API Client: Generated via NSwag / OpenAPI

**Infrastructure (local dev)**
- Docker Compose: postgres, redis, minio
- Configuration: `appsettings.json` + User Secrets / Environment variables

---

## Data model

This is the canonical schema. Prisma model names are PascalCase; table names are snake_case (set via `@@map`).

### Candidate
```prisma
model Candidate {
  id           String    @id @default(uuid())
  name         String
  email        String    @unique
  phone        String?
  source       Source
  sourceDetail String?   // referrer name, agency, UTM value
  resumeDocId  String?   // FK → Document
  createdAt    DateTime  @default(now())
  updatedAt    DateTime  @updatedAt
  applications Application[]
  document     Document? @relation(fields: [resumeDocId], references: [id])

  @@map("candidates")
}

enum Source {
  DIRECT
  LINKEDIN
  JOB_BOARD
  REFERRAL
  AGENCY
  HEADHUNTED
}
```

### Requisition
```prisma
model Requisition {
  id           String            @id @default(uuid())
  title        String
  department   String
  ownerId      String            // FK → User (hiring manager)
  jdText       String
  salaryMin    Int?
  salaryMax    Int?
  headcount    Int               @default(1)
  status       RequisitionStatus @default(DRAFT)
  createdAt    DateTime          @default(now())
  updatedAt    DateTime          @updatedAt
  applications Application[]
  owner        User              @relation(fields: [ownerId], references: [id])

  @@map("requisitions")
}

enum RequisitionStatus {
  DRAFT
  PENDING_APPROVAL
  OPEN
  ON_HOLD
  CLOSED
}
```

### Application
```prisma
model Application {
  id              String           @id @default(uuid())
  candidateId     String
  requisitionId   String
  stage           ApplicationStage @default(APPLIED)
  rejectionReason String?
  createdAt       DateTime         @default(now())
  updatedAt       DateTime         @updatedAt
  candidate       Candidate        @relation(fields: [candidateId], references: [id])
  requisition     Requisition      @relation(fields: [requisitionId], references: [id])
  interviews      Interview[]
  offer           Offer?

  @@map("applications")
}

enum ApplicationStage {
  APPLIED
  SCREENED
  INTERVIEW
  OFFER
  HIRED
  REJECTED
}
```

### Interview
```prisma
model Interview {
  id            String          @id @default(uuid())
  applicationId String
  interviewerId String
  type          InterviewType
  scheduledAt   DateTime
  durationMin   Int             @default(60)
  status        InterviewStatus @default(SCHEDULED)
  meetingLink   String?
  createdAt     DateTime        @default(now())
  application   Application     @relation(fields: [applicationId], references: [id])
  interviewer   User            @relation(fields: [interviewerId], references: [id])
  scorecard     Scorecard?

  @@map("interviews")
}

enum InterviewType {
  PHONE
  VIDEO
  TECHNICAL
  ONSITE
  CULTURE
}

enum InterviewStatus {
  SCHEDULED
  COMPLETED
  CANCELLED
  NO_SHOW
}
```

### Scorecard
```prisma
model Scorecard {
  id             String             @id @default(uuid())
  interviewId    String             @unique
  interviewerId  String
  ratings        Json               // { technical: 1-5, communication: 1-5, problemSolving: 1-5, cultureFit: 1-5 }
  recommendation Recommendation
  strengths      String
  concerns       String
  notes          String?
  submittedAt    DateTime?
  createdAt      DateTime           @default(now())
  interview      Interview          @relation(fields: [interviewId], references: [id])
  interviewer    User               @relation(fields: [interviewerId], references: [id])

  @@map("scorecards")
}

enum Recommendation {
  STRONG_YES
  YES
  NO
  STRONG_NO
}
```

### Offer
```prisma
model Offer {
  id            String      @id @default(uuid())
  applicationId String      @unique
  salary        Int
  currency      String      @default("INR")
  startDate     DateTime
  contractType  ContractType
  expiryDate    DateTime
  status        OfferStatus @default(DRAFT)
  letterDocId   String?
  signedAt      DateTime?
  createdAt     DateTime    @default(now())
  updatedAt     DateTime    @updatedAt
  application   Application @relation(fields: [applicationId], references: [id])
  document      Document?   @relation(fields: [letterDocId], references: [id])

  @@map("offers")
}

enum ContractType {
  FULL_TIME
  CONTRACT
  INTERNSHIP
}

enum OfferStatus {
  DRAFT
  PENDING_APPROVAL
  SENT
  ACCEPTED
  DECLINED
  EXPIRED
}
```

### EventLog (append-only — never update, never delete)
```prisma
model EventLog {
  id         String   @id @default(uuid())
  entityType String   // 'application' | 'offer' | 'interview' | 'scorecard' | 'requisition'
  entityId   String
  action     String   // use event catalogue names below
  actorId    String?  // null = system-triggered
  metadata   Json     @default("{}")
  createdAt  DateTime @default(now())

  @@index([entityType, entityId])
  @@index([action])
  @@map("event_log")
}
```

### User
```prisma
model User {
  id           String   @id @default(uuid())
  name         String
  email        String   @unique
  passwordHash String
  role         UserRole
  department   String?
  createdAt    DateTime @default(now())
  updatedAt    DateTime @updatedAt

  @@map("users")
}

enum UserRole {
  HR_ADMIN
  HIRING_MANAGER
  INTERVIEWER
  EXECUTIVE
  CANDIDATE
}
```

### Document
```prisma
model Document {
  id        String   @id @default(uuid())
  entityType String  // 'candidate' | 'offer' | 'onboarding'
  entityId  String
  fileKey   String   // S3 object key
  type      String   // 'resume' | 'offer_letter' | 'signed_offer' | 'onboarding_doc'
  mimeType  String
  sizeBytes Int
  createdAt DateTime @default(now())

  @@map("documents")
}
```

---

## Event catalogue

All events must use these exact string names. No ad-hoc event strings anywhere in the codebase.
Import from `@/events/event-types.ts`.

```typescript
export const Events = {
  // Application lifecycle
  APPLICATION_CREATED:         'application.created',
  APPLICATION_STAGE_CHANGED:   'application.stage_changed',
  APPLICATION_REJECTED:        'application.rejected',

  // Requisition lifecycle
  REQUISITION_CREATED:         'requisition.created',
  REQUISITION_APPROVED:        'requisition.approved',
  REQUISITION_REJECTED:        'requisition.rejected',

  // Interview lifecycle
  INTERVIEW_BOOKED:            'interview.booked',
  INTERVIEW_CANCELLED:         'interview.cancelled',
  INTERVIEW_RESCHEDULED:       'interview.rescheduled',
  INTERVIEW_COMPLETED:         'interview.completed',
  INTERVIEW_NO_SHOW:           'interview.no_show',
  INTERVIEW_REMINDER_24H:      'interview.reminder_24h',
  INTERVIEW_REMINDER_1H:       'interview.reminder_1h',

  // Scorecard lifecycle
  SCORECARD_SUBMITTED:         'scorecard.submitted',
  SCORECARD_OVERDUE:           'scorecard.overdue',
  ALL_SCORECARDS_SUBMITTED:    'scorecard.all_submitted',

  // Offer lifecycle
  OFFER_CREATED:               'offer.created',
  OFFER_APPROVAL_REQUESTED:    'offer.approval_requested',
  OFFER_APPROVED:              'offer.approved',
  OFFER_REJECTED:              'offer.rejected',
  OFFER_SENT:                  'offer.sent',
  OFFER_ACCEPTED:              'offer.accepted',
  OFFER_DECLINED:              'offer.declined',
  OFFER_EXPIRED:               'offer.expired',

  // Approval lifecycle
  APPROVAL_REQUESTED:          'approval.requested',
  APPROVAL_APPROVED:           'approval.approved',
  APPROVAL_REJECTED:           'approval.rejected',
  APPROVAL_ESCALATED:          'approval.escalated',
  APPROVAL_REMINDER:           'approval.reminder',

  // Onboarding lifecycle
  ONBOARDING_STARTED:          'onboarding.started',
  ONBOARDING_TASK_COMPLETED:   'onboarding.task_completed',
  ONBOARDING_TASK_OVERDUE:     'onboarding.task_overdue',
  ONBOARDING_COMPLETED:        'onboarding.completed',
} as const;

export type EventName = typeof Events[keyof typeof Events];
```

### How to emit an event

```typescript
// In any service file
import { eventBus } from '@/events/event-bus';
import { Events } from '@/events/event-types';
import { logEvent } from '@/events/event-logger';

// Always emit AND log together
await logEvent({
  entityType: 'application',
  entityId: application.id,
  action: Events.APPLICATION_STAGE_CHANGED,
  actorId: currentUser.id,
  metadata: { previousStage: 'SCREENED', newStage: 'INTERVIEW' },
});

await eventBus.emit(Events.APPLICATION_STAGE_CHANGED, {
  applicationId: application.id,
  candidateId: application.candidateId,
  previousStage: 'SCREENED',
  newStage: 'INTERVIEW',
});
```

Never call notification logic directly from a service. Always emit an event and let the notification worker handle it.

---

## RBAC matrix

Use the `requirePermission` middleware on every protected route.

```typescript
// Route protection pattern
fastify.get('/applications/:id', {
  preHandler: [authenticate, requirePermission('application:read')],
  handler: getApplicationHandler,
});
```

### Permission definitions

| Permission | HR_ADMIN | HIRING_MANAGER | INTERVIEWER | EXECUTIVE | CANDIDATE |
|---|---|---|---|---|---|
| `requisition:create` | ✅ | ✅ (own dept) | ❌ | ❌ | ❌ |
| `requisition:read_all` | ✅ | ❌ | ❌ | ✅ | ❌ |
| `requisition:read_own` | ✅ | ✅ | ❌ | ✅ | ❌ |
| `application:read_all` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `application:read_own_role` | ✅ | ✅ | ❌ | ❌ | ❌ |
| `application:read_self` | ✅ | ✅ | ❌ | ❌ | ✅ (own) |
| `application:stage_change` | ✅ | ✅ (own role) | ❌ | ❌ | ❌ |
| `interview:read_assigned` | ✅ | ✅ | ✅ (own) | ❌ | ✅ (own) |
| `scorecard:submit` | ✅ | ✅ | ✅ (own) | ❌ | ❌ |
| `scorecard:read_all` | ✅ | ✅ (own role, after all submitted) | ✅ (own only, before others submit) | ❌ | ❌ |
| `offer:create` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `offer:approve` | ✅ | ✅ (own role) | ❌ | ❌ | ❌ |
| `offer:read_self` | ✅ | ✅ | ❌ | ❌ | ✅ (own) |
| `analytics:read` | ✅ | ✅ (own roles) | ❌ | ✅ (aggregate) | ❌ |
| `template:manage` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `user:manage` | ✅ | ❌ | ❌ | ❌ | ❌ |

### RBAC implementation rules

1. Filtering is done at the **repository layer**, not the service or route layer.
2. Every repo method that returns a list accepts a `userId` and `userRole` parameter and applies scoping internally.
3. Never return data then filter in the service — the DB query must be scoped.
4. Candidate portal users (`CANDIDATE` role) have a completely separate JWT audience (`aud: 'candidate-portal'`). Internal user tokens cannot access candidate routes and vice versa.

```typescript
// Correct pattern — scope in repo
async function getApplications(userId: string, userRole: UserRole) {
  const where = userRole === 'HR_ADMIN' ? {} : { requisition: { ownerId: userId } };
  return prisma.application.findMany({ where });
}

// Wrong — never do this
const all = await prisma.application.findMany();
return all.filter(a => a.requisition.ownerId === userId);
```

---

## Integration contracts

Rules for how modules interact. Do not bypass these.

### Notification engine contract

The notification engine is a BullMQ worker that subscribes to all events. It is the **only** place that sends emails, in-app notifications, or webhooks. No other module may call an email function directly.

```typescript
// notification worker — in notifications/notification.worker.ts
eventBus.on(Events.APPLICATION_STAGE_CHANGED, async (payload) => {
  if (payload.newStage === 'INTERVIEW') {
    await sendEmail({
      templateId: 'interview-invitation',
      to: payload.candidateEmail,
      variables: { candidateName, roleTitle, schedulingLink },
    });
  }
});
```

### File store contract

All file operations go through `src/shared/file-store.ts`. Never call S3 SDK directly from a module.

```typescript
import { fileStore } from '@/shared/file-store';

// Upload
const { fileKey } = await fileStore.upload({ buffer, mimeType, folder: 'resumes' });

// Get presigned URL (15 min default)
const url = await fileStore.getPresignedUrl(fileKey, { expiresIn: 900 });
```

After upload, always create a `Document` record in the DB pointing to the `fileKey`.

### Template engine contract

All templated content goes through `src/shared/template-engine.ts`.

```typescript
import { templateEngine } from '@/shared/template-engine';

const html = await templateEngine.render('interview-invitation', {
  candidateName: 'Priya Sharma',
  roleTitle: 'Product Designer',
  schedulingLink: 'https://...',
});
```

Template IDs map to records in the `templates` table (managed by HR via UI). Never hardcode email body strings in code.

### Analytics contract

Analytics reads **only** from `event_log`. It must never join against operational tables (`applications`, `candidates`, etc.) to avoid performance impact on live queries.

```typescript
// Correct — read from event_log
const stageChanges = await prisma.eventLog.findMany({
  where: { action: Events.APPLICATION_STAGE_CHANGED },
});

// Wrong — never query operational tables from analytics
const apps = await prisma.application.findMany();
```

---

## API design

### REST conventions

- Base path: `/api/v1/`
- All IDs in path params are UUIDs
- Pagination: `?page=1&limit=20` (default limit: 20, max: 100)
- Sorting: `?sortBy=createdAt&sortDir=desc`
- All timestamps: ISO 8601 UTC (`2026-05-16T10:30:00.000Z`)
- All money values: integers in smallest currency unit (paise for INR)

### Standard response envelope

```typescript
// Success
{
  "success": true,
  "data": { ... },
  "meta": { "page": 1, "limit": 20, "total": 143 }  // on list endpoints
}

// Error
{
  "success": false,
  "error": {
    "code": "CANDIDATE_NOT_FOUND",     // snake_upper_case
    "message": "Human readable string",
    "details": {}                       // optional, validation errors etc.
  }
}
```

### Route naming

```
GET    /api/v1/applications              list
POST   /api/v1/applications              create
GET    /api/v1/applications/:id          get one
PATCH  /api/v1/applications/:id          update (partial)
DELETE /api/v1/applications/:id          delete

POST   /api/v1/applications/:id/stage   stage transition (not a generic PATCH)
POST   /api/v1/interviews/:id/scorecard submit scorecard
POST   /api/v1/offers/:id/approve       approve offer
POST   /api/v1/offers/:id/sign          candidate signs offer
```

Use sub-resources for actions. Avoid verbs in URLs except for explicit action endpoints (`/approve`, `/sign`, `/cancel`).

### Fastify schema validation

Every route must declare a Fastify schema. This is not optional — it is the contract with the frontend.

```typescript
const createApplicationSchema = {
  body: {
    type: 'object',
    required: ['candidateId', 'requisitionId'],
    properties: {
      candidateId: { type: 'string', format: 'uuid' },
      requisitionId: { type: 'string', format: 'uuid' },
    },
    additionalProperties: false,
  },
  response: {
    201: applicationResponseSchema,
    400: errorSchema,
    403: errorSchema,
  },
};
```

---

## Conventions

### TypeScript

- Strict mode always on
- No `any` — use `unknown` and narrow, or define the type
- Zod schemas are the source of truth for runtime validation; derive TypeScript types from them: `type CreateApplication = z.infer<typeof createApplicationSchema>`
- Barrel files (`index.ts`) only at the module level, not inside subdirectories

### Naming

| Thing | Convention | Example |
|---|---|---|
| Files | kebab-case | `candidates.service.ts` |
| Variables / functions | camelCase | `getApplicationById` |
| Classes | PascalCase | `NotificationWorker` |
| Constants | UPPER_SNAKE_CASE | `MAX_RETRY_ATTEMPTS` |
| DB columns | snake_case (Prisma maps) | `created_at` |
| API JSON fields | camelCase | `createdAt` |
| Event names | `entity.action` | `application.stage_changed` |
| Environment variables | UPPER_SNAKE_CASE | `DATABASE_URL` |

### Error handling

```typescript
// Use typed application errors — never throw raw Error objects
import { AppError } from '@/shared/errors';

throw new AppError('CANDIDATE_NOT_FOUND', 'Candidate with this ID does not exist', 404);
```

All unhandled errors must be caught by Fastify's global error handler (already configured in `src/app.ts`). Never swallow errors silently.

### Database

- All schema changes via Prisma migrations: `npx prisma migrate dev --name describe_the_change`
- Migration names must describe the change in snake_case: `add_source_detail_to_candidates`
- Never edit a migration file after it has been committed
- Seeds live in `prisma/seed.ts` — always include: 1 HR admin, 1 hiring manager, 1 interviewer, 1 executive, 2 open requisitions, 5 candidates across stages
- All queries that return lists must include pagination — never return unbounded results

### Testing

- Unit tests: services and repos (mock Prisma with `jest-mock-extended`)
- Integration tests: routes (use a real test DB, reset with `prisma migrate reset` before each suite)
- E2E tests: critical user journeys only (candidate applies → interview scheduled → offer sent)
- Minimum coverage: 80% on service layer, 60% overall
- Test file sits in the same module folder as the code it tests

---

## Phase map

What to build in what order. Never start a module before its dependencies are done.

```
Foundation (build before anything else)
  ├── Auth & RBAC middleware
  ├── Unified DB schema (all tables, initial migration)
  ├── Event bus setup (BullMQ + Redis)
  ├── File store wrapper
  └── Template engine + seed templates

Phase 1 — Core hiring loop
  ├── Requires: Foundation complete
  ├── Requisitions module
  ├── Candidates module + profile
  ├── Applications module + pipeline
  ├── Scheduling module (basic — no calendar sync yet)
  └── Scorecards module

Phase 2 — Automation & stakeholder access
  ├── Requires: Phase 1 complete
  ├── Notification engine workers (connect events to emails)
  ├── Comms module + candidate status portal
  ├── Approval workflows service
  ├── Offer module (generation + signing)
  ├── Onboarding module (checklist + tasks)
  └── Stakeholder dashboards (HR, manager, interviewer, exec views)

Phase 3 — Intelligence & integrations
  ├── Requires: Phase 2 complete + event_log populated
  ├── Analytics module (reads from event_log only)
  ├── Source tracking (UTM tags + attribution reports)
  ├── Global search (Postgres full-text or Meilisearch)
  └── External integrations (Google Calendar, Slack, HRIS)
```

**Hard dependency rules:**
1. RBAC middleware must gate all routes before Phase 1 modules are built
2. Event bus must be running before scheduling or comms modules
3. Template engine must have seed templates before comms or offer modules
4. Approval service must be complete before offer module (offer send is gated by approval)
5. Offer module must be complete before onboarding module (`offer.accepted` triggers onboarding)
6. Phase 1 must have at least 2 weeks of data in `event_log` before analytics is useful

---

## Local development setup

```bash
# 1. Start infrastructure
docker-compose up -d

# 2. Install dependencies
pnpm install

# 3. Run migrations and seed
cd apps/api
npx prisma migrate dev
npx prisma db seed

# 4. Start dev servers (from root)
pnpm dev   # starts api on :3000 and web on :5173

# 5. Check mailhog for test emails
open http://localhost:8025

# 6. Check minio for file uploads
open http://localhost:9001  # admin / minioadmin
```

Environment variables (copy `.env.example` to `.env`):

```
DATABASE_URL=postgresql://postgres:postgres@localhost:5432/hiring_tool
REDIS_URL=redis://localhost:6379
S3_ENDPOINT=http://localhost:9000
S3_ACCESS_KEY=minioadmin
S3_SECRET_KEY=minioadmin
S3_BUCKET=hiring-tool
SMTP_HOST=localhost
SMTP_PORT=1025
JWT_SECRET=change-me-in-production
JWT_REFRESH_SECRET=also-change-me
CANDIDATE_PORTAL_URL=http://localhost:5173/portal
```

---

## Common mistakes to avoid

1. **Calling email/notification logic directly from a service.** Always emit an event.
2. **Filtering data after fetching all rows.** Scope at the DB query level.
3. **Using `any` in TypeScript.** Infer types from Zod schemas.
4. **Writing unbounded list queries.** Always paginate.
5. **Querying operational tables from analytics.** Read only from `event_log`.
6. **Hardcoding email body strings.** Always use the template engine.
7. **Forgetting to log to `event_log` alongside emitting an event.** Both must always happen together — use the `logEvent` + `eventBus.emit` pattern.
8. **Cross-module imports.** Modules talk via event bus or shared services only.
9. **Skipping Fastify schema on a route.** Every route needs a schema — it's the API contract.
10. **Building Phase 2 before Phase 1 is stable.** Respect the build order.

---

*Version: 2.0 — May 2026*
*Project: Unified hiring tool*
*Stack: .NET 9 / EF Core / PostgreSQL / Angular 21*