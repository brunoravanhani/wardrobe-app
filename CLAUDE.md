# Virtual Wardrobe — Claude Code Guide

Active feature plan: `specs/001-build-virtual-wardrobe-app/plan.md`

## Project Overview

Portuguese-Brazil web application for personal wardrobe and wishlist management. React SPA + layered .NET 8 Web API + PostgreSQL, with Google OAuth login and private AWS S3 image storage.

**Status**: All phases (1–9) complete. US1 (wardrobe CRUD), US2 (wishlist management), US3 (purchased-to-wardrobe conversion), and Polish (logging, metrics, CI, accessibility) are delivered.

## Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 18+, TypeScript, Tailwind CSS, React Router, Vite |
| Backend | .NET 8 / C# 12, ASP.NET Core Web API, EF Core, Npgsql |
| Database | PostgreSQL 15+ |
| Storage | AWS S3 (private bucket, presigned URLs) |
| Auth | Google OAuth/OIDC + JWT |

## Architecture Rules

- **Repository Pattern** — application handlers depend on repository interfaces; infrastructure implements them.
- **Result Pattern** — expected success/failure flows use Result, not exceptions.
- **Rich domain entities** — aggregates encapsulate invariants; no anemic data bags.
- **Thin `Program.cs`** — only orchestrates extension methods/modules.
- **Backend-first delivery** — for each user story, backend ships before frontend.
- **Migration-first schema** — every DB change goes through EF Core migrations with timestamped names (`20260603_Name`).

## Running the Project

```bash
# Backend
cd backend
dotnet restore
dotnet ef database update --project src/VirtualWardrobe.Infrastructure --startup-project src/VirtualWardrobe.Api
dotnet run --project src/VirtualWardrobe.Api

# Frontend
cd frontend
pnpm install
pnpm dev
```

### Environment Variables

**`backend/appsettings.Development.json` or `.env`**:
- `ConnectionStrings__Default` — PostgreSQL connection string
- `Auth__Google__ClientId` / `Auth__Google__ClientSecret`
- `Jwt__SigningKey`
- `AWS__Region` / `AWS__S3__BucketName` / `AWS__AccessKeyId` / `AWS__SecretAccessKey`

**`frontend/.env.local`**:
- `VITE_API_BASE_URL=http://localhost:5000`
- `VITE_GOOGLE_CLIENT_ID`
- `VITE_DEFAULT_LOCALE=pt-BR`

## Running Tests

```bash
# Backend
cd backend
dotnet test tests/VirtualWardrobe.UnitTests
dotnet test tests/VirtualWardrobe.IntegrationTests
dotnet test tests/VirtualWardrobe.ContractTests

# Frontend
cd frontend
pnpm test
pnpm test:e2e
```

## Core Principles

### Code Quality and Reuse
All production code must pass linting and static analysis. Before creating a new component, search for an existing reusable one and extend/compose it when feasible.

### Testing Gate
Every feature must include automated tests (unit, integration, e2e/contract). New tests must fail before implementation and pass after.

### UX Consistency
All user-facing text in **pt-BR**. Code, classes, and methods in English. Consistent interaction patterns across screens; accessible forms.

### Performance Budgets
- p95 list render readiness ≤ 2s
- p95 create/edit/save confirmation ≤ 3s
- p95 conversion update ≤ 3s

### Secret Management
No secrets in source files. Use environment-backed configuration only. Never commit `.env` files. Provide `.env.example` with placeholders.

### Observability
Structured JSON logs to stdout. Key events: auth exchange, S3 presign, wishlist conversion. Metrics via `System.Diagnostics.Metrics` (meter: `VirtualWardrobe.Api`).

## Key Constraints

- Fixed predefined categories in v1 (no user-defined categories)
- Images: JPG/PNG/WebP, max 10 MB
- S3 objects remain private (Block Public Access enabled); all media access via owner-only presigned URLs
- Migrations: roll-forward is the default production remediation path

## Project Structure

```
specs/001-build-virtual-wardrobe-app/   # Feature docs (plan, spec, tasks, data model, contracts)
backend/
  src/
    VirtualWardrobe.Api/
    VirtualWardrobe.Application/
    VirtualWardrobe.Domain/
    VirtualWardrobe.Infrastructure/
  tests/
    VirtualWardrobe.UnitTests/
    VirtualWardrobe.IntegrationTests/
    VirtualWardrobe.ContractTests/
frontend/
  src/
    app/
    features/           # auth/, wardrobe/, wishlist/
    components/
    services/
    i18n/
  tests/
    unit/
    e2e/
```
