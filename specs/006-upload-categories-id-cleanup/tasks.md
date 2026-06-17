# Tasks: Upload UX, New Categories, and ID Cleanup

**Branch**: `006-upload-categories-id-cleanup` | **Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

Suggested order: B → A → C. Each block is independently shippable.

## Change B — New Categories

- [x] B1. Add `Polo = 8` and `Accessories = 9` to `ClothingCategory.cs`.
- [x] B2. Grep for exhaustive `switch`/maps over `ClothingCategory`; add arms where required by the compiler/analyzer. (None found — categories flow through enum-name parsing and the central frontend maps.)
- [x] B3. Backend test: wardrobe create/list round-trip accepts and returns `"Polo"` and `"Accessories"` (`WardrobeItemTests` `[Theory]`, 3/3 pass).
- [x] B4. Frontend: append `'Polo'`, `'Accessories'` to `CLOTHING_CATEGORIES` in `wardrobeApi.ts`.
- [x] B5. Frontend: add pt-BR labels `Polo: 'Polo'`, `Accessories: 'Acessórios'`.
- [x] B6. Frontend: extend `NUMERIC_TO_CATEGORY` with `8: 'Polo'`, `9: 'Accessories'`.
- [x] B7. Frontend test: `categories.test.ts` covers list membership, pt-BR labels, and numeric coercion (3/3 pass).

## Change A — Styled File-Upload Control

- [x] A1. Create `frontend/src/components/ImageFileInput.tsx` (sr-only native input triggered by a styled "Escolher imagem" button + filename / "Nenhum arquivo selecionado"). Label kept associated to the input via `htmlFor`.
- [x] A2. Unit test for `ImageFileInput` (placeholder, filename on selection, `accept` forwarded, label association, `error` rendered) — `ImageFileInput.test.tsx`, 4/4 pass.
- [x] A3. Adopt `ImageFileInput` for `bodyImageFile` and `careTagImageFile` in `WardrobeItemForm.tsx`; `validateImage` + error display unchanged.
- [x] A4. Adopt `ImageFileInput` for the inspiration image in `WishlistItemForm.tsx`.
- [x] A5. No e2e/selector changes needed — the field `<label>`→file-input association is preserved, so `getByLabel(...)` + `setInputFiles` still resolve the input (guarded by the A2 association test). Typecheck clean; no new unit-test regressions.

## Change C — Move ID Value Objects to Their Own File

- [x] C1. Create `backend/src/VirtualWardrobe.Domain/Common/Identifiers.cs` (namespace `VirtualWardrobe.Domain.Common`).
- [x] C2. Cut the eight `XxxId` structs from `Entity.cs` and paste them verbatim into `Identifiers.cs`; `Entity.cs` now holds only `Entity<TId>` (-48 lines).
- [x] C3. Regression gate: build clean (0 warnings/errors); `dotnet test` 65/65 pass (34 unit + 16 integration + 15 contract); diff touches only `Entity.cs` and the new `Identifiers.cs`.

## Final Verification

- [ ] V1. Backend builds and full test suite passes.
- [ ] V2. Frontend `pnpm test` (and `pnpm test:e2e` where affected) passes.
- [ ] V3. Manual smoke: create a wardrobe item in category "Acessórios" with an image via the styled control.
</content>
