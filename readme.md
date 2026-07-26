# Requra — Backend API

Requra is a requirements engineering platform built to help software teams go from raw meeting recordings and documents all the way to structured, approved requirements and user stories — with the help of AI. This repository contains the full backend API that powers the Requra platform.

---

## What Is Requra?

Most software projects fail not because of bad code, but because of unclear requirements. Requra was built to fix that. The idea is simple: you upload your meeting recordings and documents, the system processes them, extracts requirements automatically using AI, and then gives your team a proper workflow to review, approve, and track everything — from initial draft to final sign-off.

Think of it as the bridge between "we had a meeting" and "here are the approved requirements in a structured format."

---

## Architecture

The backend follows **Clean Architecture** with a clear separation of concerns across four layers:

```
Requra.Domain          → Core business entities and rules (no dependencies)
Requra.Application     → Use case interfaces, DTOs, mappings, and validations
Requra.Infrastructure  → Database, external services, background jobs, repositories
Requra.Presentation    → ASP.NET Controllers and API entry point
```

This structure ensures that the domain model stays pure and the business logic doesn't leak into infrastructure concerns. Each layer only depends on what's above it — the domain knows nothing about databases or HTTP.

---

## Features

### Authentication & User Management
- Full registration and login flow with email + password
- **Google OAuth** sign-in (supports Web, Android, and iOS clients)
- **JWT-based authentication** with access tokens and refresh tokens
- **OTP verification** via email for sensitive actions
- User profile management including avatar upload
- Role-based access — users can be admins, analysts, reviewers, or stakeholders

### Project Management
- Create and manage projects with names, descriptions, and languages
- Projects support multiple statuses: In Progress, Drafted, Completed, Cancelled
- Two project types supported (configurable via enum)
- Team members can be added to projects with specific roles
- Soft delete support — projects are never permanently removed by default

### Document Handling
- Upload documents (PDFs and other formats) tied to a project or a meeting session
- Documents are stored on **Cloudinary**
- PDF text extraction is done on the backend using **PdfPig** so the AI pipeline can work with the raw content
- Each document tracks its status (Pending → Processing → Ready / Failed)
- Documents can optionally have a transcript attached (for meeting recordings)

### Meeting Sessions
- Create scheduled meeting sessions linked to a project
- Manage the full lifecycle: Scheduled → Live → Ended / Cancelled
- Meetings can have a host, a list of participants, and platform URLs
- Duration is automatically calculated when a meeting ends
- Invite participants (project members or external stakeholders) via email

### Meeting Invitations
- Send invitations to project members or external stakeholders
- Invitations have expiry times and track their status: Pending, Accepted, Declined, Expired, Revoked
- Supports two invite types (project member vs. external stakeholder)
- Invited users can accept or decline, and the system tracks everything

### Recording Upload & Management
- Supports two upload modes for meeting recordings
- **Chunked upload** — large recordings are broken into chunks and uploaded piece by piece, which handles unreliable connections and large files gracefully
- Each chunk is tracked individually and assembled on the server
- Recordings go through their own lifecycle: READY → ACTIVE → FINALIZING → STOPPED / FAILED / EXPIRED
- A background startup service recovers recordings that were still "processing" when the server was last shut down — so nothing gets stuck
- Files are stored on **Cloudinary** after all chunks are received and merged

### AI Analysis Pipeline
- Trigger an AI analysis run against selected documents in a project
- The backend sends the combined document text to an external AI service (running at a separate endpoint)
- A background worker polls the AI service every few seconds until the job finishes
- Progress and current processing node are tracked in real time
- When the AI finishes, the raw JSON result is saved as an `AnalysisResult` and linked to the run
- If the server restarts mid-analysis, a `StartupRecoveryService` re-queues any runs that were stuck in a processing state
- A `FakeAIClient` is included for development and testing so you don't need the real AI service running locally

### Requirements Management
- Requirements are extracted (manually or by AI) and stored per project
- Each requirement has a type (Functional, Non-Functional, etc.) and a status lifecycle: Drafted → Approved / Rejected
- Requirements can be linked to source documents through a junction table
- Approval workflow: reviewers can approve or reject individual requirements with optional notes

### User Stories
- User stories are created under requirements
- Each story has a title, description, acceptance criteria (stored as a list), priority, and status
- Stories support Jira integration — you can link a story to a Jira ticket ID
- Language-aware: stories track which language they were written in

### Summaries
- The system can generate and store summaries for projects, linked to specific AI models
- Summary status tracking: Pending, Processing, Completed, Failed

### Comments
- Comment system for collaborative feedback on requirements and user stories
- Comments track their own status

### Review Flow
- External stakeholders can be invited to review a project
- Project review invitations have configurable permissions
- Stakeholder feedback is tracked with its own status

---

## Technologies Used

| Area | Technology |
|---|---|
| Framework | ASP.NET Core (**.NET 10**) |
| Database | **PostgreSQL** via Neon (serverless Postgres) |
| ORM | **Entity Framework Core 10** with EF Migrations |
| Authentication | **ASP.NET Core Identity** + **JWT Bearer tokens** |
| Social Login | **Google OAuth** (via Google.Apis.Auth) |
| File Storage | **Cloudinary** (CloudinaryDotNet SDK) |
| Email | **MailKit + MimeKit** via Gmail SMTP |
| PDF Parsing | **PdfPig** |
| Document Processing | **DocumentFormat.OpenXml** |
| Input Validation | **FluentValidation** |
| Object Mapping | **AutoMapper** |
| Background Jobs | **Hangfire Core** + custom `IHostedService` workers |
| API Docs | **Swagger / OpenAPI** (Swashbuckle) |
| Architecture Pattern | Clean Architecture (Domain / Application / Infrastructure / Presentation) |

---

## Project Structure Walkthrough

```
Requra.Domain/
  Entities/          → All domain models (Project, Requirement, UserStory, MeetingSession, Recording, etc.)
  Enums/             → 30+ enums covering every status and type in the system
  Specifications/    → Query specification base classes

Requra.Application/
  Interfaces/        → Service and repository contracts
  DTOs/              → Data transfer objects for every feature area
  DTOValidations/    → FluentValidation validators
  Mappings/          → AutoMapper profiles
  Response/          → Shared response wrapper types

Requra.Infrastructure/
  Data/              → EF DbContext (RequraDbContext)
  Configurations/    → Entity type configurations
  Migrations/        → EF Core database migrations
  Repositories/      → Data access implementations
  UnitOfWork/        → UoW pattern wrapping the DbContext
  Services/          → All business logic (Auth, Project, Meeting, Recording, AI, etc.)
  ExternalServices/  → Cloudinary, Email, Google Auth, AI HTTP client, JWT service
  Workers/           → Background workers (AnalysisRunWorker)
  Initializers/      → Database seeder and startup initializer
  MiddleWares/       → Custom middleware
  Helpers/           → Utility classes
  Http/              → File downloader HTTP client

Requra.Presentation/
  Controllers/       → REST API controllers (Auth, Project, Meeting, Document, Recording, AIRuns, Profile)
  Program.cs         → App bootstrap, DI registration, middleware pipeline
  appsettings.json   → Configuration (DB, JWT, Cloudinary, Google, Email)
```

---

## What's Coming Next

The project is still being actively developed. Here's what we're planning to improve:

- **Another Type of OAuth** — user can authenticate using GitHub. or Facebook in addition to Google.

- **Full AI pipeline integration** — Right now the AI client calls an external Python service. We want to tighten that integration, add proper retry logic, circuit breakers, and better error recovery instead of a simple polling loop.

- **Hangfire job dashboard** — Hangfire is already a dependency. We plan to expose a proper background job dashboard and migrate long-running tasks like recording finalization and AI analysis into managed Hangfire jobs with persistence.

- **Jira integration** — User stories have a `JiraTicket` field already. The plan is to add a full OAuth-based Jira integration so teams can sync approved requirements and user stories directly into their Jira projects.


- **Requirement conflict detection** — Use the AI pipeline to flag when two requirements contradict each other or overlap significantly.


- **Audit log** — Track all changes made to requirements, approvals, and project settings so teams have a full history of who changed what and when.


- **Performance improvements** — Add response caching for read-heavy endpoints, query optimization for the analysis result reads, and proper indexes on frequently filtered columns.

---

## Team

This is a graduation project. The backend was designed and built by the team using Clean Architecture principles to keep the codebase maintainable and scalable as features are added.
