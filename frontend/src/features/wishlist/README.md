# Wishlist Feature Reuse Decisions

## Reuse Checklist (US2)

- Reused auth/session bootstrap and route shell from src/app/App.tsx and provider stack.
- Reused category labels and enum contracts from src/services/wardrobeApi.ts to avoid taxonomy drift.
- Reused API-client error handling and media-upload handshake pattern from wardrobe service implementation.

## New Components Added

- WishlistPage: orchestrates active/history views, CRUD refresh, and purchase transition handling.
- WishlistItemForm: validates target price, links, and inspiration upload with pt-BR messaging.
- ConvertWishlistItemDialog: captures missing wardrobe fields (required size and optional overrides) before conversion.

## Phase 8 Conversion Reuse Decisions (US3)

- Reused `WardrobeItem` contract and category label helpers from `src/services/wardrobeApi.ts` to keep wardrobe taxonomy and DTOs aligned.
- Reused existing wishlist history state and card actions in `WishlistPage` instead of introducing a separate conversion page.
- Reused the current API client error mapping (`parseApiError`) and mutation-refresh pattern (`await loadItems()`) for conversion outcomes.

## Why A Dedicated Conversion Dialog Was Added

- Conversion requires one mandatory wardrobe field (`size`) that does not exist in wishlist data.
- Conversion can optionally override name/category/brand/price, so an inline action without form inputs would be incomplete.
- A contextual dialog keeps users in wishlist history while collecting only missing conversion inputs.

## Why Existing Shared Components Were Not Enough

- Shared primitives listed in src/components/README.md are still guidance-only and not yet implemented.
- Wishlist needed domain-specific fields (target price, external links, status views) that are not present in wardrobe form.
- Draft persistence requirements for US2 demanded feature-level integration before cross-feature extraction.

## Future Extraction Notes

- Extract common field wrappers and button bars into shared form primitives.
- Consolidate image upload validation between wardrobe and wishlist forms.
- Evaluate a shared list card primitive once wardrobe and wishlist card interactions converge.
