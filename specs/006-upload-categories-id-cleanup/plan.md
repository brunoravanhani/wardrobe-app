# Implementation Plan: Upload UX, New Categories, and ID Cleanup

**Branch**: `006-upload-categories-id-cleanup` | **Date**: 2026-06-17 | **Spec**: [spec.md](./spec.md)

## Summary

Three independent changes delivered together:

- **A — Styled upload control**: extract a reusable `ImageFileInput` component
  (styled button + filename) and adopt it in the wardrobe and wishlist forms.
  Frontend-only; no API change.
- **B — New categories**: append `Polo` and `Accessories` to `ClothingCategory`
  (backend enum) and to the frontend category list/labels/numeric map.
  No migration (category persists as enum name string).
- **C — ID file move**: move the eight strongly-typed ID value objects out of
  `Entity.cs` into a new file in the same namespace. Pure file reorganization —
  no type, behavior, schema, or API change; no usage edits.

Recommended order: **B → A → C** (all small and isolated). Each can be a separate
commit/PR slice.

## Technical Context

**Language/Version**: TypeScript (React 18+, Vite), C# 12 on .NET 8

**Migrations**: none. Change B persists categories as enum-name strings in an
existing `varchar(32)` column; Change C only relocates source files.

**API contracts**: unchanged for all three.

## Change A — Styled File-Upload Control

Steps:
1. Create `frontend/src/components/ImageFileInput.tsx`:
   - Props: `id`, `label`, `accept`, `value: File | null`, `onChange(File|null)`,
     optional `error`.
   - Renders a visually-hidden `<input type="file">` wired to a styled
     `<label>`/button ("Escolher imagem") and a filename span
     ("Nenhum arquivo selecionado" when empty).
   - Preserve accessibility: label `htmlFor` → input `id`; keyboard-focusable.
2. Replace the two `<input type="file">` blocks in `WardrobeItemForm.tsx`
   (`bodyImageFile`, `careTagImageFile`) with `ImageFileInput`. Keep the existing
   `validateImage` logic and error rendering.
3. Replace the image input in `WishlistItemForm.tsx` similarly.
4. Tests: add a unit test for `ImageFileInput` (renders button + placeholder,
   shows filename on selection, forwards `accept`, surfaces `error`). Update any
   existing form tests/e2e selectors that targeted the native input.

## Change B — New Categories

Backend:
1. Add `Polo = 8` and `Accessories = 9` to
   `backend/src/VirtualWardrobe.Domain/Common/ClothingCategory.cs`.
2. Confirm no exhaustive `switch` over `ClothingCategory` needs a new arm
   (grep usages); add arms where the compiler/analyzer requires.
3. Test: enum round-trip / API accepts and returns `"Polo"` and `"Accessories"`
   (extend an existing wardrobe contract or unit test with the new values).

Frontend (`frontend/src/services/wardrobeApi.ts`):
4. Append `'Polo'`, `'Accessories'` to `CLOTHING_CATEGORIES`.
5. Add labels `Polo: 'Polo'`, `Accessories: 'Acessórios'` to
   `CATEGORY_LABELS_PT_BR`.
6. Add `8: 'Polo'`, `9: 'Accessories'` to `NUMERIC_TO_CATEGORY`.
7. Test: assert both new categories appear in the wardrobe form dropdown and map
   to the correct pt-BR labels.

## Change C — Move ID Value Objects to Their Own File

1. Create `backend/src/VirtualWardrobe.Domain/Common/Identifiers.cs` with
   `namespace VirtualWardrobe.Domain.Common;`.
2. Cut the eight `readonly record struct` ID types (`UserId`, `MediaAssetId`,
   `WardrobeItemId`, `WishlistItemId`, `WishlistExternalLinkId`,
   `WardrobeTemplateId`, `TemplateSlotDefinitionId`, `TemplateSlotId`) from
   `Entity.cs` and paste them verbatim into `Identifiers.cs`.
3. Leave `Entity.cs` with only `Entity<TId>` (and its existing `using`).
4. No other files change — same namespace means no `using` or usage edits.
5. Regression gate: backend builds; full suite (unit, integration, contract)
   green; confirm the diff touches only `Entity.cs` and the new `Identifiers.cs`.

## Constitution Checks

- **Testing Gate**: New/updated tests for A (component) and B (categories) fail
  before, pass after. C is guarded by the existing suite passing unchanged.
- **Reuse Gate**: A introduces one shared `ImageFileInput` reused by all upload
  sites instead of duplicating markup.
- **Architecture Gate**: C only relocates source files; layering, patterns, and
  types are untouched.
- **DB Versioning Gate**: No migrations — categories persist as enum-name
  strings; C is source-only.
- **Secret Management Gate**: No new secrets.
- **UX Consistency Gate**: pt-BR for all new strings ("Escolher imagem",
  "Nenhum arquivo selecionado", "Polo", "Acessórios"); styled control matches
  existing form button styling.
- **Performance Gate**: No data-path changes; no measurable impact.
</content>
