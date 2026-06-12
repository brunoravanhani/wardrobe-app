# Tasks: Wardrobe Templates and Combined Wishlist Conversion

**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

**Tests**: Failing tests must be written before the matching implementation task
in each phase (unit → integration → contract for backend; unit → e2e for
frontend).

---

## Phase 1 — Story A Backend: Combined Conversion

**Goal**: Single `POST /v1/wishlist-items/{id}/convert` endpoint that atomically
marks the wishlist item as purchased and creates the wardrobe item.

### Tests (write first)

- [ ] T001 [US-A-BE] Unit test — `WishlistItem.ConvertToWardrobe(...)` sets
  `PurchasedAt`, returns mapped wardrobe fields, and rejects a second call on an
  already-purchased item
  `tests/VirtualWardrobe.UnitTests/Wishlist/WishlistConversionTests.cs`

- [ ] T002 [US-A-BE] Integration test — `POST /v1/wishlist-items/{id}/convert`
  creates the wardrobe item, marks wishlist as purchased, returns 201 with the
  new wardrobe item id; also covers: item not owned → 403, item already
  purchased → 409, missing required fields → 422
  `tests/VirtualWardrobe.IntegrationTests/Wishlist/WishlistConversionTests.cs`

- [ ] T003 [US-A-BE] Contract test — request/response shape for
  `POST .../convert`, including pre-filled defaults from wishlist data
  `tests/VirtualWardrobe.ContractTests/Wishlist/WishlistConversionContractTests.cs`

### Implementation

- [ ] T004 [US-A-BE] Domain: add `ConvertToWardrobe(wardrobeFields...)` method
  to `WishlistItem` — sets `PurchasedAt = now`, returns `WardrobeItemCreationData`
  value object, returns failure Result if already purchased
  `src/VirtualWardrobe.Domain/Wishlist/WishlistItem.cs`

- [ ] T005 [US-A-BE] Application: add `ConvertWishlistItemCommand` and handler
  — calls `WishlistItem.ConvertToWardrobe(...)`, persists both changes inside one
  `IUnitOfWork` scope
  `src/VirtualWardrobe.Application/Wishlist/ConvertWishlistItemCommand.cs`

- [ ] T006 [US-A-BE] API: add `POST /v1/wishlist-items/{id}/convert` endpoint
  with `ConvertWishlistItemRequest` body (wardrobe fields, pre-populated from
  wishlist on the client); return 201 with `Location` header pointing to the new
  wardrobe item
  `src/VirtualWardrobe.Api/Controllers/WishlistItemsController.cs`

---

## Phase 2 — Story A Frontend: Conversion Dialog

**Goal**: Single "Converter para Guarda-Roupa" button on wishlist cards that
opens a pre-filled confirmation dialog; "Ver no Guarda-Roupa" link in history.

### Tests (write first)

- [ ] T007 [US-A-FE] Unit test — `ConvertWishlistItemDialog` renders with
  wishlist-mapped default values, shows validation errors for missing required
  fields, and calls the API on submit
  `tests/unit/wishlist/ConvertWishlistItemDialog.test.tsx`

- [ ] T008 [US-A-FE] e2e test — full convert flow: open dialog, fill missing
  field, confirm → wardrobe item appears, wishlist item moves to history with
  "Ver no Guarda-Roupa" link
  `tests/e2e/wishlist-conversion.spec.ts`

### Implementation

- [ ] T009 [US-A-FE] Service: add `convertWishlistItem(id, payload)` to
  `frontend/src/services/wishlistApi.ts` calling
  `POST /v1/wishlist-items/{id}/convert`

- [ ] T010 [US-A-FE] Component: `ConvertWishlistItemDialog` — pre-fills
  category, name, brand, target price → price from the wishlist item; prompts
  for size and body image if missing; inline validation on required fields
  `frontend/src/features/wishlist/components/ConvertWishlistItemDialog.tsx`

- [ ] T011 [US-A-FE] Wishlist card: replace the two-step purchased/convert
  buttons with a single "Converter para Guarda-Roupa" button that opens
  `ConvertWishlistItemDialog`
  `frontend/src/features/wishlist/components/WishlistItemCard.tsx`

- [ ] T012 [US-A-FE] Wishlist history: for items with a `wardrobeItemId`,
  render a "Ver no Guarda-Roupa" link that navigates to the wardrobe item
  `frontend/src/features/wishlist/WishlistPage.tsx`

---

## Phase 3 — Story B Backend: Templates and Slots

**Goal**: Read-only template list, per-user slot CRUD, auto-fulfillment and
reversion hooks, and "Adicionar à Lista de Desejos" command. No template
create/rename/delete — templates are system-defined.

### Tests (write first)

- [ ] T013 [US-B-BE] Unit tests — `TemplateSlot` invariants: valid category,
  `Fulfill` sets wardrobe item and `fulfilled_at`, `Unfulfill` clears both,
  double-fulfill rejected; `TemplateSlotFulfillmentService` picks oldest open
  slot by `(user_id, category)` and skips when none exist
  `tests/VirtualWardrobe.UnitTests/Templates/`

- [ ] T014 [US-B-BE] Integration tests — slot add/remove per user+template;
  auto-fulfillment fires on wardrobe item create; auto-fulfillment fires on
  wishlist conversion; slot reverts to open when wardrobe item is deleted;
  extra wardrobe items beyond slot count do not block creation; UNIQUE constraint
  on `wardrobe_item_id` enforced across templates
  `tests/VirtualWardrobe.IntegrationTests/Templates/`

- [ ] T015 [US-B-BE] Contract tests — `GET /v1/wardrobe-templates` returns all
  system templates; slot list/create/delete shapes; `link-to-wishlist` response
  includes new wishlist item id
  `tests/VirtualWardrobe.ContractTests/Templates/`

### Implementation

- [ ] T016 [US-B-BE] Domain: `WardrobeTemplate` read-only value type — `Id`,
  `Name` only; no mutation methods
  `src/VirtualWardrobe.Domain/Templates/WardrobeTemplate.cs`

- [ ] T017 [US-B-BE] Domain: `TemplateSlot` aggregate — `Id`, `TemplateId`,
  `UserId`, `Category`, `WardrobeItemId?`, `WishlistItemId?`, `FulfilledAt?`;
  `Fulfill(wardrobeItemId)`, `Unfulfill()`, `LinkToWishlist(wishlistItemId)` methods
  `src/VirtualWardrobe.Domain/Templates/TemplateSlot.cs`

- [ ] T018 [US-B-BE] Application service: `TemplateSlotFulfillmentService` —
  queries open slots by `(user_id, category)` sorted by `created_at ASC`, assigns
  the first result; handles wishlist-linked slot resolution
  `src/VirtualWardrobe.Application/Templates/TemplateSlotFulfillmentService.cs`

- [ ] T019 [US-B-BE] Persistence: EF Core config for `WardrobeTemplate` (no
  `user_id`, no navigation to users) and `TemplateSlot` (UNIQUE index on
  `wardrobe_item_id`); migration `20260611_AddWardrobeTemplatesAndSlots`
  `src/VirtualWardrobe.Infrastructure/Persistence/`

- [ ] T019b [US-B-BE] Data migration: `20260611_SeedDefaultTemplates` — insert
  "Capsula" (`a1000000-0000-0000-0000-000000000001`) and "Trabalho"
  (`a1000000-0000-0000-0000-000000000002`) using `migrationBuilder.InsertData`;
  `Down()` removes them with `migrationBuilder.DeleteData`
  `src/VirtualWardrobe.Infrastructure/Persistence/Migrations/`

- [ ] T020 [US-B-BE] Repository interfaces + EF implementations:
  - `IWardrobeTemplateRepository` — `GetAllAsync()` only (no write methods)
  - `ITemplateSlotRepository` — `AddAsync`, `RemoveAsync`, `GetByUserAndTemplateAsync`,
    `GetOpenSlotAsync(userId, category)`
  `src/VirtualWardrobe.Application/Templates/`
  `src/VirtualWardrobe.Infrastructure/Templates/`

- [ ] T021 [US-B-BE] Application queries/commands:
  - `GetTemplatesQuery` — returns all system templates
  - `GetUserSlotsQuery(userId, templateId)` — returns user's slots for a template
  - `AddSlotCommand(userId, templateId, category)`
  - `RemoveSlotCommand(userId, slotId)`
  - `LinkSlotToWishlistCommand(userId, slotId)` — creates wishlist item
    pre-filled with slot category and sets `TemplateSlot.WishlistItemId`
  `src/VirtualWardrobe.Application/Templates/`

- [ ] T022 [US-B-BE] Hook auto-fulfillment into `CreateWardrobeItemHandler`
  and `ConvertWishlistItemHandler`: call `TemplateSlotFulfillmentService` after
  successful item creation
  `src/VirtualWardrobe.Application/Wardrobe/CreateWardrobeItemHandler.cs`
  `src/VirtualWardrobe.Application/Wishlist/ConvertWishlistItemCommand.cs`

- [ ] T023 [US-B-BE] Hook slot reversion into `DeleteWardrobeItemHandler`:
  if the deleted item fills a slot, call `TemplateSlot.Unfulfill()`
  `src/VirtualWardrobe.Application/Wardrobe/DeleteWardrobeItemHandler.cs`

- [ ] T024 [US-B-BE] API endpoints (no template write endpoints):
  - `GET    /v1/wardrobe-templates`
  - `GET    /v1/wardrobe-templates/{templateId}/slots`
  - `POST   /v1/wardrobe-templates/{templateId}/slots`
  - `DELETE /v1/wardrobe-templates/{templateId}/slots/{slotId}`
  - `POST   /v1/wardrobe-templates/{templateId}/slots/{slotId}/link-to-wishlist`
  `src/VirtualWardrobe.Api/Controllers/WardrobeTemplatesController.cs`

---

## Phase 4 — Story B Frontend: Template UI

**Goal**: Template selector, empty slot placeholders interleaved with owned
items, progress indicator, "Adicionar à Lista de Desejos" slot action.
No template management UI (create/rename/delete).

### Tests (write first)

- [ ] T025 [US-B-FE] Unit tests — `TemplateSlotCard` renders category label and
  "Adicionar à Lista de Desejos" when unfulfilled; renders link to wardrobe item
  when fulfilled; `TemplateProgressBar` shows correct fraction
  `tests/unit/wardrobe/TemplateSlotCard.test.tsx`

- [ ] T026 [US-B-FE] e2e test — select "Capsula", add a slot, verify placeholder;
  add a wardrobe item of the same category, verify slot is fulfilled; add a second
  item of the same category (extra), verify it also appears in the view; delete
  the first item, verify slot reverts to open; verify second item is still visible
  `tests/e2e/wardrobe-templates.spec.ts`

### Implementation

- [ ] T027 [US-B-FE] Service: `frontend/src/services/wardrobeTemplatesApi.ts`
  — `getTemplates()`, `getSlots(templateId)`, `addSlot(templateId, category)`,
  `removeSlot(templateId, slotId)`, `linkSlotToWishlist(templateId, slotId)`

- [ ] T028 [US-B-FE] Component: `TemplateSlotCard` — empty placeholder showing
  category label in pt-BR and "Adicionar à Lista de Desejos" button; fulfilled
  state shows the wardrobe item name with a link
  `frontend/src/features/wardrobe/components/TemplateSlotCard.tsx`

- [ ] T029 [US-B-FE] Component: `TemplateProgressBar` — "N de M peças
  adquiridas" display, rendered above the wardrobe grid when a template is active
  `frontend/src/features/wardrobe/components/TemplateProgressBar.tsx`

- [ ] T030 [US-B-FE] Template selector: dropdown in the wardrobe view header
  listing all system templates; "Sem template" option (default) shows only owned
  wardrobe items with no slot placeholders
  `frontend/src/features/wardrobe/WardrobePage.tsx`

- [ ] T031 [US-B-FE] Wardrobe view with active template: render
  `TemplateSlotCard` placeholders for unfulfilled slots in each category section,
  interleaved with owned items; owned items beyond the slot count appear below
  the slot row — no items are hidden
  `frontend/src/features/wardrobe/WardrobePage.tsx`

- [ ] T032 [US-B-FE] Slot panel: collapsible panel or section within the
  wardrobe view that lets the user add/remove category slots for the active
  template (no template create/rename/delete controls)
  `frontend/src/features/wardrobe/components/TemplateSlotPanel.tsx`

- [ ] T033 [US-B-FE] "Adicionar à Lista de Desejos" action on
  `TemplateSlotCard`: calls `linkSlotToWishlist`, then navigates to or highlights
  the new wishlist item
  `frontend/src/features/wardrobe/components/TemplateSlotCard.tsx`
