# Backend Change Spec: Labeled Wishlist External Links

**Status**: Proposed (backend not yet implemented)

**Origin**: `wireframe.png` prototype alignment. The prototype renders wishlist
reference links as named links (e.g. **"Ver na Loja"**, **"Pinterest inspiration"**)
rather than raw URLs. The wishlist form in the prototype "Component set" pairs each
link with a **label** field and a URL field.

## Problem

Today an external link carries only a URL end-to-end:

- Domain `WishlistExternalLink` exposes `Url` only (no label).
  `backend/src/VirtualWardrobe.Domain/Wishlist/WishlistExternalLink.cs`
- The API request/response models links as a bare `string[]` of URLs.
  `backend/src/VirtualWardrobe.Api/Controllers/WishlistItemsController.cs`
  (`CreateWishlistItemRequest.Links`, `UpdateWishlistItemRequest.Links`,
  `WishlistItemResponse.Links`)
- The frontend therefore cannot send or display a human label.

**Interim frontend behavior already shipped**: the wishlist cards render each URL as
a clickable link using the URL hostname (e.g. `loja.exemplo`) as the display text.
Once this backend change lands, the frontend should display the stored label instead
(see Task 7).

## Goal

Allow each wishlist external link to carry an optional, user-provided **label**
(max 80 chars, matching `data-model.md` → `WishlistExternalLink.label`) that is
persisted and returned by the API, while preserving the existing
"duplicate URL per item is rejected" rule.

## Contract Change (target shape)

A link becomes an object instead of a string:

```jsonc
// Request (POST/PATCH /v1/wishlist-items)
"links": [
  { "url": "https://loja.exemplo/item", "label": "Ver na Loja" },
  { "url": "https://pinterest.com/pin/123", "label": "Pinterest inspiration" }
]

// Response (WishlistItemResponse)
"links": [
  { "url": "https://loja.exemplo/item", "label": "Ver na Loja" }
]
```

`label` is optional (nullable). When omitted, the API stores `null` and the client
falls back to a derived label.

> **Breaking change note**: this alters the `links` JSON shape from `string[]` to an
> object array. Frontend (Task 7) and all link fixtures in tests must move together.
> If a non-breaking rollout is required, expose the object array under a new field
> (e.g. `linkDetails`) and keep `links` as a deprecated URL-only mirror for one
> release; default decision below is the clean break since there are no external
> API consumers.

## Tasks

### Task 1 — Domain: add `Label` to `WishlistExternalLink`
- File: `backend/src/VirtualWardrobe.Domain/Wishlist/WishlistExternalLink.cs`
- Add read-only `string? Label` property.
- Extend `Create(...)` and `Rehydrate(...)` with an optional `label` parameter.
- Validation: trim label; treat empty/whitespace as `null`; reject `label.Length > 80`
  with an `ArgumentException` (mirrors existing URL validation style).
- Keep the existing absolute-URL validation unchanged.

### Task 2 — Domain: thread label through `WishlistItem`
- File: `backend/src/VirtualWardrobe.Domain/Wishlist/WishlistItem.cs`
- Update the link add/replace behavior (the method that builds `ExternalLinks`) to
  accept `(url, label)` pairs.
- Preserve the existing invariant: duplicate URL per wishlist item is rejected
  (dedupe on URL only — label does not make a duplicate URL unique).

### Task 3 — Persistence: EF Core mapping + migration
- Add the `Label` column to the `WishlistExternalLink` configuration
  (`backend/src/VirtualWardrobe.Infrastructure/...` EF config for the entity).
- Column: `varchar(80)`, nullable.
- Create a timestamped migration per CLAUDE.md convention
  (`<timestamp>_AddWishlistLinkLabel`) via:
  `dotnet ef migrations add <timestamp>_AddWishlistLinkLabel --project src/VirtualWardrobe.Infrastructure --startup-project src/VirtualWardrobe.Api`
- Roll-forward only; backfill is not required (existing rows get `NULL`).
- Update the rehydration/repository read path to load `Label`.

### Task 4 — Application: command inputs
- File: `backend/src/VirtualWardrobe.Application/Wishlist/` (Create/Update inputs in
  `CreateWishlistItemCommand` and related input records).
- Replace the `IReadOnlyList<string>` links input with a small record
  `WishlistLinkInput(string Url, string? Label)` (Application-layer DTO).
- Map inputs into the domain `(url, label)` add/replace call.

### Task 5 — API: request/response contracts
- File: `backend/src/VirtualWardrobe.Api/Controllers/WishlistItemsController.cs`
- Introduce `record WishlistLinkPayload(string Url, string? Label)`.
- Change `CreateWishlistItemRequest.Links` and `UpdateWishlistItemRequest.Links`
  to `WishlistLinkPayload[]?`.
- Change `WishlistItemResponse.Links` to `WishlistLinkPayload[]`, and update `Map(...)`
  to project `item.ExternalLinks.Select(x => new WishlistLinkPayload(x.Url, x.Label))`.

### Task 6 — Backend tests (write failing first, per TR-004)
- Unit (`VirtualWardrobe.UnitTests`): label trimming, empty→null, `>80` rejection,
  duplicate-URL still rejected regardless of label.
- Contract (`VirtualWardrobe.ContractTests`): request/response `links` object shape,
  including `label` omitted → `null`.
- Integration (`VirtualWardrobe.IntegrationTests`): create with labels → list returns
  labels for the owner; data isolation preserved.

### Task 7 — Frontend follow-up (after backend ships)
- `frontend/src/services/wishlistApi.ts`: change `WishlistItem.links` and the upsert
  input from `string[]` to `{ url: string; label: string | null }[]`; update
  `toApiPayload`.
- `frontend/src/features/wishlist/components/WishlistItemForm.tsx`: replace the
  newline-separated URL textarea with paired URL + label inputs (repeatable rows),
  matching the prototype "Wishlist Form" (URL field + optional label field, per-row
  validation for invalid URL).
- `frontend/src/features/wishlist/WishlistPage.tsx`: render `link.label ?? deriveLinkLabel(link.url)`
  as the anchor text; the `deriveLinkLabel` hostname fallback already exists.
- Update `frontend/tests/e2e/wishlist*.spec.ts` link fixtures/mocks to the object shape.

## Out of Scope
- Reordering links, per-link icons/favicons, link click analytics.
- Any change to wardrobe items or media handling (image rendering is already
  delivered on the frontend via the existing `POST /v1/media/{id}/view-url` endpoint).
