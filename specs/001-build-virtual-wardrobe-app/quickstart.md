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
