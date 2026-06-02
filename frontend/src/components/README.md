# Component Inventory and Reuse Guide

## Core Reusable Building Blocks

- `Form primitives`: labels, inputs, helper text, and validation summary wrappers should be shared before any feature-specific forms are introduced.
- `Card surfaces`: wardrobe cards, wishlist cards, and conversion summaries should share spacing, border, and typography tokens.
- `Upload controls`: image pickers and upload progress indicators must be reused across wardrobe and wishlist flows.

## Reuse Rules

- Search existing `frontend/src/components` modules before creating a new UI primitive.
- Prefer extension through props (variants, labels, slots) rather than cloning a component.
- When a net-new component is necessary, document why existing primitives could not satisfy the flow.

## Planned Shared Components (Phase 2 Baseline)

- `FormField`: field label, required marker, error slot, and hint text container.
- `CurrencyInput`: BRL-friendly input behavior with masked formatting and numeric value extraction.
- `ImageUploadField`: accepted MIME checks (`jpg`, `png`, `webp`), max-size guard, and standardized preview block.
- `EntityCard`: title, metadata list, and action slot used by wardrobe and wishlist list views.

## Governance Notes

- UI copy remains in pt-BR.
- Accessibility requirements (keyboard focus ring, descriptive labels, ARIA associations) apply to every reusable component.
- Components used by multiple features require a short usage example in their module docs.