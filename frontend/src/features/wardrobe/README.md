# Wardrobe Feature Reuse Decisions

## Reuse Checklist (US1)

- Reused app shell, auth bootstrap, and routing from src/app/App.tsx.
- Reused category and DTO contracts from src/services/wardrobeApi.ts to keep API mapping centralized.
- Reused shared visual primitives already established in Tailwind utility classes for cards/forms/buttons to avoid new design tokens.

## New Components Added

- WardrobePage: feature container that orchestrates category filtering, list loading, and form editing states.
- WardrobeItemForm: wardrobe-specific form that validates pt-BR input rules and image constraints before API calls.

## Why Existing Shared Components Were Not Enough

- The planned cross-feature primitives (FormField, CurrencyInput, ImageUploadField, EntityCard) are documented but not implemented yet.
- To keep US1 delivery independent and complete, wardrobe needed a local form + list implementation now.
- The form structure was intentionally built to be extractable later into shared primitives during US2/Polish without changing API contracts.

## Future Extraction Notes

- Extract repeated label/input/error structure to a FormField primitive.
- Extract file validation + upload input into ImageUploadField.
- Extract wardrobe list card markup into EntityCard once wishlist cards exist.
