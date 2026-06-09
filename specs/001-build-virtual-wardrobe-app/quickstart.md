# Quickstart: Virtual Wardrobe and Wishlist Management

## 1. Prerequisites

- Node.js 20+
- pnpm or npm
- .NET SDK 8
- PostgreSQL 15+
- AWS account with a private S3 bucket configured for object storage
- Google OAuth credentials (web client)

## 2. Environment Configuration

Create local environment files with placeholders only.

### Backend (`backend/.env` or secure equivalent)

- `ASPNETCORE_ENVIRONMENT=Development`
- `ConnectionStrings__Default=Host=localhost;Port=5432;Database=virtual_wardrobe;Username=<user>;Password=<password>`
- `Auth__Google__ClientId=<google-client-id>`
- `Auth__Google__ClientSecret=<google-client-secret>`
- `Jwt__SigningKey=<jwt-signing-key>`
- `AWS__Region=<aws-region>`
- `AWS__S3__BucketName=<private-bucket-name>`
- `AWS__AccessKeyId=<access-key-id>`
- `AWS__SecretAccessKey=<secret-access-key>`

### Frontend (`frontend/.env.local`)

- `VITE_API_BASE_URL=http://localhost:5000`
- `VITE_GOOGLE_CLIENT_ID=<google-client-id>`
- `VITE_DEFAULT_LOCALE=pt-BR`

## 3. Local Run Flow

1. Start PostgreSQL locally and create the target database.
2. Run backend migrations and start API.
3. Start frontend development server.
4. Sign in with Google and validate wardrobe/wishlist core flows.
5. Confirm the S3 bucket has Block Public Access enabled before media tests.

## 3.1 Database Versioning and Migrations

- Migration authority: EF Core migrations are the only supported relational schema change mechanism.
- Naming convention: use timestamped names (example: `20260603_AddWishlistIndexes`) to preserve deterministic ordering.
- Source control: migration files and model snapshots must be committed with the related feature change.

Recommended commands:

```bash
# create migration
cd backend
dotnet ef migrations add 20260603_AddWishlistIndexes \
	--project src/VirtualWardrobe.Infrastructure \
	--startup-project src/VirtualWardrobe.Api

# generate reviewed SQL script for release pipeline
dotnet ef migrations script \
	--project src/VirtualWardrobe.Infrastructure \
	--startup-project src/VirtualWardrobe.Api \
	--idempotent \
	--output ./artifacts/sql/wardrobe-migrations.sql

# apply latest migration locally
dotnet ef database update \
	--project src/VirtualWardrobe.Infrastructure \
	--startup-project src/VirtualWardrobe.Api
```

Rollback guidance:

- Prefer roll-forward with a correcting migration in shared environments.
- Use down migration rollback only for controlled local/dev recovery.
- Validate migration execution by checking `__EFMigrationsHistory` after deployment.

Example command sequence (adjust when scaffolding exists):

```bash
# backend
cd backend
dotnet restore
dotnet ef database update
dotnet run --project src/VirtualWardrobe.Api

# frontend
cd ../frontend
pnpm install
pnpm dev
```

## 4. Test Execution Gates

Run tests with fail-before-pass proof for behavior changes.

```bash
# backend
cd backend
dotnet test tests/VirtualWardrobe.UnitTests
dotnet test tests/VirtualWardrobe.IntegrationTests
dotnet test tests/VirtualWardrobe.ContractTests

# frontend
cd ../frontend
pnpm test
pnpm test:e2e
```

## 5. Validation Checklist

- Google login succeeds and user data isolation is enforced.
- Wardrobe item CRUD works for all fixed categories.
- Wishlist supports target price, links, and inspiration image.
- Purchased wishlist items are hidden from active list and retained in history.
- Conversion from purchased wishlist item creates wardrobe item once (idempotent behavior on repeat requests).
- Upload validation enforces JPG/PNG/WebP and max 10 MB.
- Media URLs are owner-only, presigned, short-lived, and backed by a private S3 bucket.
- All UI copy is in pt-BR.
- p95 performance targets from spec are met in test runs.

## 6. Observability

Structured JSON logs are written to stdout (configured in `ApiHostingExtensions`). Key log events:

| Event | Logger | Fields |
|-------|--------|--------|
| Google token exchange | `AuthController` | `UserId` on success |
| Auth exchange failure | `AuthController` | exception message |
| Presigned upload URL requested | `MediaController` | `Purpose` |
| Presigned upload URL issued | `MediaController` | `MediaAssetId` |
| Presigned view URL requested | `MediaController` | `MediaAssetId`, `UserId` |
| Presigned URL failure | `MediaController` | error message |
| Conversion initiated | `WishlistItemsController` | `WishlistItemId`, `UserId` |
| Conversion succeeded | `WishlistItemsController` | `WishlistItemId`, `WardrobeItemId` |
| Conversion failed | `WishlistItemsController` | `WishlistItemId`, error message |

Metrics (via `System.Diagnostics.Metrics`, meter name `VirtualWardrobe.Api`):

- `auth.exchange.total` / `auth.exchange.failures`
- `media.upload_url.total` / `media.view_url.total` / `media.presign.failures`
- `wishlist.conversion.total` / `wishlist.conversion.successes` / `wishlist.conversion.failures`

## 7. CI Pipeline

The `.github/workflows/ci.yml` pipeline runs on every push and pull request to `main`:

1. **Secret & Config Audit** — verifies no `.env` files are tracked, no hardcoded secrets in source, and `.env.example` files use placeholders only.
2. **Backend Tests** — restores, builds, and runs unit, integration, and contract test suites against a live PostgreSQL service container.
3. **Frontend Tests** — lints, type-checks, runs unit tests, builds, and runs Playwright e2e tests (wardrobe, wishlist, conversion, accessibility, performance).

Required GitHub secrets:
- `JWT_SIGNING_KEY_TEST` — a test-only signing key used only in the CI environment.

## 8. Final Delivery Notes (Phase 9 — 2026-06-08)

All phases (1–9) are complete. Delivered capabilities:

- **US1**: Wardrobe item CRUD with category filtering, private image upload/view via S3 presigned URLs.
- **US2**: Wishlist management with target price, external links, inspiration image, purchased history, and unsaved draft protection.
- **US3**: Purchased-to-wardrobe conversion with idempotency and missing-field dialog.
- **Polish**: Structured logging and metrics, accessibility e2e regression suite, p95 performance verification, CI pipeline with secret audit.

Constitution compliance: all gates (code quality, testing, UX consistency, performance, secret management, observability) are satisfied across all delivered phases.
