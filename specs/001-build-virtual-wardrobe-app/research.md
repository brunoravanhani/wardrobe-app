# Research: Virtual Wardrobe and Wishlist Management

## Decision 1: Authentication approach

- Decision: Use Google OAuth/OIDC sign-in in the SPA, then exchange verified identity with backend to issue first-party API session/token.
- Rationale: Meets explicit Google-login requirement while keeping backend authorization under project control for domain operations and ownership checks.
- Alternatives considered: Backend-only Google redirect flow (more coupling with frontend navigation); custom username/password (rejected by requirement).

## Decision 2: Media privacy and access model

- Decision: Store images in a private AWS S3 bucket with Block Public Access enabled and expose owner-scoped access through short-lived presigned URLs issued by backend after ownership validation.
- Rationale: Aligns with requirement that only authenticated owners can access their images, supports direct browser upload/download flows, and reduces API bandwidth for media transfer.
- Alternatives considered: Public object URLs (fails privacy requirement); storing image binaries directly in relational database (higher DB bloat and backup cost); proxying every download through backend (stronger control but higher backend cost and latency).

## Decision 2a: Upload and retrieval handshake

- Decision: Frontend will request a backend-generated S3 presigned upload URL for each image, upload directly to S3, then persist the resulting media asset reference on the business entity. For display, frontend will request a short-lived presigned view URL from backend.
- Rationale: Keeps bucket private while letting the browser transfer files directly to S3 and preserving backend ownership checks.
- Alternatives considered: Multipart form upload through backend (simpler contract but unnecessary backend bandwidth); permanent signed cookies/session proxy (more operational complexity for v1).

## Decision 3: Category governance for v1

- Decision: Enforce fixed predefined categories only in v1 (`T-Shirt`, `Shirt`, `Pants`, `Trousers`, `Shorts`, `Coats`, `Shoes`).
- Rationale: Keeps UI/analytics consistent and avoids taxonomy drift in first release.
- Alternatives considered: Fully custom categories (adds moderation/normalization complexity); fixed + `Other` (less precise reporting and filtering in v1).

## Decision 4: Wishlist budget representation

- Decision: Model wishlist budget as a single target price value.
- Rationale: Clarified requirement favors simple entry and unambiguous validation.
- Alternatives considered: Min/max range (extra validation and UX complexity); optional range endpoints (inconsistent comparisons).

## Decision 5: Purchased-to-wardrobe lifecycle

- Decision: Keep purchased wishlist items as historical records and hide them from active wishlist view by default after conversion.
- Rationale: Preserves purchase history and auditability while minimizing active-list clutter.
- Alternatives considered: Hard-delete after conversion (loss of history); always show in active list (reduced usability).

## Decision 6: API contract style

- Decision: Use OpenAPI-first REST contract for wardrobe, wishlist, media upload handshake, and conversion endpoints.
- Rationale: Enables contract testing and clear integration boundary between SPA and API.
- Alternatives considered: GraphQL (additional server complexity for initial scope); ad-hoc undocumented REST (fails quality/testing gate).

## Decision 7: Testing strategy and fail-first gate

- Decision: Implement unit + integration + e2e + contract tests, requiring failing test proof before implementation and passing after changes.
- Rationale: Directly satisfies constitution and specification testing gates across logic, persistence, and user journeys.
- Alternatives considered: Unit-only approach (insufficient boundary confidence); manual QA only (not acceptable by constitution).
