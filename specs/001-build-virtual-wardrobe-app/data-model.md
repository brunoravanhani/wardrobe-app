# Data Model: Virtual Wardrobe and Wishlist Management

## Entity: User

- Purpose: Represents an authenticated account owner.
- Fields:
- `id` (UUID, PK)
- `googleSubject` (string, unique, required)
- `email` (string, required)
- `displayName` (string, optional)
- `locale` (string, default `pt-BR`)
- `createdAtUtc` (timestamp, required)
- `updatedAtUtc` (timestamp, required)
- Relationships:
- 1-to-many with `WardrobeItem`
- 1-to-many with `WishlistItem`
- 1-to-many with `MediaAsset`

## Entity: WardrobeItem

- Purpose: Stores owned clothing entries.
- Fields:
- `id` (UUID, PK)
- `userId` (UUID, FK -> User.id, required)
- `category` (enum, required)
- `name` (string, required, 1-120 chars)
- `brand` (string, optional, max 120 chars)
- `size` (string, required, max 32 chars)
- `price` (decimal(12,2), optional, >= 0)
- `bodyImageAssetId` (UUID, FK -> MediaAsset.id, optional)
- `careTagImageAssetId` (UUID, FK -> MediaAsset.id, optional)
- `sourceWishlistItemId` (UUID, FK -> WishlistItem.id, optional)
- `createdAtUtc` (timestamp, required)
- `updatedAtUtc` (timestamp, required)
- Validation rules:
- Category must be one of fixed v1 categories.
- At least `name`, `category`, and `size` are required at creation.
- `price` cannot be negative.

## Entity: WishlistItem

- Purpose: Stores desired future purchases and conversion history.
- Fields:
- `id` (UUID, PK)
- `userId` (UUID, FK -> User.id, required)
- `category` (enum, required)
- `name` (string, required, 1-120 chars)
- `brand` (string, optional, max 120 chars)
- `targetPrice` (decimal(12,2), required, >= 0)
- `inspirationImageAssetId` (UUID, FK -> MediaAsset.id, optional)
- `status` (enum: `Active`, `Purchased`, required)
- `purchasedAtUtc` (timestamp, optional)
- `convertedWardrobeItemId` (UUID, FK -> WardrobeItem.id, optional)
- `createdAtUtc` (timestamp, required)
- `updatedAtUtc` (timestamp, required)
- Validation rules:
- `targetPrice` is mandatory.
- `convertedWardrobeItemId` can only be set when `status = Purchased`.
- Active-list queries exclude `status = Purchased` by default.

## Entity: WishlistExternalLink

- Purpose: Supports one-or-more external references per wishlist item.
- Fields:
- `id` (UUID, PK)
- `wishlistItemId` (UUID, FK -> WishlistItem.id, required)
- `url` (string, required, valid absolute URL)
- `label` (string, optional, max 80 chars)
- `createdAtUtc` (timestamp, required)
- Validation rules:
- Duplicate URL per wishlist item should be prevented.

## Entity: MediaAsset

- Purpose: Represents uploaded image metadata and ownership.
- Fields:
- `id` (UUID, PK)
- `userId` (UUID, FK -> User.id, required)
- `storageKey` (string, unique, required)
- `contentType` (enum: `image/jpeg`, `image/png`, `image/webp`, required)
- `fileSizeBytes` (int, required, <= 10 MB)
- `visibility` (enum: `PrivateOwnerOnly`, required)
- `createdAtUtc` (timestamp, required)
- Validation rules:
- Accept only JPG/PNG/WebP.
- Reject files > 10 MB.
- Asset owner must match item owner during association.

## Entity: SchemaVersionHistory

- Purpose: Tracks applied database schema migrations and ensures deterministic schema versioning.
- Fields:
- `migrationId` (string, PK)
- `productVersion` (string, required)
- Source: EF Core managed `__EFMigrationsHistory` table.
- Validation rules:
- Migration identifiers must be unique and ordered by creation timestamp convention.
- Production environments apply migrations in order; skipped versions are not allowed.

## Enum: ClothingCategory

- Values:
- `TShirt`
- `Shirt`
- `Pants`
- `Trousers`
- `Shorts`
- `Coats`
- `Shoes`

## State Transitions

### Wishlist Item

- `Active` -> `Purchased` (when user marks as purchased)
- `Purchased` -> `Purchased + convertedWardrobeItemId` (when converted to wardrobe)
- `Purchased` remains historical and is hidden from active list by default

### Conversion Flow Consistency

- Conversion creates one `WardrobeItem` and links `WishlistItem.convertedWardrobeItemId`.
- Repeat conversion request for the same wishlist item must be rejected idempotently.

## Schema Versioning Rules

- Every relational model change requires a corresponding EF migration file in source control.
- Migration filenames use timestamp-first naming for deterministic ordering.
- Release pipelines must verify pending migrations and apply them before serving traffic.
