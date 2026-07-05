# HyRe

HyRe is a full-stack hiring and recruitment platform built with .NET, Angular, PostgreSQL, and .NET Aspire. It models the core workflow of a recruiting team: creating requisitions, tracking candidates through a hiring pipeline, scheduling interviews, collecting scorecards, managing notes and documents, and notifying users when important hiring events occur.

The project is designed as a practical clean-architecture application rather than a toy CRUD demo. It separates domain rules, application use cases, infrastructure concerns, and web delivery so the codebase can grow without every feature becoming tightly coupled to the API layer.

## What It Demonstrates

- End-to-end hiring workflow across requisitions, candidates, applications, pipeline stages, interviews, scorecards, notes, documents, tags, templates, and notifications.
- Clean Architecture with Domain, Application, Infrastructure, Web, AppHost, ServiceDefaults, and Shared projects.
- CQRS-style request handling with MediatR and FluentValidation.
- JWT authentication, refresh tokens, role-based permissions, and audit/event logging.
- PostgreSQL persistence through Entity Framework Core with migrations and repository boundaries.
- Background processing with Hangfire for notification and asynchronous job workflows.
- Angular frontend for recruiter-facing workflows, including candidates, requisitions, pipeline, interviews, scorecards, dashboard, auth, layout, and notifications.
- .NET Aspire orchestration for the API, frontend, PostgreSQL, service discovery, health checks, telemetry, and local developer experience.
- Test coverage across domain, application, infrastructure, functional, and browser acceptance test projects.

## Tech Stack

| Area | Tools |
| --- | --- |
| Backend | .NET 10, ASP.NET Core Minimal APIs, MediatR, FluentValidation |
| Frontend | Angular 21, TypeScript, RxJS, Angular CDK, Tailwind CSS |
| Data | PostgreSQL, Entity Framework Core, EF migrations |
| Auth & Security | ASP.NET Core Identity, JWT bearer auth, refresh tokens, RBAC permissions |
| Jobs & Messaging | Hangfire, domain events, notification handlers |
| Storage & Integrations | Local file storage, AWS S3 abstractions, MailKit email service |
| Orchestration | .NET Aspire AppHost, ServiceDefaults, OpenTelemetry |
| Testing | NUnit, Shouldly, Moq, Respawn, Reqnroll, Playwright |

## Architecture

```text
src/
  Domain/          Core entities, value objects, enums, domain events, permissions
  Application/     Commands, queries, validators, interfaces, behaviours
  Infrastructure/  EF Core, repositories, identity, storage, email, jobs, services
  Web/             Minimal API endpoints, auth, Angular client app, OpenAPI/Scalar
  AppHost/         .NET Aspire orchestration for local development
  ServiceDefaults/ Shared service discovery, health checks, telemetry defaults
  Shared/          Cross-project service names and shared constants

tests/
  Domain.UnitTests/
  Application.UnitTests/
  Application.FunctionalTests/
  Infrastructure.IntegrationTests/
  Web.AcceptanceTests/
  TestAppHost/
```

The application layer owns use cases such as creating candidates, approving requisitions, advancing applications, scheduling interviews, submitting scorecards, and uploading documents. The web layer stays thin by routing HTTP requests into commands and queries, then returning a consistent `ApiResponse` envelope.

## Core Modules

- **Requisitions**: create, update, submit, approve, reject, hold, close, and clone job requisitions.
- **Candidates**: create candidate profiles, search candidates, update details, and apply candidates to requisitions.
- **Pipeline**: view applications by requisition, advance/reject applications, and bulk-advance candidates between stages.
- **Interviews**: schedule, reschedule, cancel, complete, mark no-shows, and manage interviewer availability.
- **Scorecards**: save drafts, submit feedback, summarize evaluations, and track interviewer scorecards.
- **Documents & Files**: upload candidate/application documents through pluggable storage services.
- **Notifications**: react to hiring events such as candidate creation, interview updates, scorecard submission, offer changes, and pipeline movement.
- **Security**: role-aware permission matrix for HR admins, hiring managers, interviewers, executives, candidates, and administrators.

## Getting Started

### Prerequisites

- .NET 10 SDK, matching `global.json`
- Node.js and npm for the Angular client
- Docker Desktop for the Aspire-managed PostgreSQL container

### Restore and Build

```bash
dotnet restore
dotnet build
```

### Run with Aspire

```bash
dotnet run --project src/AppHost
```

The Aspire AppHost starts PostgreSQL, the ASP.NET Core API, and the Angular frontend. It also exposes the Aspire dashboard for logs, traces, health checks, and service endpoints.

Useful local endpoints:

- API and Scalar reference: exposed by the `Web` service in the Aspire dashboard
- Frontend: exposed by the `WebFrontend` service in the Aspire dashboard
- Scalar API docs: `/scalar`

### Frontend Only

```bash
cd src/Web/ClientApp
npm install
npm start
```

The Angular dev server uses `proxy.conf.json` for API calls during local development.

### Tests

```bash
dotnet test
```

Browser acceptance tests live under `tests/Web.AcceptanceTests` and use Playwright/Reqnroll. If Playwright browsers are not installed on a fresh machine, install them before running the acceptance suite.

## Project Status

HyRe is an active open project for building a modern hiring platform with production-oriented backend design, a structured Angular frontend, and local cloud-native development through .NET Aspire. Contributions, issues, and implementation ideas are welcome as the product surface matures.

## Why This Project Exists

Recruitment tools often split work across spreadsheets, inboxes, applicant trackers, calendars, and manual status updates. HyRe explores a unified workflow where recruiting data, candidate movement, interviews, scorecards, documents, notifications, and permissions live in one system with a maintainable architecture underneath it.
