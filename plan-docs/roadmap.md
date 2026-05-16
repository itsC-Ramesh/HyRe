# Unified hiring tool — full product roadmap

> Internal product spec for a unified, end-to-end hiring and onboarding platform.
> Target: startup / SMB (< 200 employees). All six pain points addressed: application tracking, interview scheduling, stakeholder visibility, candidate communications, offer & onboarding, and analytics.

---

## Table of contents

1. [Product vision](#1-product-vision)
2. [Stakeholders & roles](#2-stakeholders--roles)
3. [Architecture overview](#3-architecture-overview)
4. [Foundation — must build first](#4-foundation--must-build-first)
5. [Phase 1 — Core hiring loop (weeks 1–8)](#5-phase-1--core-hiring-loop-weeks-18)
6. [Phase 2 — Automation & stakeholder access (weeks 9–16)](#6-phase-2--automation--stakeholder-access-weeks-916)
7. [Phase 3 — Intelligence & analytics (weeks 17–24)](#7-phase-3--intelligence--analytics-weeks-1724)
8. [Integration map](#8-integration-map)
9. [Event-driven triggers](#9-event-driven-triggers)
10. [Data model (core entities)](#10-data-model-core-entities)
11. [Role-based access control matrix](#11-role-based-access-control-matrix)
12. [Build sequence & dependencies](#12-build-sequence--dependencies)

---

## 1. Product vision

**Problem:** Manual handling of applications, scattered interview scheduling, no unified status tracking, and fragmented communication across HR, managers, interviewers, executives, and candidates.

**Solution:** A single internal platform where every stakeholder — candidates, HR, hiring managers, interviewers, and executives — has a role-appropriate self-serve portal. All data lives in one place. All actions trigger the right automated responses.

**Core design principles:**
- Single source of truth — one candidate record, one application record, no duplicates
- Event-driven — every significant action emits an event; automation reacts to events, not manual triggers
- Role-scoped — every stakeholder sees exactly what they need, nothing more
- Self-serve — candidates track their own status; interviewers submit their own scorecards; managers approve from their inbox

---

## 2. Stakeholders & roles

| Role | What they do in the system | Access level |
|---|---|---|
| **HR admin** | Manages everything — roles, candidates, comms, reports | Full read/write across all modules |
| **Hiring manager** | Creates requisitions, reviews scorecards, approves offers | Read/write on their own roles; read-only on others |
| **Interviewer** | Shares availability, receives assignments, submits scorecards | Calendar + scorecard module only |
| **Executive** | Reviews hiring health at org level | Read-only aggregate dashboards |
| **Candidate** | Tracks own application status, self-schedules interviews | Candidate portal only (own data only) |

---

## 3. Architecture overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Application layer                         │
│                                                                  │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐   │
│  │ HR admin │  │ Manager  │  │Interviewer│  │  Candidate   │   │
│  │  portal  │  │  portal  │  │  portal  │  │    portal    │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └──────┬───────┘   │
│       └─────────────┴──────────────┴────────────────┘           │
│                              │                                   │
│                         RBAC gateway                             │
│                              │                                   │
├──────────────────────────────┼──────────────────────────────────┤
│                        Module layer                              │
│                                                                  │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  Requisition│  │   Pipeline   │  │  Interview scheduling │   │
│  │   module    │  │   & tracking │  │       module         │   │
│  └─────────────┘  └──────────────┘  └──────────────────────┘   │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  Candidate  │  │    Offer &   │  │  Analytics &         │   │
│  │    comms    │  │  onboarding  │  │    reporting         │   │
│  └─────────────┘  └──────────────┘  └──────────────────────┘   │
│                                                                  │
├──────────────────────────────────────────────────────────────────┤
│                       Foundation layer                           │
│                                                                  │
│  Auth & RBAC │ Unified data store │ Notification engine          │
│  File store  │  Template engine   │ Audit log                    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Foundation — must build first

These are shared services consumed by every module. Build these before any feature module. Skipping or deferring them creates compounding technical debt.

---

### 4.1 Auth & RBAC

**Purpose:** Login, session management, and permission enforcement across every route and action.

**Features:**
- Email/password login with session tokens (add SSO in Phase 2)
- Role definitions: `hr_admin`, `hiring_manager`, `interviewer`, `executive`, `candidate`
- Permission middleware on every API endpoint
- Route-level guards on frontend views
- Candidate portal has a separate, scoped session (no access to internal views)
- Audit log of every authenticated action (who did what, when)

**Integrations:**
- All modules check RBAC before returning data
- Notification engine checks permissions before sending links
- Candidate portal session scope enforced separately from internal roles

**Key decision:** Define your permission model before writing a single module. Adding RBAC retroactively is one of the most painful retrofits in internal tooling.

---

### 4.2 Unified data store

**Purpose:** Single database schema shared by all modules. No duplicate candidate records. No sync between separate systems.

**Core tables:**

| Table | Key fields |
|---|---|
| `candidates` | id, name, email, phone, source, created_at |
| `requisitions` | id, title, department, owner_id, status, salary_band, created_at |
| `applications` | id, candidate_id, requisition_id, stage, created_at, updated_at |
| `interviews` | id, application_id, interviewer_id, scheduled_at, type, status |
| `scorecards` | id, interview_id, interviewer_id, ratings (JSON), notes, submitted_at |
| `offers` | id, application_id, salary, start_date, status, signed_at |
| `events_log` | id, entity_type, entity_id, action, actor_id, metadata (JSON), created_at |
| `notifications` | id, recipient_id, type, payload (JSON), read_at, created_at |
| `documents` | id, entity_type, entity_id, file_key, type, created_at |

**Integrations:**
- All modules read/write here
- Events log is append-only (never update, never delete)
- Analytics reads exclusively from `events_log` to avoid impacting live performance

---

### 4.3 Notification engine

**Purpose:** Centralised event bus. Modules emit events; this service routes them to the right delivery channel (email, in-app, webhook).

**Features:**
- Event subscription model — modules publish events, engine routes to subscribers
- Email delivery via SMTP or SendGrid
- In-app notification feed (bell icon, unread count)
- Webhook support for external integrations (Phase 3)
- Delivery status tracking (sent, bounced, opened)
- Retry logic on failure (3 attempts, exponential backoff)

**Events consumed (examples):**
- `application.created` → acknowledgement email to candidate
- `stage.changed` → status email to candidate + internal notification to HR
- `interview.booked` → calendar invite + reminder emails
- `scorecard.submitted` → notification to hiring manager
- `offer.approved` → offer letter sent to candidate
- `approval.pending` (> 48h) → escalation to approver's manager

**Integrations:**
- Pipeline module emits stage-change events
- Scheduling module emits booking/cancellation events
- Offer module emits approval and acceptance events
- Candidate portal consumes in-app notifications

---

### 4.4 File & document store

**Purpose:** Centralised, access-controlled storage for all files: resumes, scorecards, offer letters, onboarding docs.

**Features:**
- Resume upload (PDF, DOCX) with version history
- Offer letter PDF generation from template
- Onboarding document templates
- Presigned, time-limited download URLs (no public file access)
- File metadata stored in `documents` table; binaries in S3-compatible store

**Integrations:**
- Candidate profiles link to resume files
- Offer module generates and stores signed offer PDFs
- Onboarding module reads checklist templates
- RBAC enforced on all file access

---

### 4.5 Template engine

**Purpose:** Reusable, editable content templates for all outbound content — emails, offer letters, scorecards, onboarding checklists.

**Features:**
- Dynamic variable substitution: `{{candidate.name}}`, `{{role.title}}`, `{{offer.start_date}}`
- Version control on templates (edit with history)
- HR-editable via rich text editor (no code required)
- Preview mode before sending
- Template categories: email, offer_letter, scorecard, onboarding_checklist

**Built-in templates to ship on day one:**
- Application acknowledgement email
- Interview invitation email
- Rejection email
- Offer letter (standard)
- Onboarding day-1 checklist

**Integrations:**
- Comms module uses email templates
- Offer module uses offer letter template
- Onboarding module uses checklist templates
- Scorecard module uses structured feedback template

---

## 5. Phase 1 — Core hiring loop (weeks 1–8)

**Goal:** Replace the most painful manual work. By end of Phase 1, HR can open a role, track candidates through stages, schedule interviews, and collect structured feedback — all in one place.

---

### 5.1 Job requisitions

**Purpose:** The starting point for every hiring workflow. Create and approve open roles before any candidate tracking begins.

**Features:**
- Create role: title, department, hiring manager, job description, salary band, target headcount
- Approval workflow: hiring manager submits → HR admin approves → requisition is live
- Status: `draft` → `pending_approval` → `open` → `on_hold` → `closed`
- Clone from previous requisition (saves time for recurring roles)
- Link to job board post URL (manual in Phase 1; automated in Phase 3)
- Internal notes field (not visible to candidates)

**Integrations:**
- Feeds candidate pipeline (applications are scoped to a requisition)
- Drives interviewer panel assignments
- Scopes all analytics reports (per-role, per-department)
- Approval workflow uses the shared approval service (Phase 2)

---

### 5.2 Candidate pipeline

**Purpose:** Visual kanban of every candidate across all stages. The primary daily-use view for HR.

**Stages:**
```
Applied → Screened → Interview → Offer → Hired
                                       → Rejected (available at any stage)
```

**Features:**
- Kanban board per requisition (cards = candidates)
- Drag-to-move between stages with confirmation prompt
- Automatic stage-change event emitted on move
- Bulk status update (select multiple candidates, move stage)
- Candidate tagging: `strong`, `hold`, `referral`, `passive`
- Quick-view card: name, current stage, days in stage, last activity
- List view alternative for power users
- Filter by: stage, tag, requisition, interviewer, date applied

**Integrations:**
- Writes `stage.changed` events to notification engine on every move
- Stage = "Interview" links to scheduling module to book
- Stage = "Offer" links to offer module
- Stage = "Hired" triggers onboarding checklist creation
- All stage history stored in `events_log`

---

### 5.3 Candidate profiles

**Purpose:** Single record per candidate. Every piece of information, communication, and activity in one place.

**Sections:**
- **Overview:** Name, email, phone, source, applied roles, current stage
- **Resume:** Uploaded file + parsed fields (name, experience, education, skills)
- **Activity timeline:** Every stage change, interview, scorecard, email — in chronological order
- **Scorecards:** All interviewer feedback aggregated in one view
- **Internal notes:** HR and hiring manager comments (never visible to candidate)
- **Communications:** All emails sent/received, threaded

**Features:**
- Duplicate detection on email address at application time
- Source tracking: referral, LinkedIn, job board, direct, agency
- "Applied before" flag if candidate exists from a previous role
- Link to candidate portal (what the candidate sees)

**Integrations:**
- Profile linked from every pipeline card
- Scorecards attach directly to profile (via interview → scorecard chain)
- Comms module logs all email threads here
- File store links resume PDFs here

---

### 5.4 Interview scheduling

**Purpose:** Eliminate calendar back-and-forth. Interviewers share availability; candidates self-book from a link.

**Features:**
- **Interviewer availability:** Each interviewer sets available slots in a weekly calendar UI
- **Scheduling link:** HR generates a link per candidate; candidate picks from available slots
- **Panel interviews:** Coordinate multiple interviewers into a single slot (intersection of availability)
- **Interview types:** Phone screen, video call, technical, onsite, culture
- **Automatic confirmations:** Calendar invite sent to all parties on booking
- **Reminders:** Automated emails at 24h and 1h before interview
- **Reschedule/cancel:** Candidate or HR can reschedule; reason captured; all parties notified
- **No-show handling:** Mark as no-show, option to reschedule or reject

**Calendar sync (Phase 1 — basic):**
- iCal invite attachment in confirmation emails
- Full Google/Outlook two-way sync in Phase 3

**Integrations:**
- Reads interviewer list from RBAC (only `interviewer` and `hiring_manager` roles)
- Writes booked slot to `interviews` table on candidate profile
- Emits `interview.booked` event to notification engine
- Triggers scorecard creation for interviewer on booking

---

### 5.5 Scorecards

**Purpose:** Replace informal, inconsistent interview feedback with structured, comparable evaluations.

**Template structure:**
```
Rating dimensions (1–5 scale):
  - Technical / role-specific skills
  - Communication
  - Problem solving
  - Culture fit
  - Overall recommendation: Strong yes / Yes / No / Strong no

Free text:
  - Strengths (required)
  - Concerns (required)
  - Notes (optional)
```

**Features:**
- Per-round scorecard (each interview gets its own)
- Interviewer cannot see other scorecards before submitting their own (blind review)
- After all scorecards submitted, aggregate view unlocked for hiring manager
- Scorecard status: `pending` → `submitted`
- Reminder sent to interviewer if not submitted within 24h of interview
- HR can view all scorecards at any time

**Integrations:**
- Automatically created when interview is booked (from scheduling module)
- Attached to candidate profile under the relevant application
- Readable by hiring manager before offer decision
- Analytics module reads aggregated scores for quality-of-hire reporting (Phase 3)

---

## 6. Phase 2 — Automation & stakeholder access (weeks 9–16)

**Goal:** Reduce manual triggers. Open the tool to all stakeholders with role-appropriate views. Close the hiring loop with a clean offer-to-onboarding flow.

---

### 6.1 Candidate communications

**Purpose:** Every candidate knows where they stand, automatically. HR stops writing one-off emails.

**Features:**
- **Trigger-based auto-emails:** Stage changes emit emails from predefined templates
- **Candidate status portal:** Candidates log in (magic link) and see their application status, upcoming interviews, and next steps — no need to email HR asking "where am I?"
- **Two-way reply threading:** Candidate replies to emails are captured and logged on their profile
- **Manual send:** HR can send ad hoc emails from any template at any time
- **Opt-out management:** Candidate can opt out of non-essential comms; mandatory process comms still send

**Automatic triggers:**
| Stage change | Email sent |
|---|---|
| Applied | "We received your application" |
| Screened → Interview | "We'd like to invite you to interview" + scheduling link |
| Any stage → Rejected | "Thank you for your time" (with optional personalised note) |
| Offer sent | "Your offer letter is ready" |

**Integrations:**
- All triggers fired by notification engine on `stage.changed` events
- Email content from template engine (HR can edit templates)
- All threads logged on candidate profile
- Candidate portal reads application status directly from data core

---

### 6.2 Stakeholder dashboards

**Purpose:** Every stakeholder has a view built for their job. Nobody needs to ask HR for a status update.

**HR admin view:**
- All open requisitions with candidate counts per stage
- Full pipeline across all roles (filterable)
- Overdue actions (pending approvals, overdue scorecards, stalled candidates)
- Activity feed (today's interviews, recent stage changes)

**Hiring manager view:**
- Roles they own: candidate pipeline per role
- Scorecards for their roles (read-only until all submitted, then full view)
- Pending approvals requiring their action
- Interview calendar for their roles

**Interviewer view:**
- Upcoming interviews (calendar view)
- Pending scorecards to complete
- Candidate brief (name, role, CV, interview type) — no other candidate data visible

**Executive view:**
- Org-level funnel: open roles, total pipeline, avg time-to-hire
- Role health summary (roles with no movement in > 2 weeks flagged)
- Department-level breakdown
- No candidate-level data

**Integrations:**
- RBAC gates which data each role can query
- Reads from data core and events log
- Interviewer view links to scheduling module
- Executive view pre-populated from analytics module (Phase 3)

---

### 6.3 Approval workflows

**Purpose:** Enforce sign-off gates on requisitions and offers. Nothing slips through without the right approvals.

**Features:**
- Configurable approval chains per entity type (requisitions, offers)
- Email-based approval actions: approver receives email with "Approve" / "Reject" buttons (no login required)
- Rejection requires a reason (stored in audit log)
- Escalation: if no action within 48h, reminder sent; after 72h, escalated to approver's manager
- Parallel approvals: multiple approvers can be required (all must approve)
- Audit trail: every approval decision recorded with timestamp and actor

**Default approval chains:**
- New requisition: hiring manager → HR admin
- Offer letter: HR admin → hiring manager (for compensation review)

**Integrations:**
- Requisition module uses for new role approvals
- Offer module uses before letter is sent
- Notification engine sends approval request emails
- All decisions written to `events_log`

---

### 6.4 Offer & onboarding

**Purpose:** From verbal yes to signed doc to Day 1 checklist — one continuous flow, no manual handoffs.

**Offer flow:**
1. HR initiates offer from candidate profile (stage = Offer)
2. Offer details entered: salary, start date, role, contract type, expiry date
3. Offer letter generated from template (dynamic variables substituted)
4. HR admin + hiring manager approval required (via approval workflow)
5. On approval: letter sent to candidate via email with signing link
6. Candidate signs digitally; signed copy stored in file store
7. Stage automatically moves to "Hired" on signature

**Onboarding flow (post-acceptance):**
- Onboarding checklist automatically created on `offer.accepted` event
- Checklist assigned to HR (internal tasks) and new hire (self-serve tasks)
- IT provisioning task list generated (email setup, laptop, system access)
- New hire receives access to onboarding portal with their checklist
- HR tracks completion; overdue tasks flagged in HR dashboard

**Integrations:**
- Approval workflows gates letter send
- File store holds generated and signed offer PDFs
- Template engine generates offer letter content
- Notification engine sends letter to candidate and triggers onboarding checklist
- Data core stage updated to "Hired" on signature

---

## 7. Phase 3 — Intelligence & analytics (weeks 17–24)

**Goal:** Turn accumulated data into decisions. Surface patterns, predict bottlenecks, connect to the wider stack.

---

### 7.1 Analytics & reports

**Purpose:** Actionable insight for HR and leadership. Move from gut feel to data.

**Metrics:**

| Metric | Definition |
|---|---|
| Time-to-hire | Days from requisition open to offer accepted |
| Time-in-stage | Average days candidates spend at each pipeline stage |
| Funnel conversion rate | % of candidates moving from stage to stage |
| Drop-off by stage | Where candidates are being lost |
| Offer acceptance rate | Offers sent vs accepted |
| Interviewer load | Interviews per interviewer per week/month |
| Source-to-hire | Which sourcing channel produces the most hires |
| Scorecard distribution | Average ratings per dimension across all interviews |

**Report types:**
- Role report: full funnel for a single requisition
- Department report: aggregate across all roles in a department
- Interviewer report: load, scorecard completion rate, average ratings given
- Executive summary: org-wide, exportable as PDF for leadership review

**Features:**
- Date range filter
- Department and role filters
- Scheduled report delivery (weekly email to exec list)
- CSV export for all data

**Integrations:**
- Reads exclusively from `events_log` (never queries live operational tables)
- Feeds executive dashboard in stakeholder module
- Scorecard data joined from `scorecards` table for quality metrics
- Report delivery via notification engine

---

### 7.2 Source tracking

**Purpose:** Know which hiring channel produces the best candidates and hires.

**Features:**
- Source tags on application intake links (UTM-style: `?source=linkedin`, `?source=referral`)
- Source field on candidate profile (auto-populated from link, editable by HR)
- Source-to-stage funnel: how far do candidates from each source typically get?
- Source-to-hire conversion rate per channel
- Cost-per-hire by source (manual cost input by HR per channel)

**Sources tracked:**
- Direct (application link)
- LinkedIn
- Job board (configurable: Naukri, Indeed, etc.)
- Employee referral (with referrer name)
- Agency (with agency name)
- Passive / headhunted

**Integrations:**
- Source stored on `candidates` table in data core
- Analytics module aggregates source data for reports
- Requisition module shows channel performance per role

---

### 7.3 Global search & filters

**Purpose:** Find any candidate, role, document, or note instantly across the entire system.

**Features:**
- Full-text search across: candidate names, notes, role titles, company names, skills
- Advanced filters: stage, requisition, interviewer, source, date applied, date of last activity
- Saved filter views (e.g. "All strong candidates in engineering roles")
- Search results link directly to candidate profiles or pipeline cards
- Recent searches remembered per user

**Integrations:**
- Reads from data core (indexed for search performance)
- RBAC filters search results — HR sees all, managers see their roles only
- Results link to pipeline cards and candidate profiles

---

### 7.4 External integrations

**Purpose:** Connect the hiring tool to the rest of the company stack. Remove manual data re-entry at role boundaries.

**Integrations to build:**

| Integration | What it does |
|---|---|
| Google Calendar / Outlook | Two-way sync of interview slots; changes reflect in both systems |
| Slack | Notify HR channel on stage changes; send approval requests via DM |
| Background check (Checkr / similar) | Trigger check on offer acceptance; result webhooks back to profile |
| HRIS (BambooHR, Rippling, Darwinbox) | Export hire record on offer acceptance; no duplicate data entry |
| Job boards (LinkedIn, Naukri, Indeed) | Pull applications directly into pipeline (removes manual import) |

**Integrations:**
- Scheduling module syncs via Google/Outlook Calendar APIs
- Notification engine routes to Slack channels via webhook
- Offer module triggers background check webhook on acceptance
- Data core exports hire record to HRIS via API on `offer.accepted` event

---

## 8. Integration map

How data flows between building blocks:

| Trigger | What happens |
|---|---|
| Candidate applies | Candidate profile created in data core; `application.created` event emitted |
| Pipeline stage changes | Notification engine emits event → template-based email sent to candidate via comms module |
| Interview booked | Calendar invite sent to all parties; scorecard created and assigned to interviewer |
| Scorecard submitted | Stored on candidate profile; hiring manager notified via notification engine |
| All scorecards in | Aggregate view unlocked for hiring manager; hiring decision prompt |
| Offer initiated | Approval workflow triggered; HR admin + hiring manager must approve |
| Offer approved | Offer letter generated from template → sent to candidate with signing link |
| Offer accepted / signed | Stage → Hired; onboarding checklist created; HRIS export triggered; IT provisioning list created |
| All events | Written to `events_log`; analytics module aggregates nightly |

---

## 9. Event-driven triggers

The notification engine subscribes to these events. All automation is event-driven — no cron jobs, no manual sends.

| Event | Automatic action |
|---|---|
| `application.created` | Send acknowledgement email to candidate |
| `stage.changed → screened` | Send interview invitation email + scheduling link |
| `interview.booked` | Send confirmation to candidate + interviewer; schedule reminders |
| `interview.reminder_24h` | Send reminder email to candidate and interviewer |
| `interview.reminder_1h` | Send reminder email to candidate and interviewer |
| `scorecard.overdue` (24h after interview) | Reminder to interviewer to complete scorecard |
| `scorecard.submitted` | Notify hiring manager; check if all scorecards now in |
| `all_scorecards.submitted` | Notify HR + hiring manager that aggregate view is ready |
| `stage.changed → rejected` | Send rejection email to candidate |
| `offer.initiated` | Send approval request to approval chain |
| `approval.pending` (> 48h) | Send escalation reminder to approver |
| `offer.approved` | Send offer letter to candidate via email |
| `offer.accepted` | Move stage to Hired; create onboarding checklist; trigger HRIS export |
| `offer.declined` | Notify HR; stage back to Interview for reconsideration |
| `onboarding.task_overdue` | Remind assigned person (HR or new hire) |

---

## 10. Data model (core entities)

### Candidates
```
id              UUID, PK
name            string
email           string, UNIQUE
phone           string, nullable
source          enum (direct, linkedin, job_board, referral, agency, headhunted)
source_detail   string, nullable (referrer name, agency name, UTM)
resume_doc_id   UUID, FK → documents
created_at      timestamp
updated_at      timestamp
```

### Requisitions
```
id              UUID, PK
title           string
department      string
owner_id        UUID, FK → users (hiring manager)
jd_text         text
salary_min      integer, nullable
salary_max      integer, nullable
headcount       integer, default 1
status          enum (draft, pending_approval, open, on_hold, closed)
created_at      timestamp
updated_at      timestamp
```

### Applications
```
id              UUID, PK
candidate_id    UUID, FK → candidates
requisition_id  UUID, FK → requisitions
stage           enum (applied, screened, interview, offer, hired, rejected)
rejection_reason string, nullable
created_at      timestamp
updated_at      timestamp
```

### Interviews
```
id              UUID, PK
application_id  UUID, FK → applications
interviewer_id  UUID, FK → users
type            enum (phone, video, technical, onsite, culture)
scheduled_at    timestamp
duration_min    integer
status          enum (scheduled, completed, cancelled, no_show)
meeting_link    string, nullable
created_at      timestamp
```

### Scorecards
```
id              UUID, PK
interview_id    UUID, FK → interviews
interviewer_id  UUID, FK → users
ratings         JSONB  -- {technical: 4, communication: 5, problem_solving: 3, culture: 4}
recommendation  enum (strong_yes, yes, no, strong_no)
strengths       text
concerns        text
notes           text, nullable
submitted_at    timestamp, nullable
created_at      timestamp
```

### Offers
```
id              UUID, PK
application_id  UUID, FK → applications
salary          integer
currency        string, default 'INR'
start_date      date
contract_type   enum (full_time, contract, internship)
expiry_date     date
status          enum (draft, pending_approval, sent, accepted, declined, expired)
letter_doc_id   UUID, FK → documents, nullable
signed_at       timestamp, nullable
created_at      timestamp
updated_at      timestamp
```

### Events log (append-only)
```
id              UUID, PK
entity_type     string  -- 'application', 'offer', 'interview', etc.
entity_id       UUID
action          string  -- 'stage.changed', 'offer.accepted', etc.
actor_id        UUID, FK → users, nullable (null = system)
metadata        JSONB   -- previous_stage, new_stage, etc.
created_at      timestamp
```

---

## 11. Role-based access control matrix

| Resource | HR admin | Hiring manager | Interviewer | Executive | Candidate |
|---|---|---|---|---|---|
| All requisitions | Read/Write | Own: R/W, Others: Read | None | Read (aggregate) | None |
| All candidate profiles | Read/Write | Own roles: Read | Assigned only: Read | None | Own only |
| Pipeline (all roles) | Read/Write | Own roles: Read/Write | None | Read (aggregate) | None |
| Scorecards (before all submitted) | Read | Read | Own only | None | None |
| Scorecards (after all submitted) | Read | Read | Read (all for role) | None | None |
| Offer details | Read/Write | Read | None | None | Own only |
| Analytics | Read/Write | Own roles | None | Read | None |
| Templates | Read/Write | None | None | None | None |
| User management | Read/Write | None | None | None | None |
| Candidate comms | Read/Write | Own roles | None | None | Own only |
| Onboarding tasks | Read/Write | Read | None | None | Own only |

---

## 12. Build sequence & dependencies

```
Week 1–2   Auth & RBAC
Week 2–3   Unified data store (schema + migrations)
Week 3–4   Notification engine (email delivery only first)
Week 4     File store + Template engine (basic)

Week 5     Job requisitions module
Week 5–6   Candidate profiles + pipeline (kanban)
Week 6–7   Interview scheduling module
Week 7–8   Scorecards module

Week 9–10  Candidate comms (triggers + status portal)
Week 10–11 Approval workflows
Week 11–12 Offer module (generation + signing)
Week 12–13 Onboarding module (checklist + IT tasks)
Week 13–14 Stakeholder dashboards (HR, manager, interviewer, exec views)
Week 15–16 In-app notification feed + Notification engine v2

Week 17–18 Analytics module (events log aggregation)
Week 18–19 Source tracking
Week 19–20 Global search + saved filters
Week 20–22 External integrations: Google Calendar, Slack
Week 22–24 External integrations: HRIS export, job board import, background checks
```

### Hard dependency rules

1. **Auth & RBAC** must be done before any user-facing module
2. **Data store schema** must be stable before building requisitions or pipeline
3. **Notification engine** must be running before comms or scheduling modules
4. **Template engine** must exist before offer module or comms triggers
5. **Pipeline + Scorecards** must be done before analytics (nothing to analyse yet)
6. **Approval workflows** must be done before offer module (offer send gate depends on it)
7. **Offer module** must be done before onboarding module

---

*Last updated: May 2026*
*Status: Concept / pre-development*