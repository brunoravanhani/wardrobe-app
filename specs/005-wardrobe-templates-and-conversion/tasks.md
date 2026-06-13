# Tasks: Wardrobe Templates and Combined Wishlist Conversion

**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

**Tests**: Failing tests must be written before the matching implementation task
in each phase (unit → integration → contract for backend; unit → e2e for
frontend).

---

## Phase 1 — Story A Backend: Combined Conversion ✓

**Goal**: Single `POST /v1/wishlist-items/{id}/convert` endpoint that atomically
marks the wishlist item as purchased and creates the wardrobe item.

### Tests (write first)

- [x] T001 [US-A-BE] Unit test — `WishlistItem.ConvertToWardrobe(...)` sets
  `PurchasedAt`, returns mapped wardrobe fields, and rejects a second call on an
  already-purchased item
  `tests/VirtualWardrobe.UnitTests/Wishlist/WishlistConversionTests.cs`

- [x] T002 [US-A-BE] Integration test — combined convert creates the wardrobe
  item, marks wishlist as purchased, and is idempotent; history filtering confirmed
  `tests/VirtualWardrobe.IntegrationTests/Wishlist/WishlistConversionTests.cs`

- [x] T003 [US-A-BE] Contract test — active-item combined convert succeeds
  without prior mark-purchased; idempotent double-convert returns same wardrobe id
  `tests/VirtualWardrobe.ContractTests/Wishlist/WishlistConversionContractTests.cs`

### Implementation

- [x] T004 [US-A-BE] Domain: added `WardrobeItemCreationData` value object and
  `WishlistItem.ConvertToWardrobe()` method — sets `PurchasedAt = now`, returns
  creation data, throws if already purchased
  `src/VirtualWardrobe.Domain/Wishlist/WardrobeItemCreationData.cs`
  `src/VirtualWardrobe.Domain/Wishlist/WishlistItem.cs`

- [x] T005 [US-A-BE] Application: added `CombinedConvertAsync` to
  `ConvertWishlistItemCommand` — handles Active items (purchase + convert) and
  already-Purchased items (convert only) in one `SaveChanges` scope; removed
  dead `MarkAsPurchasedAsync` and `ConvertToWardrobeAsync` methods
  `src/VirtualWardrobe.Application/Wishlist/ConvertWishlistItemCommand.cs`

- [x] T006 [US-A-BE] API: `POST /v1/wishlist-items/{id}/convert` now delegates
  to `CombinedConvertAsync`; removed `POST .../mark-purchased` endpoint
  `src/VirtualWardrobe.Api/Controllers/WishlistItemsController.cs`

---

## Phase 2 — Story A Frontend: Conversion Dialog ✓

**Goal**: Single "Converter para Guarda-Roupa" button on wishlist cards that
opens a pre-filled confirmation dialog; "Ver no Guarda-Roupa" link in history.

### Tests (write first)

- [x] T007 [US-A-FE] Unit test — `ConvertWishlistItemDialog` renders with
  wishlist-mapped default values, shows validation errors for missing required
  fields, and calls the API on submit (4 tests, all passing)
  `tests/unit/wishlist/ConvertWishlistItemDialog.test.tsx`

- [x] T008 [US-A-FE] e2e test — full convert flow: open dialog, fill missing
  field, confirm → wardrobe item appears, wishlist item moves to history with
  "Ver no Guarda-Roupa" link
  `tests/e2e/wishlist-conversion.spec.ts`

### Implementation

- [x] T009 [US-A-FE] Service: removed `markAsPurchased` (deleted endpoint);
  `convertToWardrobe(id, payload)` already present calling
  `POST /v1/wishlist-items/{id}/convert`
  `frontend/src/services/wishlistApi.ts`

- [x] T010 [US-A-FE] Component: `ConvertWishlistItemDialog` — pre-fills
  category, name, brand, target price → price from the wishlist item; prompts
  for size; inline validation on required fields; `noValidate` added to form
  `frontend/src/features/wishlist/components/ConvertWishlistItemDialog.tsx`

- [x] T011 [US-A-FE] Wishlist card: replaced the two-step purchased/convert
  buttons with a single "Converter para guarda-roupa" button shown when
  `!item.convertedWardrobeItemId`; removed `handleMarkAsPurchased`
  `frontend/src/features/wishlist/WishlistPage.tsx`

- [x] T012 [US-A-FE] Wishlist history: for items with `convertedWardrobeItemId`,
  renders a "Ver no Guarda-Roupa" `<Link to="/">` navigating to the wardrobe page
  `frontend/src/features/wishlist/WishlistPage.tsx`

---

## Phase 3 — Story B Backend: Templates and Slots

**Goal**: Read-only template+composition query, per-user slot materialization on
template selection, auto-fulfillment and reversion hooks, and
"Adicionar à Lista de Desejos" command. No slot add/remove by users —
composition is fixed per template.

### Tests (write first)

- [ ] T013 [US-B-BE] Unit tests:
  - `TemplateSlot` invariants: `Fulfill` sets wardrobe item and `fulfilled_at`;
    `Unfulfill` clears both; double-fulfill rejected
  - `TemplateSlotFulfillmentService` picks oldest open slot by `(user_id, category)`
    and is a no-op when none exist
  - `SelectTemplateCommand` creates exactly N slots matching `TemplateSlotDefinitions`
    quantities; deletes unfulfilled slots of the previous template; does not touch
    fulfilled slots of the previous template
  `tests/VirtualWardrobe.UnitTests/Templates/`

- [ ] T014 [US-B-BE] Integration tests:
  - `POST /v1/wardrobe-templates/{id}/select` materializes the correct slot count
    (Capsula → 20, Trabalho → 9); runs auto-fulfillment against existing wardrobe
    items in the same transaction
  - Switching templates deletes unfulfilled slots of the previous template and
    preserves fulfilled ones
  - Auto-fulfillment fires on wardrobe item create
  - Auto-fulfillment fires on wishlist conversion
  - Slot reverts to open when its wardrobe item is deleted
  - Extra wardrobe items beyond slot count do not block creation
  - UNIQUE constraint on `wardrobe_item_id` enforced across templates
  `tests/VirtualWardrobe.IntegrationTests/Templates/`

- [ ] T015 [US-B-BE] Contract tests:
  - `GET /v1/wardrobe-templates` returns all system templates with their slot
    compositions (category + quantity per template)
  - `GET /v1/wardrobe-templates/{id}/slots` returns user's materialized slots
  - `POST /v1/wardrobe-templates/{id}/select` response shape
  - `POST .../slots/{slotId}/link-to-wishlist` response includes new wishlist item id
  `tests/VirtualWardrobe.ContractTests/Templates/`

### Implementation

- [ ] T016 [US-B-BE] Domain: `WardrobeTemplate` read-only value type — `Id`,
  `Name`, `IReadOnlyList<TemplateSlotDefinition> SlotDefinitions`; no mutation
  methods. `TemplateSlotDefinition` value type — `Category`, `Quantity`.
  `src/VirtualWardrobe.Domain/Templates/WardrobeTemplate.cs`
  `src/VirtualWardrobe.Domain/Templates/TemplateSlotDefinition.cs`

- [ ] T017 [US-B-BE] Domain: `TemplateSlot` aggregate — `Id`, `TemplateId`,
  `UserId`, `Category`, `WardrobeItemId?`, `WishlistItemId?`, `FulfilledAt?`;
  `Fulfill(wardrobeItemId)`, `Unfulfill()`, `LinkToWishlist(wishlistItemId)` methods
  `src/VirtualWardrobe.Domain/Templates/TemplateSlot.cs`

- [ ] T018 [US-B-BE] Application service: `TemplateSlotFulfillmentService` —
  queries open slots by `(user_id, category)` sorted by `created_at ASC`, assigns
  the first result; handles wishlist-linked slot resolution
  `src/VirtualWardrobe.Application/Templates/TemplateSlotFulfillmentService.cs`

- [ ] T019 [US-B-BE] Persistence: EF Core config for `WardrobeTemplate`,
  `TemplateSlotDefinition`, and `TemplateSlot` (UNIQUE index on
  `wardrobe_item_id`); `active_template_id` column on `Users`;
  migration `20260612_AddWardrobeTemplatesAndSlots`
  `src/VirtualWardrobe.Infrastructure/Persistence/`

- [ ] T019b [US-B-BE] Data migration: `20260612_SeedDefaultTemplates` — insert
  "Capsula" (`a1000000-0000-0000-0000-000000000001`) and "Trabalho"
  (`a1000000-0000-0000-0000-000000000002`) plus all `TemplateSlotDefinitions` rows
  (Capsula: 8 TShirt, 3 Shirt, 3 Pants, 3 Shorts, 3 Shoes; Trabalho: 5 Shirt,
  3 Trousers, 1 Shoes); `Down()` removes all seeded rows
  `src/VirtualWardrobe.Infrastructure/Persistence/Migrations/`

- [ ] T020 [US-B-BE] Repository interfaces + EF implementations:
  - `IWardrobeTemplateRepository` — `GetAllAsync()` returns templates with their
    `TemplateSlotDefinitions` (no write methods)
  - `ITemplateSlotRepository` — `GetByUserAndTemplateAsync(userId, templateId)`,
    `InsertBatchAsync(slots)`, `DeleteUnfulfilledByTemplateAsync(userId, templateId)`,
    `GetOpenSlotAsync(userId, category)`
  `src/VirtualWardrobe.Application/Templates/`
  `src/VirtualWardrobe.Infrastructure/Templates/`

- [ ] T021 [US-B-BE] Application queries/commands:
  - `GetTemplatesQuery` — returns all system templates with slot compositions
  - `GetUserSlotsQuery(userId, templateId)` — returns user's materialized slots
  - `SelectTemplateCommand(userId, templateId)` — atomically deletes unfulfilled
    slots of the previous template, inserts all slots per `TemplateSlotDefinitions`,
    updates `Users.active_template_id`, runs `TemplateSlotFulfillmentService`
    against existing wardrobe items; all in one `IUnitOfWork` scope
  - `LinkSlotToWishlistCommand(userId, slotId)` — creates a wishlist item
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

- [ ] T024 [US-B-BE] API endpoints (no template write endpoints; no slot
  add/delete by user):
  - `GET  /v1/wardrobe-templates` — all templates with their slot compositions
  - `GET  /v1/wardrobe-templates/{templateId}/slots` — user's materialized slots
  - `POST /v1/wardrobe-templates/{templateId}/select` — activate/switch template
  - `POST /v1/wardrobe-templates/{templateId}/slots/{slotId}/link-to-wishlist`
  `src/VirtualWardrobe.Api/Controllers/WardrobeTemplatesController.cs`

---

## Phase 4 — Story B Frontend: Template UI

**Goal**: Template selector (auto-materializes slots on first selection),
switching confirmation modal, empty slot placeholders interleaved with owned
items, progress indicator, "Adicionar à Lista de Desejos" slot action.
No slot add/remove controls — composition is fixed.

### Tests (write first)

- [ ] T025 [US-B-FE] Unit tests:
  - `TemplateSlotCard` renders category label and "Adicionar à Lista de Desejos"
    when unfulfilled; renders wardrobe item name + link when fulfilled
  - `TemplateProgressBar` shows correct fraction ("N de M peças adquiridas")
  - Template selector calls `selectTemplate` on change and shows a confirmation
    modal when the user already has an active template
  `tests/unit/wardrobe/TemplateSlotCard.test.tsx`
  `tests/unit/wardrobe/TemplateProgressBar.test.tsx`

- [ ] T026 [US-B-FE] e2e tests:
  - Select "Capsula" → 20 slot placeholders appear (8 TShirt, 3 Shirt, 3 Pants,
    3 Shorts, 3 Shoes grouped by category); progress bar shows "0 de 20"
  - Add a wardrobe item of category TShirt → oldest TShirt slot is fulfilled;
    progress bar updates
  - Add a second TShirt → appears below the slot row (extra item always visible)
  - Delete the first TShirt → slot reverts to open placeholder
  - Switch to "Trabalho" → confirmation modal appears; on confirm, unfulfilled
    Capsula slots are gone, 9 Trabalho slots appear
  `tests/e2e/wardrobe-templates.spec.ts`

### Implementation

- [ ] T027 [US-B-FE] Service: `frontend/src/services/wardrobeTemplatesApi.ts`
  — `getTemplates()`, `getUserSlots(templateId)`, `selectTemplate(templateId)`,
  `linkSlotToWishlist(templateId, slotId)`

- [ ] T028 [US-B-FE] Component: `TemplateSlotCard` — unfulfilled state shows
  category label in pt-BR and "Adicionar à Lista de Desejos" button; fulfilled
  state shows wardrobe item name with a link
  `frontend/src/features/wardrobe/components/TemplateSlotCard.tsx`

- [ ] T029 [US-B-FE] Component: `TemplateProgressBar` — "N de M peças
  adquiridas" display, rendered above the wardrobe grid when a template is active
  `frontend/src/features/wardrobe/components/TemplateProgressBar.tsx`

- [ ] T030 [US-B-FE] Template selector: dropdown in wardrobe view header listing
  all system templates plus "Sem template" option; on selection, if a different
  template is already active show a confirmation modal ("Trocar para X removerá
  os slots não preenchidos de Y. Continuar?"); on confirm call `selectTemplate`
  `frontend/src/features/wardrobe/WardrobePage.tsx`

- [ ] T031 [US-B-FE] Wardrobe view with active template: render
  `TemplateSlotCard` placeholders for unfulfilled slots per category section,
  interleaved with owned items; owned items beyond the slot count appear below
  the slot row — no items are hidden; render `TemplateProgressBar` above the grid
  `frontend/src/features/wardrobe/WardrobePage.tsx`

- [ ] T032 [US-B-FE] "Adicionar à Lista de Desejos" action on unfulfilled
  `TemplateSlotCard`: calls `linkSlotToWishlist`, then navigates to or highlights
  the new wishlist item
  `frontend/src/features/wardrobe/components/TemplateSlotCard.tsx`
