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

- [ ] A1. Create `frontend/src/components/ImageFileInput.tsx` (hidden native input + styled "Escolher imagem" button + filename / "Nenhum arquivo selecionado").
- [ ] A2. Unit test for `ImageFileInput` (placeholder, filename on selection, `accept` forwarded, `error` rendered) — fails before, passes after.
- [ ] A3. Adopt `ImageFileInput` for `bodyImageFile` and `careTagImageFile` in `WardrobeItemForm.tsx`; keep `validateImage` + error display.
- [ ] A4. Adopt `ImageFileInput` for the image input in `WishlistItemForm.tsx`.
- [ ] A5. Update existing form/e2e tests/selectors that targeted the native file input.

## Change C — Move ID Value Objects to Their Own File

- [ ] C1. Create `backend/src/VirtualWardrobe.Domain/Common/Identifiers.cs` (namespace `VirtualWardrobe.Domain.Common`).
- [ ] C2. Cut the eight `XxxId` structs from `Entity.cs` and paste them verbatim into `Identifiers.cs`; leave `Entity.cs` with only `Entity<TId>`.
- [ ] C3. Regression gate: backend builds and `dotnet test` (UnitTests, IntegrationTests, ContractTests) passes; confirm the diff touches only `Entity.cs` and `Identifiers.cs`.

## Final Verification

- [ ] V1. Backend builds and full test suite passes.
- [ ] V2. Frontend `pnpm test` (and `pnpm test:e2e` where affected) passes.
- [ ] V3. Manual smoke: create a wardrobe item in category "Acessórios" with an image via the styled control.
</content>
