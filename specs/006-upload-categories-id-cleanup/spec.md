# Feature Specification: Upload UX, New Categories, and ID Cleanup

**Feature Branch**: `006-upload-categories-id-cleanup`

**Created**: 2026-06-17

**Status**: Clarified

## Overview

Three independent maintenance/enhancement changes bundled into one delivery:

1. **Styled file-upload control** — replace the bare native `<input type="file">`
   in every image upload with a reusable styled control: a custom
   "Escolher imagem" button plus the selected filename shown next to it. Applies
   to all current upload sites (wardrobe item form: body image and care-tag
   image; wishlist item form image).

2. **Two new clothing categories** — add `Polo` and `Acessórios` (Accessories) to
   the fixed `ClothingCategory` set, available everywhere categories are
   selectable and displayable (backend enum, frontend list, pt-BR labels).

3. **Move ID value objects to their own file** — relocate the per-aggregate ID
   value objects (`WardrobeItemId`, `WishlistItemId`, `MediaAssetId`, `UserId`,
   `WishlistExternalLinkId`, `WardrobeTemplateId`, `TemplateSlotDefinitionId`,
   `TemplateSlotId`) out of `Entity.cs` into a dedicated file in the same
   `VirtualWardrobe.Domain.Common` namespace, leaving `Entity.cs` with only
   `Entity<TId>`. The structs are kept unchanged; this is a file-organization
   move with no type, behavior, API, or schema change.

---

## Clarifications

### Session 2026-06-17

- Q: What does "better view" for file uploads mean? → A: A styled
  "Escolher imagem" button plus the selected filename text. (No preview
  thumbnail, no drag-and-drop, no separate remove button in v1.)
- Q: Where do the two new categories go in the enum? → A: Appended as new
  numeric values so existing persisted category data and seeds are untouched
  (`Polo = 8`, `Accessories = 9`).
- Q: Should the ID value objects be removed/replaced with `Guid`? → A: No —
  keep them exactly as they are; only move them out of `Entity.cs` into a
  separate file in the same namespace. No usage changes anywhere.

---

## Change 1 — Styled File-Upload Control

### Current state

Each upload is a raw `<input type="file" accept="image/jpeg,image/png,image/webp">`
inside a `<label>`. The browser renders its default "Choose File / no file
chosen" control, which is inconsistent with the rest of the form styling and
gives no clear affordance in pt-BR.

Upload sites:
- `frontend/src/features/wardrobe/components/WardrobeItemForm.tsx` — `bodyImageFile`, `careTagImageFile`
- `frontend/src/features/wishlist/components/WishlistItemForm.tsx` — image input

### Target state

A single reusable component (e.g. `ImageFileInput`) under
`frontend/src/components/` that:

- Renders a styled "Escolher imagem" button consistent with existing form
  buttons (Tailwind, matching the project palette).
- Shows the selected file's name beside the button, or a neutral
  "Nenhum arquivo selecionado" placeholder when none is chosen.
- Keeps the real `<input type="file">` visually hidden but accessible (label
  association / `aria` wiring preserved), retaining `accept` and the existing
  validation hooks (type and 10 MB size checks stay in the form).
- Emits the selected `File | null` exactly as the current inputs do, so form
  state and submission are unchanged.

### Out of scope (this change)

- Image preview thumbnails
- Drag-and-drop dropzone
- A dedicated remove/clear button (selecting a new file replaces; this matches
  current behavior)

### Acceptance Scenarios

1. **Given** the wardrobe item form, **When** the user opens the body-image
   control, **Then** a styled "Escolher imagem" button is shown with
   "Nenhum arquivo selecionado" beside it.
2. **Given** the styled control, **When** the user selects a valid image,
   **Then** the chosen filename replaces the placeholder and the file is held in
   form state exactly as today.
3. **Given** a selected file that violates type or size rules, **When** the user
   submits, **Then** the same inline validation message appears as before
   (behavior unchanged).
4. **Given** the wishlist item form, **When** the user uses its image control,
   **Then** it presents the same styled button + filename experience.

---

## Change 2 — New Clothing Categories: Polo and Acessórios

### Backend

`ClothingCategory` enum (`VirtualWardrobe.Domain.Common.ClothingCategory`) gains:

```
Polo        = 8
Accessories = 9
```

Values are appended to preserve existing persisted `category` strings and the
seeded `TemplateSlotDefinitions` numbering. The category is persisted as the
enum **name** string (`record.Category = item.Category.ToString()`), so `"Polo"`
and `"Accessories"` become valid stored values automatically — no migration is
required.

### Frontend

`frontend/src/services/wardrobeApi.ts`:
- Add `'Polo'` and `'Accessories'` to `CLOTHING_CATEGORIES`.
- Add pt-BR labels to `CATEGORY_LABELS_PT_BR`: `Polo: 'Polo'`,
  `Accessories: 'Acessórios'`.
- Extend `NUMERIC_TO_CATEGORY`: `8: 'Polo'`, `9: 'Accessories'`.

Both new categories then appear automatically in every category dropdown and
label lookup (wardrobe form, wishlist form, conversion dialog, filters).

### Acceptance Scenarios

1. **Given** the wardrobe item form category dropdown, **When** the user opens
   it, **Then** "Polo" and "Acessórios" appear as selectable options.
2. **Given** a wardrobe item saved with category Polo, **When** it is listed,
   **Then** its category renders as "Polo" in pt-BR.
3. **Given** the wishlist form, **When** the user selects "Acessórios" and saves,
   **Then** the item persists and round-trips correctly.
4. **Given** existing items in pre-existing categories, **When** the change ships,
   **Then** they are unaffected (no migration, no renumbering).

### Out of scope (this change)

- Adding the new categories to any template's default slot composition.
- Category-specific icons or imagery.

---

## Change 3 — Move ID Value Objects to Their Own File

### Current state

`Entity.cs` declares `Entity<TId>` plus eight `readonly record struct` ID
wrappers (`UserId`, `MediaAssetId`, `WardrobeItemId`, `WishlistItemId`,
`WishlistExternalLinkId`, `WardrobeTemplateId`, `TemplateSlotDefinitionId`,
`TemplateSlotId`) all in one file. Mixing the base entity type with the eight
ID structs makes the file harder to navigate.

### Target state

- Move the eight ID value-object structs verbatim into a new file in the same
  folder and namespace, e.g.
  `backend/src/VirtualWardrobe.Domain/Common/Identifiers.cs`
  (namespace stays `VirtualWardrobe.Domain.Common`).
- `Entity.cs` retains only `Entity<TId>`.
- The structs themselves are unchanged — same names, same members, same
  `New()`/`ToString()`. Because the namespace is unchanged, no `using`
  statements and no usages anywhere else need to change.

### Constraints

- **No type, behavior, API, or schema change.** Pure file reorganization.
- All existing tests must pass unchanged with no edits to test code.

### Acceptance Scenarios

1. **Given** the codebase after the change, **When** `Entity.cs` is inspected,
   **Then** it contains only `Entity<TId>` and no `XxxId` structs.
2. **Given** the new identifiers file, **When** it is inspected, **Then** it
   contains all eight ID structs unchanged in the `VirtualWardrobe.Domain.Common`
   namespace.
3. **Given** the full backend test suite, **When** it runs, **Then** all tests
   pass with no source edits outside the two moved files.

### Out of scope (this change)

- Replacing the ID value objects with plain `Guid`.
- Renaming the structs, changing their members, or changing the namespace.
- Any database, migration, or API contract change.

---

## Cross-Cutting Notes

- The three changes are independent and can be reviewed/merged separately, but
  ship together on branch `006-upload-categories-id-cleanup`.
- **Testing Gate**: changes A and B add or update tests that fail before and pass
  after — styled-input render/behavior test and category presence tests
  (frontend list + backend enum round-trip). Change 3 is a file move guarded by
  the existing backend suite passing unchanged.
- **UX Consistency Gate**: all new user-facing text in pt-BR ("Escolher imagem",
  "Nenhum arquivo selecionado", "Polo", "Acessórios").
- **Architecture Gate**: Change 3 only relocates source; it does not alter
  layering, the domain model, or any type.
</content>
</invoke>
