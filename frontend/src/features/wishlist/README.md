# Wishlist Feature Reuse Decisions

## Reuse Checklist (US2)

- Reused auth/session bootstrap and route shell from src/app/App.tsx and provider stack.
- Reused category labels and enum contracts from src/services/wardrobeApi.ts to avoid taxonomy drift.
- Reused API-client error handling and media-upload handshake pattern from wardrobe service implementation.

## New Components Added

- WishlistPage: orchestrates active/history views, CRUD refresh, and purchase transition handling.
- WishlistItemForm: validates target price, links, and inspiration upload with pt-BR messaging.

## Why Existing Shared Components Were Not Enough

- Shared primitives listed in src/components/README.md are still guidance-only and not yet implemented.
- Wishlist needed domain-specific fields (target price, external links, status views) that are not present in wardrobe form.
- Draft persistence requirements for US2 demanded feature-level integration before cross-feature extraction.

## Future Extraction Notes

- Extract common field wrappers and button bars into shared form primitives.
- Consolidate image upload validation between wardrobe and wishlist forms.
- Evaluate a shared list card primitive once wardrobe and wishlist card interactions converge.
