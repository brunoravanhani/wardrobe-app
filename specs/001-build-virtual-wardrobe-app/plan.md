# Implementation Plan: Virtual Wardrobe and Wishlist Management

**Branch**: `001-build-virtual-wardrobe-app` | **Date**: 2026-06-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-build-virtual-wardrobe-app/spec.md`

## Summary

Build a Portuguese-Brazil web product for personal wardrobe and wishlist management using a React SPA and a layered .NET Web API backed by PostgreSQL. The solution includes Google login, fixed v1 categories, private-by-default media handling, single target price for wishlist, and purchased-history conversion into wardrobe with measurable UX/performance gates and full automated test coverage across unit, integration, and end-to-end/contract layers.

Implementation sequencing is backend-first then frontend for each user story from US1 onward, and backend architecture enforces Repository Pattern, Result Pattern, rich domain entities, and a thin Program.cs composition root.

## Technical Context

**Language/Version**: TypeScript (frontend, React 18+), C# 12 on .NET 8 (backend), SQL (PostgreSQL 15+)

**Primary Dependencies**: React SPA stack (React, React Router, Tailwind CSS, query/state library), ASP.NET Core Web API, Entity Framework Core, Npgsql provider, Google OAuth/OIDC integration, AWS S3 SDK for .NET

**Storage**: PostgreSQL for relational domain data; private AWS S3 bucket for images (wardrobe body image, care-tag image, wishlist inspiration image) with backend-issued presigned upload/view URLs after ownership checks

**Testing**: Frontend unit/component tests + end-to-end browser tests; backend unit + integration tests (database and auth boundaries); API contract tests derived from OpenAPI

**Target Platform**: Modern desktop/mobile browsers for SPA; Linux container or equivalent hosted runtime for API

**Project Type**: Web application (SPA frontend + layered REST API backend)

**Performance Goals**: p95 list render readiness <= 2s; p95 create/edit/save confirmation <= 3s; p95 conversion consistency update <= 3s

**Constraints**: UI text in pt-BR; code/classes/methods in English; fixed predefined categories in v1 only; images limited to JPG/PNG/WebP and <= 10 MB; S3 objects remain private with Block Public Access enabled; media view access limited to authenticated owner

**Scale/Scope**: Initial release for low-to-medium usage (up to ~5k monthly active users, up to ~100k item records total) with clear migration path for category and scale expansion

## Architectural Decisions

- Repository Pattern is mandatory for application-layer data access. Application handlers depend on repository interfaces; infrastructure provides implementations.
- Result Pattern is mandatory for expected success/failure flows in domain and application operations.
- Rich domain entities are required: aggregates encapsulate invariants and behavior, not only data bags.
- Program.cs must remain a thin composition root that only orchestrates extension methods/modules.
- From US1 onward, each user story is delivered in two phases: backend first, frontend second.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Code Quality Gate: PASS. Enforce TypeScript and C# lint/static analysis; keep business rules in domain/application services to avoid duplication across controllers/components.
- Testing Gate: PASS. Define fail-first then pass criteria for unit, integration, e2e, and contract tests for every changed flow.
- UX Consistency Gate: PASS. Maintain consistent pt-BR terminology across wardrobe/wishlist; define keyboard/accessibility checks for forms, upload controls, and state transitions.
- Reuse Gate: PASS. Reuse shared form, card, and validation primitives before introducing new components; reuse backend cross-cutting abstractions (result/error handling, validation pipeline).
- Architecture Gate: PASS. Repository and Result patterns are mandatory, rich domain entities are required, and Program.cs remains thin by convention and review checks.
- Performance Gate: PASS. Enforce p95 budgets from spec with synthetic journey timing in CI and targeted profile captures for list and conversion flows.
- Secret Management Gate: PASS. Google credentials, DB connection, AWS access settings, and bucket configuration will be environment-backed only; no hardcoded sensitive values.
- Observability Gate: PASS. Define structured logs for auth, S3 upload/view URL issuance, and conversion flows plus metrics for latency/error rate and conversion success.

## Project Structure

### Documentation (this feature)

```text
specs/001-build-virtual-wardrobe-app/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── wardrobe-api.openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── VirtualWardrobe.Api/
│   ├── VirtualWardrobe.Application/
│   ├── VirtualWardrobe.Domain/
│   └── VirtualWardrobe.Infrastructure/
└── tests/
    ├── VirtualWardrobe.UnitTests/
    ├── VirtualWardrobe.IntegrationTests/
    └── VirtualWardrobe.ContractTests/

frontend/
├── src/
│   ├── app/
│   ├── features/
│   │   ├── auth/
│   │   ├── wardrobe/
│   │   └── wishlist/
│   ├── components/
│   ├── services/
│   └── i18n/
└── tests/
    ├── unit/
    └── e2e/
```

**Structure Decision**: Use a monorepo with separate `frontend` and `backend` applications to preserve SPA/API autonomy while sharing a single specification and delivery workflow.

## Phase 0 Research Summary

Research decisions are recorded in [research.md](./research.md) and resolve all technical uncertainties from this plan.

## Phase 1 Design Outputs

- Data model documented in [data-model.md](./data-model.md)
- API contracts documented in [contracts/wardrobe-api.openapi.yaml](./contracts/wardrobe-api.openapi.yaml)
- Developer setup and execution flow documented in [quickstart.md](./quickstart.md)

## Delivery Sequencing

- US1: backend phase first, then frontend phase.
- US2: backend phase first, then frontend phase.
- US3: backend phase first, then frontend phase.
- Polish starts after frontend completion for all in-scope user stories.

## Post-Design Constitution Check

- Code Quality and Reuse: PASS. Responsibilities are split by frontend feature modules and backend layers, with explicit reuse-first guidance.
- Testing: PASS. Unit/integration/e2e/contract coverage mapped to user stories and key risk points.
- UX Consistency: PASS. pt-BR UX and accessibility checks included in quickstart validation and contract-level error semantics.
- Performance: PASS. Measurable p95 thresholds retained; validation approach defined in quickstart.
- Secret Management: PASS. Environment-backed secret contract defined for backend and frontend runtime config, including AWS bucket and credentials.
- Observability and Safe Evolution: PASS. Auth, S3 media access, and conversion telemetry planned with non-breaking contract versioning approach.

## Complexity Tracking

No constitution violations or complexity exemptions are required at planning time.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |

