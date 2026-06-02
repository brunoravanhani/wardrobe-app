# Tasks: Virtual Wardrobe and Wishlist Management

**Input**: Design documents from `/specs/001-build-virtual-wardrobe-app/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md, contracts/wardrobe-api.openapi.yaml

**Tests**: Automated tests are REQUIRED by constitution. Include unit, integration, contract, and end-to-end coverage with fail-before-pass execution for every affected story.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Scaffold the planned monorepo structure and baseline toolchain.

- [ ] T001 Scaffold the backend solution and project references in backend/VirtualWardrobe.sln
- [ ] T002 Scaffold the React SPA workspace and scripts in frontend/package.json
- [ ] T003 [P] Configure backend linting and static analysis rules in backend/Directory.Build.props
- [ ] T004 [P] Configure frontend linting, formatting, and Tailwind entry setup in frontend/eslint.config.js
- [ ] T005 [P] Add backend environment placeholder configuration in backend/.env.example
- [ ] T006 [P] Add frontend environment placeholder configuration in frontend/.env.example

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared architecture that blocks all user stories until complete.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T007 Define shared domain primitives, IDs, and base entity behavior in backend/src/VirtualWardrobe.Domain/Common/Entity.cs
- [ ] T008 [P] Configure the EF Core database context and set registrations in backend/src/VirtualWardrobe.Infrastructure/Persistence/VirtualWardrobeDbContext.cs
- [ ] T009 Create the initial PostgreSQL migration for users and shared media tables in backend/src/VirtualWardrobe.Infrastructure/Persistence/Migrations/InitialCreate.cs
- [ ] T010 [P] Implement Google identity verification against OAuth tokens in backend/src/VirtualWardrobe.Infrastructure/Auth/GoogleTokenVerifier.cs
- [ ] T011 [P] Implement API session issuance and authenticated user resolution in backend/src/VirtualWardrobe.Application/Auth/AuthSessionService.cs
- [ ] T012 [P] Implement private AWS S3 presigned upload and view URL generation in backend/src/VirtualWardrobe.Infrastructure/Storage/S3PresignedUrlService.cs
- [ ] T013 Configure API dependency injection, auth, exception handling, and structured logging in backend/src/VirtualWardrobe.Api/Program.cs
- [ ] T014 [P] Set up frontend app shell, routing, auth bootstrap, and pt-BR providers in frontend/src/app/App.tsx
- [ ] T015 Create reusable component inventory and reuse guidance for forms, cards, and uploads in frontend/src/components/README.md

**Checkpoint**: Foundation ready. User stories can start after this phase, with US3 additionally depending on both US1 and US2.

---

## Phase 3: User Story 1 - Organize Wardrobe Catalog (Priority: P1) 🎯 MVP

**Goal**: Let an authenticated user create, edit, view, filter, and delete wardrobe items by fixed category with private media attachments.

**Independent Test**: Sign in, create wardrobe items across categories with images and details, edit them, filter by category, and confirm only the owner can access the images and records.

### Tests for User Story 1 (REQUIRED) ⚠️

- [ ] T016 [P] [US1] Add unit tests for wardrobe and media validation rules in backend/tests/VirtualWardrobe.UnitTests/Wardrobe/WardrobeValidationTests.cs
- [ ] T017 [P] [US1] Add integration tests for wardrobe CRUD, category filtering, and owner isolation in backend/tests/VirtualWardrobe.IntegrationTests/Wardrobe/WardrobeItemTests.cs
- [ ] T018 [P] [US1] Add contract tests for auth exchange, wardrobe CRUD, and media presign endpoints in backend/tests/VirtualWardrobe.ContractTests/Wardrobe/WardrobeContractTests.cs
- [ ] T019 [P] [US1] Add end-to-end coverage for wardrobe creation, edit, and filtering in frontend/tests/e2e/wardrobe.spec.ts

### Implementation for User Story 1

- [ ] T020 [P] [US1] Create the media asset aggregate and ownership rules in backend/src/VirtualWardrobe.Domain/Media/MediaAsset.cs
- [ ] T021 [P] [US1] Create the wardrobe item aggregate and category constraints in backend/src/VirtualWardrobe.Domain/Wardrobe/WardrobeItem.cs
- [ ] T022 [US1] Implement wardrobe create and update commands with validators in backend/src/VirtualWardrobe.Application/Wardrobe/CreateWardrobeItemCommand.cs
- [ ] T023 [US1] Map wardrobe and media persistence rules in backend/src/VirtualWardrobe.Infrastructure/Persistence/Configurations/WardrobeItemConfiguration.cs
- [ ] T024 [US1] Implement private media upload/view endpoints in backend/src/VirtualWardrobe.Api/Controllers/MediaController.cs
- [ ] T025 [US1] Implement wardrobe CRUD and category filter endpoints in backend/src/VirtualWardrobe.Api/Controllers/WardrobeItemsController.cs
- [ ] T026 [P] [US1] Implement the wardrobe API client and DTO mapping in frontend/src/services/wardrobeApi.ts
- [ ] T027 [US1] Implement the wardrobe page with category tabs and item list state in frontend/src/features/wardrobe/WardrobePage.tsx
- [ ] T028 [US1] Implement the wardrobe item form with image upload and pt-BR validation messages in frontend/src/features/wardrobe/components/WardrobeItemForm.tsx
- [ ] T029 [US1] Document wardrobe-specific component reuse decisions in frontend/src/features/wardrobe/README.md

**Checkpoint**: User Story 1 is independently functional and represents the MVP.

---

## Phase 4: User Story 2 - Track Wishlist Intent (Priority: P2)

**Goal**: Let an authenticated user manage a wishlist with fixed categories, target price, external links, and inspiration image while keeping data isolated per user.

**Independent Test**: Sign in, create and edit wishlist entries with target price, links, and inspiration image, then verify the active wishlist renders correct data and rejects duplicate or invalid links.

### Tests for User Story 2 (REQUIRED) ⚠️

- [ ] T030 [P] [US2] Add unit tests for wishlist target-price and external-link validation in backend/tests/VirtualWardrobe.UnitTests/Wishlist/WishlistValidationTests.cs
- [ ] T031 [P] [US2] Add integration tests for wishlist CRUD, history filtering defaults, and duplicate-link rejection in backend/tests/VirtualWardrobe.IntegrationTests/Wishlist/WishlistItemTests.cs
- [ ] T032 [P] [US2] Add contract tests for wishlist CRUD endpoints in backend/tests/VirtualWardrobe.ContractTests/Wishlist/WishlistContractTests.cs
- [ ] T033 [P] [US2] Add end-to-end coverage for wishlist create and edit flows in frontend/tests/e2e/wishlist.spec.ts

### Implementation for User Story 2

- [ ] T034 [P] [US2] Create the wishlist item aggregate and status defaults in backend/src/VirtualWardrobe.Domain/Wishlist/WishlistItem.cs
- [ ] T035 [P] [US2] Create the wishlist external link entity and duplicate protection rules in backend/src/VirtualWardrobe.Domain/Wishlist/WishlistExternalLink.cs
- [ ] T036 [US2] Implement wishlist create and update commands with validators in backend/src/VirtualWardrobe.Application/Wishlist/CreateWishlistItemCommand.cs
- [ ] T037 [US2] Map wishlist persistence, link ownership, and default active filtering in backend/src/VirtualWardrobe.Infrastructure/Persistence/Configurations/WishlistItemConfiguration.cs
- [ ] T038 [US2] Implement wishlist CRUD and history filter endpoints in backend/src/VirtualWardrobe.Api/Controllers/WishlistItemsController.cs
- [ ] T039 [P] [US2] Implement the wishlist API client and DTO mapping in frontend/src/services/wishlistApi.ts
- [ ] T040 [US2] Implement the wishlist page with active and history views in frontend/src/features/wishlist/WishlistPage.tsx
- [ ] T041 [US2] Implement the wishlist form with target price, links, and inspiration upload in frontend/src/features/wishlist/components/WishlistItemForm.tsx
- [ ] T042 [US2] Document wishlist-specific component reuse decisions in frontend/src/features/wishlist/README.md

**Checkpoint**: User Stories 1 and 2 are independently functional and can be demoed separately.

---

## Phase 5: User Story 3 - Convert Purchases to Wardrobe (Priority: P3)

**Goal**: Let a user mark wishlist items as purchased, keep them in history, and convert them into wardrobe items without duplicate conversion.

**Independent Test**: Starting from an existing wishlist item and wardrobe-capable system, mark the item as purchased, convert it to a wardrobe item, supply any missing required wardrobe fields, and verify the history and wardrobe stay consistent.

### Tests for User Story 3 (REQUIRED) ⚠️

- [ ] T043 [P] [US3] Add unit tests for purchase, conversion, and idempotency rules in backend/tests/VirtualWardrobe.UnitTests/Wishlist/WishlistConversionTests.cs
- [ ] T044 [P] [US3] Add integration tests for purchase history retention and wishlist-to-wardrobe conversion in backend/tests/VirtualWardrobe.IntegrationTests/Wishlist/WishlistConversionTests.cs
- [ ] T045 [P] [US3] Add contract tests for purchase and conversion endpoints in backend/tests/VirtualWardrobe.ContractTests/Wishlist/WishlistConversionContractTests.cs
- [ ] T046 [P] [US3] Add end-to-end coverage for purchase and convert flows in frontend/tests/e2e/wishlist-conversion.spec.ts

### Implementation for User Story 3

- [ ] T047 [US3] Implement purchase and conversion commands with missing-field validation in backend/src/VirtualWardrobe.Application/Wishlist/ConvertWishlistItemCommand.cs
- [ ] T048 [US3] Extend wishlist endpoints for purchase, history retention, and conversion in backend/src/VirtualWardrobe.Api/Controllers/WishlistItemsController.cs
- [ ] T049 [US3] Extend wardrobe persistence to link converted wishlist items idempotently in backend/src/VirtualWardrobe.Infrastructure/Persistence/Configurations/WardrobeItemConfiguration.cs
- [ ] T050 [P] [US3] Extend the wishlist API client for purchase and conversion actions in frontend/src/services/wishlistApi.ts
- [ ] T051 [US3] Implement purchased-history actions and conversion entry points in frontend/src/features/wishlist/WishlistPage.tsx
- [ ] T052 [US3] Implement the missing-field conversion dialog in frontend/src/features/wishlist/components/ConvertWishlistItemDialog.tsx
- [ ] T053 [US3] Document conversion-flow reuse decisions in frontend/src/features/wishlist/README.md

**Checkpoint**: All user stories are functional, and conversion behavior is independently testable.

---

## Phase 6: Polish & Cross-Cutting Concerns

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
- **Phase 3: User Story 1** depends on Phase 2 only.
- **Phase 4: User Story 2** depends on Phase 2 only.
- **Phase 5: User Story 3** depends on Phase 2 plus completion of Phase 3 and Phase 4 because it converts wishlist records into wardrobe records.
- **Phase 6: Polish** depends on the user stories that are in scope being complete.

### User Story Dependencies

- **US1 (P1)**: No dependency on other user stories after the foundational phase.
- **US2 (P2)**: No dependency on US1 after the foundational phase.
- **US3 (P3)**: Depends on US1 and US2 because conversion requires both wardrobe and wishlist behavior to exist.

### Within Each User Story

- Tests must be written and observed failing before implementation work begins.
- Domain entities and validation rules come before application handlers.
- Application handlers come before controllers and persistence wiring.
- Backend API support comes before frontend integration and end-to-end completion.
- Reuse documentation must be completed before introducing story-specific net-new components.

## Parallel Opportunities

- Setup tasks `T003` through `T006` can run in parallel after scaffolding starts.
- Foundational tasks `T010`, `T011`, `T012`, and `T014` can run in parallel once `T007` is in place.
- In US1, tests `T016` through `T019` can run in parallel, and entity tasks `T020` and `T021` can run in parallel.
- In US2, tests `T030` through `T033` can run in parallel, and entity tasks `T034` and `T035` can run in parallel.
- In US3, tests `T043` through `T046` can run in parallel, and frontend/client work `T050` can proceed once the contract is stable.
- Polish tasks `T054` through `T057` can run in parallel before the final quickstart validation task.

## Parallel Example: User Story 1

```text
T016 [US1] backend/tests/VirtualWardrobe.UnitTests/Wardrobe/WardrobeValidationTests.cs
T017 [US1] backend/tests/VirtualWardrobe.IntegrationTests/Wardrobe/WardrobeItemTests.cs
T018 [US1] backend/tests/VirtualWardrobe.ContractTests/Wardrobe/WardrobeContractTests.cs
T019 [US1] frontend/tests/e2e/wardrobe.spec.ts

T020 [US1] backend/src/VirtualWardrobe.Domain/Media/MediaAsset.cs
T021 [US1] backend/src/VirtualWardrobe.Domain/Wardrobe/WardrobeItem.cs
```

## Parallel Example: User Story 2

```text
T030 [US2] backend/tests/VirtualWardrobe.UnitTests/Wishlist/WishlistValidationTests.cs
T031 [US2] backend/tests/VirtualWardrobe.IntegrationTests/Wishlist/WishlistItemTests.cs
T032 [US2] backend/tests/VirtualWardrobe.ContractTests/Wishlist/WishlistContractTests.cs
T033 [US2] frontend/tests/e2e/wishlist.spec.ts

T034 [US2] backend/src/VirtualWardrobe.Domain/Wishlist/WishlistItem.cs
T035 [US2] backend/src/VirtualWardrobe.Domain/Wishlist/WishlistExternalLink.cs
```

## Parallel Example: User Story 3

```text
T043 [US3] backend/tests/VirtualWardrobe.UnitTests/Wishlist/WishlistConversionTests.cs
T044 [US3] backend/tests/VirtualWardrobe.IntegrationTests/Wishlist/WishlistConversionTests.cs
T045 [US3] backend/tests/VirtualWardrobe.ContractTests/Wishlist/WishlistConversionContractTests.cs
T046 [US3] frontend/tests/e2e/wishlist-conversion.spec.ts

T050 [US3] frontend/src/services/wishlistApi.ts
T052 [US3] frontend/src/features/wishlist/components/ConvertWishlistItemDialog.tsx
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Validate wardrobe CRUD, category filtering, and private media flow independently.
5. Demo or deploy the MVP before expanding scope.

### Incremental Delivery

1. Setup + Foundational create the shared auth, persistence, S3, and UI shell.
2. Deliver US1 as the first usable product increment.
3. Deliver US2 without requiring US1 changes to remain testable.
4. Deliver US3 after US1 and US2 stabilize.
5. Finish with cross-cutting polish and final quickstart validation.

### Parallel Team Strategy

1. One group completes Setup and Foundational work together.
2. After Foundation is complete, one developer can take US1 while another takes US2.
3. US3 starts after US1 and US2 contracts and persistence are complete.
4. Polish tasks can be split across observability, accessibility, performance, and CI hardening.

## Notes

- Every task follows the required checklist format with task ID, optional `[P]`, optional story label, action, and exact file path.
- User-facing copy stays in pt-BR, while code artifacts remain in English.
- Secrets must remain environment-backed; example files use placeholders only.
- Stop at each checkpoint and verify the story independently before proceeding.