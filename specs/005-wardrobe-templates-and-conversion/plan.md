# Implementation Plan: Wardrobe Templates and Combined Wishlist Conversion

**Branch**: `005-wardrobe-templates-and-conversion` | **Date**: 2026-06-11 | **Spec**: [spec.md](./spec.md)

## Summary

Two incremental features built on top of the delivered US1–US3 baseline:

- **Story A** reforms the existing two-step purchased → convert flow into a
  single action: one dialog, one transaction.

- **Story B** introduces `WardrobeTemplate` (system-only, read-only to users)
  and `TemplateSlot` (per-user). Each template ships with a fixed slot
  composition defined in `TemplateSlotDefinitions` — users do not add or remove
  slots. The user selects a template; the system materializes all predefined
  slots automatically on first selection. Only one template is active at a time;
  switching deactivates the previous one (unfulfilled slots are deleted). Extra
  wardrobe items beyond the slot count are always visible in the wardrobe view.
  The frontend renders empty slots as placeholder cards interleaved with owned items.

Delivery is backend-first per project convention, Stories A then B because A
produces the combined conversion endpoint that B's wishlist-link fulfillment
depends on.

## Technical Context

**Language/Version**: TypeScript (React 18+, Vite), C# 12 on .NET 8

**New tables**: `WardrobeTemplates` (system), `TemplateSlotDefinitions` (system),
`TemplateSlots` (per-user). One new column on `Users`: `active_template_id`.

**New domain types**: `WardrobeTemplate` (read-only value), `TemplateSlotDefinition`
(read-only value), `TemplateSlot` aggregate.

**Changed flows**:
- `POST /v1/wishlist-items/{id}/convert` (new combined endpoint) replaces the
  two-call sequence (`PATCH .../purchase` → `POST .../convert`). Old endpoints
  stay alive until Story A frontend ships.
- Wardrobe item creation triggers auto-fulfillment against `TemplateSlots`.
- Wardrobe item deletion triggers slot reversion.
- Template selection triggers slot materialization (`SelectTemplateCommand`).

**Migrations** (per CLAUDE.md naming convention):
- `20260612_AddWardrobeTemplatesAndSlots` — schema: all new tables + the
  `active_template_id` column on `Users`; UNIQUE index on
  `TemplateSlots.wardrobe_item_id`
- `20260612_SeedDefaultTemplates` — data: inserts "Capsula" and "Trabalho"
  with fixed UUIDs and all `TemplateSlotDefinitions` rows; `Down()` deletes them

## Architectural Decisions

- **Templates and definitions are system-only value objects** — `WardrobeTemplate`
  and `TemplateSlotDefinition` have no `user_id` and expose no mutation methods.
  No Create/Rename/Delete/AddSlot commands exist for templates or definitions.
  The repository only exposes `GetAll()` (returning templates with their
  compositions).
- **Slots are the per-user aggregate** — all user-owned state (fulfillment,
  wishlist link) lives on `TemplateSlot`. A slot belongs to one `(user, template,
  category)` tuple but its position within that tuple is determined by the
  definition, not by user action.
- **Materialization as an application command** — `SelectTemplateCommand` handles
  the full switching logic atomically: delete unfulfilled slots from the previous
  template, insert new slots per definition, update `Users.active_template_id`,
  and run initial auto-fulfillment against existing wardrobe items — all in one
  `IUnitOfWork` scope.
- **Auto-fulfillment as an application service** — `TemplateSlotFulfillmentService`
  is called by `CreateWardrobeItemHandler`, `ConvertWishlistItemHandler`, and
  `SelectTemplateCommand` after a successful item write. It queries open slots
  by `(user_id, category)` across all templates and assigns the oldest. This
  keeps the rule in one place.
- **1:1 enforced at DB level** — UNIQUE constraint on `TemplateSlots.wardrobe_item_id`
  guards against concurrent races in addition to the domain invariant.
- **Extra items need no special treatment** — the wardrobe view renders all owned
  items regardless of slot assignment; slots are an overlay, not a filter.
- **Slot-wishlist link is nullable** — set only when the user triggers
  "Adicionar à Lista de Desejos" on an empty slot.
- **Combined conversion endpoint is additive** — keep `PATCH .../purchase` and
  `POST .../convert` alive until Story A frontend ships and replaces them.

## Phase Breakdown

### Phase 1 — Story A Backend: Combined Conversion

Goal: single `POST /v1/wishlist-items/{id}/convert` endpoint that atomically
marks the wishlist item as purchased and creates the wardrobe item.

Steps:
1. Add `WishlistItem.ConvertToWardrobe(...)` domain method — sets `PurchasedAt`,
   returns `WardrobeItemCreationData` value object, rejects if already purchased.
2. Add `ConvertWishlistItemCommand` handling both writes in one `IUnitOfWork` scope.
3. Add `POST /v1/wishlist-items/{id}/convert` endpoint.
4. Write failing tests (unit, integration, contract) first, then implement.

### Phase 2 — Story A Frontend: Conversion Dialog

Goal: single "Converter para Guarda-Roupa" button on wishlist cards that opens
a pre-filled confirmation dialog; "Ver no Guarda-Roupa" in history.

Steps:
1. `ConvertWishlistItemDialog` component (pre-fills from wishlist, validates
   required wardrobe fields).
2. Wire to new `POST .../convert` endpoint.
3. "Ver no Guarda-Roupa" link in wishlist history.
4. Update e2e fixtures.

### Phase 3 — Story B Backend: Templates and Slots

Goal: read-only template+composition query, per-user slot materialization on
selection, auto-fulfillment hook, slot reversion hook, and
"Adicionar à Lista de Desejos" command.

Steps:
1. `WardrobeTemplate` value type (id, name, IReadOnlyList<TemplateSlotDefinition>).
2. `TemplateSlotDefinition` value type (category, quantity) — no mutation methods.
3. `TemplateSlot` aggregate (Fulfill, Unfulfill, LinkToWishlist).
4. `TemplateSlotFulfillmentService` application service.
5. EF Core config + `20260612_AddWardrobeTemplatesAndSlots` migration (includes
   `active_template_id` column on `Users`).
6. `20260612_SeedDefaultTemplates` data migration — inserts templates and all
   `TemplateSlotDefinitions` rows for Capsula (20 slots across 5 categories) and
   Trabalho (9 slots across 3 categories).
7. `IWardrobeTemplateRepository` (GetAll returning templates with compositions)
   + `ITemplateSlotRepository` (query by user/template, insert batch, delete batch).
8. Application queries/commands:
   - `GetTemplatesQuery` — returns all templates with their slot definitions
   - `GetUserSlotsQuery` — returns the active user's materialized slots
   - `SelectTemplateCommand` — materializes slots on selection/switch
   - `LinkSlotToWishlistCommand` — links an open slot to a new wishlist item
9. Hook fulfillment into `CreateWardrobeItemHandler` and `ConvertWishlistItemHandler`.
10. Hook reversion into `DeleteWardrobeItemHandler`.
11. Write failing tests (unit, integration, contract) first, then implement.

### Phase 4 — Story B Frontend: Template UI

Goal: template selector, empty slot placeholders interleaved with owned items,
progress indicator, "Adicionar à Lista de Desejos" slot action. No slot
add/remove controls — the composition is fixed and materialized automatically.

Steps:
1. `wardrobeTemplatesApi.ts` service (GET templates, GET user slots,
   POST select template, POST link-to-wishlist).
2. Template selector dropdown in wardrobe view header ("Sem template" = no
   active template). Selecting a template calls `SelectTemplateCommand` and
   shows a confirmation modal if the user already has an active template
   ("Trocar para X removerá os slots não preenchidos de Y. Continuar?").
3. `TemplateSlotCard` placeholder component (unfulfilled and fulfilled states).
4. `TemplateProgressBar` ("N de M peças adquiridas").
5. Wardrobe view renders slot placeholders interleaved with owned items per
   category; extra items beyond slot count are always visible below.
6. "Adicionar à Lista de Desejos" action on unfulfilled `TemplateSlotCard`.
7. Update e2e tests.

## Constitution Checks

- **Testing Gate**: Failing tests before each implementation phase.
- **Reuse Gate**: `TemplateSlotCard` extends existing card primitives; conversion
  dialog reuses the wardrobe item form.
- **Architecture Gate**: Repository and Result patterns for all new handlers; no
  DbContext in application layer; templates and definitions exposed as read-only
  value objects.
- **DB Versioning Gate**: Two timestamped migrations — schema first, then data.
- **Performance Gate**: Slot materialization is a bounded batch insert (max 20
  rows for Capsula); slot query is `(user_id, template_id)`-indexed;
  auto-fulfillment is a single UPDATE bounded by user's open slot count.
- **Secret Management Gate**: No new secrets.
- **UX Consistency Gate**: All new UI text in pt-BR; placeholder cards follow
  established card design; no slot management controls exposed to users; switching
  confirmation modal guards against accidental data loss.
