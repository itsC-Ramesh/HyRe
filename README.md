# HyRe

HyRe is a unified hiring and recruitment platform designed to modernize the way organizations find, evaluate, and hire talent.

## 🚀 Vision
The goal of HyRe is to eliminate the friction in recruitment by providing a unified portal for HR Admins, Hiring Managers, Interviewers, and Candidates. By leveraging a granular Role-Based Access Control (RBAC) system and a modern tech stack, it ensures that the right people have the right tools at every stage of the hiring pipeline.

## 🛠 Tech Stack
- **Backend**: .NET 10 (Core)
- **Architecture**: Clean Architecture (Domain-Driven Design)
- **Orchestration**: .NET Aspire
- **Database**: PostgreSQL
- **Security**: JWT-based Authentication with Refresh Token Rotation
- **Messaging**: MediatR (CQRS Pattern)

## 🔐 Security & Access Control
HyRe implements a robust security layer:
- **Stateless JWT Auth**: High-performance authentication without server-side session bloat.
- **Refresh Token Rotation**: Enhanced security with automatic token rotation and reuse detection.
- **Granular Permissions**: A comprehensive permission matrix (`Resource:Action`) ensures least-privilege access.
- **Audit Logging**: Every sensitive action—from registration to role changes—is captured in an append-only audit trail.

### Role Hierarchy
1. **HR Admin**: Full platform governance and user management.
2. **Hiring Manager**: End-to-end requisition and candidate pipeline control.
3. **Interviewer**: Focused access for evaluating candidates and submitting scorecards.
4. **Executive**: Strategic oversight through analytics and high-level reports.
5. **Candidate**: A secure portal for application tracking and communications.

## 🏗 Getting Started

### Prerequisites
- .NET 10 SDK
- Docker Desktop (for PostgreSQL/Aspire resources)

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run --project src/AppHost
```
The **Aspire Dashboard** will launch automatically, providing real-time logs, metrics, and endpoint traces.

### Test
```bash
dotnet test
```

## 📜 Development Standards
- **Clean Code**: Adherence to SOLID principles and DDD patterns.
- **Observability**: Built-in OpenTelemetry support via .NET Aspire.
- **Scalability**: Designed for containerized deployments and microservice evolution.

---
*Built with ❤️ for modern recruitment teams.*
