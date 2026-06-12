# Feature Specification: Wardrobe Templates and Combined Wishlist Conversion

**Feature Branch**: `005-wardrobe-templates-and-conversion`

**Created**: 2026-06-11

**Status**: Clarified

## Overview

Two related enhancements that tighten the loop between planning (wishlist) and ownership (wardrobe):

1. **Combined Wishlist → Wardrobe Conversion** — merge the existing two-step
   "mark as purchased → convert" flow (US3) into a single action. The user
   clicks one button, confirms/fills in the wardrobe details via a dialog, and both
   "purchased" and the new wardrobe item are committed in one transaction.

2. **Wardrobe Templates** — system-defined, named checklists of category-level
   "slots" (items the user intends to own). Users choose one of the available
   templates and add category slots to it. Unfilled slots appear as visible empty
   placeholders in the wardrobe view alongside any owned items. Users can have
   more wardrobe items in a category than the template requires — extra items are
   always visible. Slots are auto-fulfilled when a matching wardrobe item is added.

---

## Clarifications

### Session 2026-06-11

- Q: Should conversion bypass "mark as purchased"? → A: No — purchased and
  wardrobe conversion are the same action (one combined flow).
- Q: What happens to the wishlist item after conversion? → A: Keep as
  purchased-history (same as today).
- Q: Handle missing required fields on conversion? → A: Open a fill-in dialog
  for the user to complete and confirm before the wardrobe item is created.
- Q: What defines a slot? → A: Category only (no name or extra fields).
- Q: Single vs multiple templates? → A: Multiple named templates; the user
  selects which template to display.
- Q: How is a slot fulfilled? → A: Automatically when a wardrobe item of the
  matching category is added (first open slot in creation order gets filled).
- Q: Can a slot link to a wishlist item? → A: Yes — converting the linked
  wishlist item auto-fulfills the slot.
- Q: Should empty slots offer "Adicionar à Lista de Desejos"? → A: Yes.
- Q: One-to-one between slot and wardrobe item? → A: Yes, strict 1:1 — one
  wardrobe item per slot, one slot per wardrobe item.
- Q: Can users create custom templates? → A: No — users can only choose from
  the system-defined templates and add slots to them.
- Q: Can users add more wardrobe items than the template requires? → A: Yes —
  extra items in the same category are always visible alongside the template
  slots; there is no cap.

---

## Data Model

### New Tables

```
WardrobeTemplates  (system table — no user_id)
  id     UUID PK
  name   varchar(100) NOT NULL

TemplateSlots  (per-user — one row per slot a user adds to a template)
  id               UUID PK
  template_id      UUID FK → WardrobeTemplates
  user_id          UUID FK → Users (owner of this slot)
  category         WardrobeCategory (enum, NOT NULL)
  wardrobe_item_id UUID FK → WardrobeItems (nullable, UNIQUE — 1:1)
  wishlist_item_id UUID FK → WishlistItems (nullable)
  fulfilled_at     timestamptz (nullable; set when wardrobe_item_id is assigned)
  created_at       timestamptz
```

`WardrobeTemplates` has no `user_id` — templates are global and shared.
Users own only their slots; each slot belongs to one template and one user.

### System Default Templates

Two rows are seeded via a dedicated data migration
(`20260611_SeedDefaultTemplates`):

| id (fixed UUID)                        | name     |
|----------------------------------------|----------|
| `a1000000-0000-0000-0000-000000000001` | Capsula  |
| `a1000000-0000-0000-0000-000000000002` | Trabalho |

Templates are read-only to users — they are never created, renamed, or deleted
through the API.

### Invariants

- A `WardrobeItem` may appear in at most one `TemplateSlot` across all
  templates and users (enforced by UNIQUE constraint on `wardrobe_item_id`).
- A `TemplateSlot` is fulfilled if and only if `wardrobe_item_id IS NOT NULL`.
- Deleting a `WardrobeItem` that fills a slot clears `wardrobe_item_id` and
  `fulfilled_at`, making the slot open again.
- `wishlist_item_id` records which wishlist item is the "plan" for this slot;
  it is cleared when the slot is fulfilled via that item's conversion.
- Users can have more wardrobe items in a category than there are slots for that
  category — extra items are displayed normally and do not need a slot.

### Auto-fulfillment rule

When a wardrobe item is created (including via conversion):

1. Query all open slots (`wardrobe_item_id IS NULL`) owned by the same user
   where `slot.category = wardrobeItem.category`, across all templates.
2. Sort by `created_at ASC` (oldest open slot first).
3. Assign the first result; if none exist, do nothing.

---

## User Stories

### User Story A — Combined Wishlist Conversion (Priority: P2)

As an authenticated user, I can convert a wishlist item into a wardrobe item in
a single action that simultaneously marks it as purchased, so I never have to
repeat data entry or navigate two separate flows.

**Why this priority**: Closes the gap in US3 and is a prerequisite for the
wishlist-linked slot fulfillment in Story B.

**Independent Test**: Create a wishlist item, click "Converter para
Guarda-Roupa", fill the confirmation dialog, and verify: wardrobe item appears,
wishlist item is marked as purchased and hidden from active view, wishlist
history retains the item.

**Acceptance Scenarios**:

1. **Given** a signed-in user with an active wishlist item, **When** the user
   triggers "Converter para Guarda-Roupa", **Then** a dialog pre-filled with
   mapped fields (category, name, brand, target price → price) is shown for
   review/completion.

2. **Given** the conversion dialog with all required fields filled, **When** the
   user confirms, **Then** a wardrobe item is created, the wishlist item is
   marked as purchased, and it is hidden from the active wishlist (visible only
   in history).

3. **Given** the conversion dialog with missing required fields, **When** the
   user attempts to confirm, **Then** inline validation prevents submission until
   all required fields are supplied.

4. **Given** an already-converted (purchased) wishlist item in history, **When**
   the user views wishlist history, **Then** the item shows a "Ver no
   Guarda-Roupa" link to the resulting wardrobe item.

---

### User Story B — Wardrobe Templates (Priority: P3)

As an authenticated user, I can choose one of the available templates, add
category slots to it, see unfilled slots as empty placeholders alongside my
owned items, and track my progress.

**Why this priority**: Depends on Story A being complete so that wishlist
conversion can auto-fulfill template slots.

**Independent Test**: Select "Capsula", add a "Camiseta" slot. Add a T-shirt
to the wardrobe. Verify the slot is auto-fulfilled. Add a second T-shirt and
verify it appears in the wardrobe view even though there is no second slot (extra
items are always shown). Then verify the T-shirt already assigned to the slot
cannot be assigned to another slot (1:1 rule).

**Acceptance Scenarios**:

1. **Given** a signed-in user, **When** the user selects a template (e.g.
   "Capsula") and adds a category slot, **Then** an empty placeholder appears in
   the wardrobe view under that category.

2. **Given** a template with an open slot for category X, **When** the user
   adds or converts a wardrobe item of category X, **Then** the oldest open slot
   is automatically fulfilled and the placeholder is replaced by a link to that
   wardrobe item.

3. **Given** a user with more wardrobe items in a category than there are slots
   for that category, **When** the user views the wardrobe under any template,
   **Then** all extra items are visible alongside (or below) the template slots.

4. **Given** a template slot linked to a wishlist item, **When** the user
   converts that wishlist item, **Then** the slot is auto-fulfilled in the same
   transaction.

5. **Given** an empty slot, **When** the user selects "Adicionar à Lista de
   Desejos", **Then** a new wishlist item pre-filled with the slot's category is
   created and linked to the slot.

6. **Given** a template with multiple slots, **When** the user views the
   wardrobe under that template, **Then** a progress indicator shows how many
   slots are fulfilled (e.g. "3 de 5 peças adquiridas").

7. **Given** a fulfilled slot whose wardrobe item is later deleted, **When** the
   user views the template, **Then** the slot reverts to unfulfilled/open.

---

## Out of Scope (v1)

- User-created, renamed, or deleted templates
- Category slot with name, notes, size, or brand fields (category-only in v1)
- Multiple wardrobe items satisfying one slot (1:1 only)
- Slot reordering within a template
- AI-powered outfit or purchase suggestions based on template gaps
- Template sharing between users
- Per-slot budget tracking separate from wishlist target price
- Drag-and-drop slot management
