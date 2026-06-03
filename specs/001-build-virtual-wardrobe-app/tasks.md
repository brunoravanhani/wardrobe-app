# Tasks: Virtual Wardrobe and Wishlist Management

**Input**: Design documents from `/specs/001-build-virtual-wardrobe-app/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md, contracts/wardrobe-api.openapi.yaml

**Tests**: Automated tests are REQUIRED by constitution. Include unit, integration, contract, and end-to-end coverage with fail-before-pass execution for every affected story.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Scaffold the planned monorepo structure and baseline toolchain.

- [X] T001 Scaffold the backend solution and project references in backend/VirtualWardrobe.sln
- [X] T002 Scaffold the React SPA workspace and scripts in frontend/package.json
- [X] T003 [P] Configure backend linting and static analysis rules in backend/Directory.Build.props
- [X] T004 [P] Configure frontend linting, formatting, and Tailwind entry setup in frontend/eslint.config.js
- [X] T005 [P] Add backend environment placeholder configuration in backend/.env.example
- [X] T006 [P] Add frontend environment placeholder configuration in frontend/.env.example

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared architecture that blocks all user stories until complete.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T007 Define shared domain primitives, IDs, and base entity behavior in backend/src/VirtualWardrobe.Domain/Common/Entity.cs
- [X] T008 [P] Configure the EF Core database context and set registrations in backend/src/VirtualWardrobe.Infrastructure/Persistence/VirtualWardrobeDbContext.cs
- [X] T009 Create the initial PostgreSQL migration for users and shared media tables in backend/src/VirtualWardrobe.Infrastructure/Persistence/Migrations/InitialCreate.cs
- [X] T010 [P] Implement Google identity verification against OAuth tokens in backend/src/VirtualWardrobe.Infrastructure/Auth/GoogleTokenVerifier.cs
- [X] T011 [P] Implement API session issuance and authenticated user resolution in backend/src/VirtualWardrobe.Application/Auth/AuthSessionService.cs
- [X] T012 [P] Implement private AWS S3 presigned upload and view URL generation in backend/src/VirtualWardrobe.Infrastructure/Storage/S3PresignedUrlService.cs
- [X] T013 Configure API dependency injection, auth, exception handling, and structured logging in backend/src/VirtualWardrobe.Api/Program.cs
- [X] T014 [P] Set up frontend app shell, routing, auth bootstrap, and pt-BR providers in frontend/src/app/App.tsx
- [X] T015 Create reusable component inventory and reuse guidance for forms, cards, and uploads in frontend/src/components/README.md

**Checkpoint**: Foundation ready. User stories can start after this phase, with US3 additionally depending on both US1 and US2.

---

## Phase 3: User Story 1 Backend - Organize Wardrobe Catalog (Priority: P1) 🎯 MVP

**Goal**: Deliver the full US1 backend using Repository Pattern, Result Pattern, rich domain entities, and a minimal `Program.cs` composition root style.

**Independent Test**: Execute backend unit, integration, and contract tests for wardrobe and media flows while confirming owner-only access and explicit failure results for invalid requests.

### Tests for User Story 1 Backend (REQUIRED) ⚠️

- [X] T016 [P] [US1-BE] Add unit tests for wardrobe and media validation rules in backend/tests/VirtualWardrobe.UnitTests/Wardrobe/WardrobeValidationTests.cs
- [X] T017 [P] [US1-BE] Add integration tests for wardrobe CRUD, category filtering, and owner isolation in backend/tests/VirtualWardrobe.IntegrationTests/Wardrobe/WardrobeItemTests.cs
- [X] T018 [P] [US1-BE] Add contract tests for auth exchange, wardrobe CRUD, and media presign endpoints in backend/tests/VirtualWardrobe.ContractTests/Wardrobe/WardrobeContractTests.cs
- [X] T059 [P] [US1-BE] Add unit tests for Result pattern success/failure flows and domain error mapping in backend/tests/VirtualWardrobe.UnitTests/Common/ResultPatternTests.cs
- [X] T060 [P] [US1-BE] Add integration tests for repository-backed handlers (no direct DbContext usage in application layer) in backend/tests/VirtualWardrobe.IntegrationTests/Architecture/RepositoryPatternTests.cs
- [X] T064 [P] [US1-BE] Add negative tests for invalid image format and files above 10 MB in wardrobe media endpoints in backend/tests/VirtualWardrobe.ContractTests/Wardrobe/WardrobeMediaValidationContractTests.cs

### Implementation for User Story 1 Backend

- [X] T020 [P] [US1-BE] Create the media asset aggregate with rich behaviors and ownership invariants in backend/src/VirtualWardrobe.Domain/Media/MediaAsset.cs
- [X] T021 [P] [US1-BE] Create the wardrobe item aggregate with rich behaviors and category constraints in backend/src/VirtualWardrobe.Domain/Wardrobe/WardrobeItem.cs
- [X] T061 [US1-BE] Introduce Result base types and domain/application error contracts in backend/src/VirtualWardrobe.Application/Common/Result.cs
- [X] T062 [US1-BE] Extract API configuration into extension methods and keep Program.cs as thin composition root in backend/src/VirtualWardrobe.Api/Program.cs
- [X] T063 [US1-BE] Define repository interfaces for wardrobe and media in backend/src/VirtualWardrobe.Application/Wardrobe/Interfaces.cs
- [X] T022 [US1-BE] Implement wardrobe create and update commands with validators and Result returns in backend/src/VirtualWardrobe.Application/Wardrobe/CreateWardrobeItemCommand.cs
- [X] T023 [US1-BE] Map wardrobe and media persistence rules and repository implementations in backend/src/VirtualWardrobe.Infrastructure/Persistence/Configurations/WardrobeItemConfiguration.cs
- [X] T024 [US1-BE] Implement private media upload/view endpoints with Result-aware error handling in backend/src/VirtualWardrobe.Api/Controllers/MediaController.cs
- [X] T025 [US1-BE] Implement wardrobe CRUD and category filter endpoints using repository-backed handlers in backend/src/VirtualWardrobe.Api/Controllers/WardrobeItemsController.cs

**Checkpoint**: US1 backend is independently functional and architecture guardrails are enforced.

---

## Phase 4: User Story 1 Frontend - Organize Wardrobe Catalog (Priority: P1)

**Goal**: Deliver the US1 frontend experience in pt-BR using the US1 backend contracts.

**Independent Test**: Sign in, create wardrobe items across categories with images and details, edit them, filter by category, and confirm expected validation/error messages in pt-BR.

### Tests for User Story 1 Frontend (REQUIRED) ⚠️

- [ ] T019 [P] [US1-FE] Add end-to-end coverage for wardrobe creation, edit, and filtering in frontend/tests/e2e/wardrobe.spec.ts

### Implementation for User Story 1 Frontend

- [ ] T026 [P] [US1-FE] Implement the wardrobe API client and DTO mapping in frontend/src/services/wardrobeApi.ts
- [ ] T027 [US1-FE] Implement the wardrobe page with category tabs and item list state in frontend/src/features/wardrobe/WardrobePage.tsx
- [ ] T028 [US1-FE] Implement the wardrobe item form with image upload and pt-BR validation messages in frontend/src/features/wardrobe/components/WardrobeItemForm.tsx
- [ ] T029 [US1-FE] Document wardrobe-specific component reuse decisions in frontend/src/features/wardrobe/README.md

**Checkpoint**: User Story 1 is independently functional and represents the MVP.

---

## Phase 5: User Story 2 Backend - Track Wishlist Intent (Priority: P2)

**Goal**: Deliver the full US2 backend with repository-backed access and Result-based flow control.

**Independent Test**: Execute backend tests for wishlist CRUD, history filtering defaults, duplicate-link rejection, and upload validation boundaries.

### Tests for User Story 2 Backend (REQUIRED) ⚠️

- [ ] T030 [P] [US2-BE] Add unit tests for wishlist target-price and external-link validation in backend/tests/VirtualWardrobe.UnitTests/Wishlist/WishlistValidationTests.cs
- [ ] T031 [P] [US2-BE] Add integration tests for wishlist CRUD, history filtering defaults, and duplicate-link rejection in backend/tests/VirtualWardrobe.IntegrationTests/Wishlist/WishlistItemTests.cs
- [ ] T032 [P] [US2-BE] Add contract tests for wishlist CRUD endpoints in backend/tests/VirtualWardrobe.ContractTests/Wishlist/WishlistContractTests.cs
- [ ] T065 [P] [US2-BE] Add negative tests for invalid image format and files above 10 MB in wishlist inspiration endpoints in backend/tests/VirtualWardrobe.ContractTests/Wishlist/WishlistMediaValidationContractTests.cs

### Implementation for User Story 2 Backend

- [ ] T034 [P] [US2-BE] Create the wishlist item aggregate with rich behaviors and status defaults in backend/src/VirtualWardrobe.Domain/Wishlist/WishlistItem.cs
- [ ] T035 [P] [US2-BE] Create the wishlist external link entity with duplicate protection rules in backend/src/VirtualWardrobe.Domain/Wishlist/WishlistExternalLink.cs
- [ ] T066 [US2-BE] Define repository interfaces for wishlist aggregate and queries in backend/src/VirtualWardrobe.Application/Wishlist/Interfaces.cs
- [ ] T036 [US2-BE] Implement wishlist create and update commands with validators and Result returns in backend/src/VirtualWardrobe.Application/Wishlist/CreateWishlistItemCommand.cs
- [ ] T037 [US2-BE] Map wishlist persistence, link ownership, and default active filtering including repository implementations in backend/src/VirtualWardrobe.Infrastructure/Persistence/Configurations/WishlistItemConfiguration.cs
- [ ] T038 [US2-BE] Implement wishlist CRUD and history filter endpoints using repository-backed handlers in backend/src/VirtualWardrobe.Api/Controllers/WishlistItemsController.cs

**Checkpoint**: US2 backend is independently functional and stable.

---

## Phase 6: User Story 2 Frontend - Track Wishlist Intent (Priority: P2)

**Goal**: Deliver the US2 frontend experience with active/history views and robust form validation in pt-BR.

**Independent Test**: Sign in, create and edit wishlist entries with target price, links, and inspiration image, then verify active/history rendering and clear validation messaging.

### Tests for User Story 2 Frontend (REQUIRED) ⚠️

- [ ] T033 [P] [US2-FE] Add end-to-end coverage for wishlist create and edit flows in frontend/tests/e2e/wishlist.spec.ts
- [ ] T067 [P] [US2-FE] Add end-to-end coverage for unsaved form draft protection and recovery in frontend/tests/e2e/wishlist-unsaved-draft.spec.ts

### Implementation for User Story 2 Frontend

- [ ] T039 [P] [US2-FE] Implement the wishlist API client and DTO mapping in frontend/src/services/wishlistApi.ts
- [ ] T040 [US2-FE] Implement the wishlist page with active and history views in frontend/src/features/wishlist/WishlistPage.tsx
- [ ] T041 [US2-FE] Implement the wishlist form with target price, links, and inspiration upload in frontend/src/features/wishlist/components/WishlistItemForm.tsx
- [ ] T068 [US2-FE] Implement unsaved draft persistence and recovery for wardrobe and wishlist forms in frontend/src/app/providers/DraftStateProvider.tsx
- [ ] T042 [US2-FE] Document wishlist-specific component reuse decisions in frontend/src/features/wishlist/README.md

**Checkpoint**: User Stories 1 and 2 are independently functional and can be demoed separately.

---

## Phase 7: User Story 3 Backend - Convert Purchases to Wardrobe (Priority: P3)

**Goal**: Deliver US3 backend conversion rules with idempotent repository-backed behavior and Result-based error paths.

**Independent Test**: Starting from an existing wishlist item, mark as purchased, convert to wardrobe, and verify idempotency and history retention through backend tests.

### Tests for User Story 3 Backend (REQUIRED) ⚠️

- [ ] T043 [P] [US3-BE] Add unit tests for purchase, conversion, and idempotency rules in backend/tests/VirtualWardrobe.UnitTests/Wishlist/WishlistConversionTests.cs
- [ ] T044 [P] [US3-BE] Add integration tests for purchase history retention and wishlist-to-wardrobe conversion in backend/tests/VirtualWardrobe.IntegrationTests/Wishlist/WishlistConversionTests.cs
- [ ] T045 [P] [US3-BE] Add contract tests for purchase and conversion endpoints in backend/tests/VirtualWardrobe.ContractTests/Wishlist/WishlistConversionContractTests.cs

### Implementation for User Story 3 Backend

- [ ] T047 [US3-BE] Implement purchase and conversion commands with missing-field validation and Result returns in backend/src/VirtualWardrobe.Application/Wishlist/ConvertWishlistItemCommand.cs
- [ ] T048 [US3-BE] Extend wishlist endpoints for purchase, history retention, and conversion in backend/src/VirtualWardrobe.Api/Controllers/WishlistItemsController.cs
- [ ] T049 [US3-BE] Extend wardrobe persistence to link converted wishlist items idempotently in backend/src/VirtualWardrobe.Infrastructure/Persistence/Configurations/WardrobeItemConfiguration.cs

**Checkpoint**: US3 backend is independently functional and testable.

---

## Phase 8: User Story 3 Frontend - Convert Purchases to Wardrobe (Priority: P3)

**Goal**: Deliver US3 frontend conversion flows on top of stable backend contracts.

**Independent Test**: Mark wishlist item as purchased, convert with missing-field completion, and verify history and wardrobe consistency on UI.

### Tests for User Story 3 Frontend (REQUIRED) ⚠️

- [ ] T046 [P] [US3-FE] Add end-to-end coverage for purchase and convert flows in frontend/tests/e2e/wishlist-conversion.spec.ts

### Implementation for User Story 3 Frontend

- [ ] T050 [P] [US3-FE] Extend the wishlist API client for purchase and conversion actions in frontend/src/services/wishlistApi.ts
- [ ] T051 [US3-FE] Implement purchased-history actions and conversion entry points in frontend/src/features/wishlist/WishlistPage.tsx
- [ ] T052 [US3-FE] Implement the missing-field conversion dialog in frontend/src/features/wishlist/components/ConvertWishlistItemDialog.tsx
- [ ] T053 [US3-FE] Document conversion-flow reuse decisions in frontend/src/features/wishlist/README.md

**Checkpoint**: All user stories are functional, and conversion behavior is independently testable.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Finalize quality, accessibility, performance, security, and documentation across all stories.

- [ ] T054 [P] Add structured logging and metrics for auth, S3 presign flows, and conversion outcomes in backend/src/VirtualWardrobe.Api/Observability/TelemetryConfig.cs
- [ ] T055 [P] Add accessibility regression coverage for wardrobe and wishlist flows in frontend/tests/e2e/accessibility.spec.ts
- [ ] T056 [P] Add primary-journey performance verification for p95 budgets in frontend/tests/e2e/performance.spec.ts
- [ ] T057 [P] Add CI secret and configuration audit checks in .github/workflows/ci.yml
- [ ] T058 Run quickstart validation and capture final execution notes in specs/001-build-virtual-wardrobe-app/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** has no dependencies and starts immediately.
- **Phase 2: Foundational** depends on Phase 1 and blocks every user story.
- **Phase 3: US1 Backend** depends on Phase 2 and establishes architectural guardrails (Repository Pattern, Result Pattern, rich entities, thin Program.cs).
- **Phase 4: US1 Frontend** depends on Phase 3.
- **Phase 5: US2 Backend** depends on Phase 2 and should start after Phase 3 architectural tasks (`T061`, `T062`, `T063`) are complete.
- **Phase 6: US2 Frontend** depends on Phase 5.
- **Phase 7: US3 Backend** depends on Phases 3 and 5.
- **Phase 8: US3 Frontend** depends on Phase 7.
- **Phase 9: Polish** depends on Phases 4, 6, and 8.

### User Story Dependencies

- **US1 (P1)**: Backend first, then frontend.
- **US2 (P2)**: Backend first, then frontend. Independent from US1 behavior, but shares architectural standards introduced in Phase 3.
- **US3 (P3)**: Depends on US1 and US2 backend behavior, then frontend completion.

### Within Each User Story

- Tests must be written and observed failing before implementation work begins.
- Rich domain entities with invariants and behaviors come before application handlers.
- Repository interfaces come before handler implementation; repository implementations come before endpoint wiring.
- Application handlers must return Result types and avoid direct infrastructure concerns.
- API `Program.cs` remains a thin composition root that only calls extracted configuration extensions.
- Backend support comes before frontend integration and end-to-end completion.
- Reuse documentation must be completed before introducing story-specific net-new components.

## Parallel Opportunities

- Setup tasks `T003` through `T006` can run in parallel after scaffolding starts.
- Foundational tasks `T010`, `T011`, `T012`, and `T014` can run in parallel once `T007` is in place.
- In US1 backend, tests `T016`, `T017`, `T018`, `T059`, and `T060` can run in parallel; entities `T020` and `T021` can run in parallel.
- In US1 frontend, tasks `T026` and `T027` can run in parallel after backend contract stabilization.
- In US2 backend, tests `T030`, `T031`, `T032`, and `T065` can run in parallel; entities `T034` and `T035` can run in parallel.
- In US2 frontend, tasks `T039`, `T040`, and `T067` can run in parallel after API contract stabilization.
- In US3 backend, tests `T043`, `T044`, and `T045` can run in parallel.
- In US3 frontend, tasks `T050` and `T052` can run in parallel after conversion contract stabilization.
- Polish tasks `T054` through `T057` can run in parallel before final quickstart validation.

## Parallel Example: User Story 1 (Backend -> Frontend)

```text
T016 [US1-BE] backend/tests/VirtualWardrobe.UnitTests/Wardrobe/WardrobeValidationTests.cs
T017 [US1-BE] backend/tests/VirtualWardrobe.IntegrationTests/Wardrobe/WardrobeItemTests.cs
T018 [US1-BE] backend/tests/VirtualWardrobe.ContractTests/Wardrobe/WardrobeContractTests.cs
T059 [US1-BE] backend/tests/VirtualWardrobe.UnitTests/Common/ResultPatternTests.cs
T060 [US1-BE] backend/tests/VirtualWardrobe.IntegrationTests/Architecture/RepositoryPatternTests.cs

T020 [US1-BE] backend/src/VirtualWardrobe.Domain/Media/MediaAsset.cs
T021 [US1-BE] backend/src/VirtualWardrobe.Domain/Wardrobe/WardrobeItem.cs

T019 [US1-FE] frontend/tests/e2e/wardrobe.spec.ts
T026 [US1-FE] frontend/src/services/wardrobeApi.ts
```

## Parallel Example: User Story 2 (Backend -> Frontend)

```text
T030 [US2-BE] backend/tests/VirtualWardrobe.UnitTests/Wishlist/WishlistValidationTests.cs
T031 [US2-BE] backend/tests/VirtualWardrobe.IntegrationTests/Wishlist/WishlistItemTests.cs
T032 [US2-BE] backend/tests/VirtualWardrobe.ContractTests/Wishlist/WishlistContractTests.cs
T065 [US2-BE] backend/tests/VirtualWardrobe.ContractTests/Wishlist/WishlistMediaValidationContractTests.cs

T034 [US2-BE] backend/src/VirtualWardrobe.Domain/Wishlist/WishlistItem.cs
T035 [US2-BE] backend/src/VirtualWardrobe.Domain/Wishlist/WishlistExternalLink.cs

T033 [US2-FE] frontend/tests/e2e/wishlist.spec.ts
T039 [US2-FE] frontend/src/services/wishlistApi.ts
```

## Parallel Example: User Story 3 (Backend -> Frontend)

```text
T043 [US3-BE] backend/tests/VirtualWardrobe.UnitTests/Wishlist/WishlistConversionTests.cs
T044 [US3-BE] backend/tests/VirtualWardrobe.IntegrationTests/Wishlist/WishlistConversionTests.cs
T045 [US3-BE] backend/tests/VirtualWardrobe.ContractTests/Wishlist/WishlistConversionContractTests.cs

T047 [US3-BE] backend/src/VirtualWardrobe.Application/Wishlist/ConvertWishlistItemCommand.cs
T049 [US3-BE] backend/src/VirtualWardrobe.Infrastructure/Persistence/Configurations/WardrobeItemConfiguration.cs

T046 [US3-FE] frontend/tests/e2e/wishlist-conversion.spec.ts
T052 [US3-FE] frontend/src/features/wishlist/components/ConvertWishlistItemDialog.tsx
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: US1 Backend (including architecture guardrails).
4. Complete Phase 4: US1 Frontend.
5. Validate wardrobe CRUD, category filtering, and private media flow independently.
6. Demo or deploy the MVP before expanding scope.

### Incremental Delivery

1. Setup + Foundational create the shared auth, persistence, S3, and UI shell.
2. Establish backend architecture guardrails in US1 backend.
3. Deliver US1 backend then frontend as first usable increment.
4. Deliver US2 backend then frontend while keeping independent testability.
5. Deliver US3 backend then frontend after US1/US2 stabilization.
6. Finish with cross-cutting polish and final quickstart validation.

### Parallel Team Strategy

1. One group completes Setup and Foundational work together.
2. After Foundation, backend team starts Phase 3 while frontend team prepares shared UI primitives and test harnesses.
3. Frontend implementation for each user story starts only after its backend phase reaches contract stability.
4. Polish tasks split across observability, accessibility, performance, and CI hardening.

## Notes

- Every task follows the required checklist format with task ID, optional `[P]`, optional story label, action, and exact file path.
- User-facing copy stays in pt-BR, while code artifacts remain in English.
- Secrets must remain environment-backed; example files use placeholders only.
- Stop at each checkpoint and verify the story independently before proceeding.