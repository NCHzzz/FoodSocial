## Context

FoodSocial's product truth treats `Dish` and `Place` as first-class MVP entities. `docs/project-discovery/04_functional_requirements.md` makes canonical dish identity mandatory for food-first posts, while `07_domain_model.md`, `08_database_design.md`, `10_architecture.md`, and `12_backend_structure.md` assign canonical dish identity and normalization to the Dish boundary.

The current implementation has strong Identity/UserProfile/BFF foundations, but `docs/context/current_build_status.md` still identifies Dish and Post as earlier-stage slices. This change closes the most important dependency gap before food post creation: the platform needs stable Dish identifiers and query behavior before posts can require `dishId` honestly.

Constraints that shape the design:

- Dish must remain a separate first-class service boundary.
- PostgreSQL is the Dish service source of truth.
- Angular should call BFF-facing routes rather than direct service URLs.
- Search must stay PostgreSQL-first and bounded.
- This change must not introduce Place, Post publishing, recommendation, vector search, or heavyweight search infrastructure.

## Goals / Non-Goals

**Goals:**

- Add canonical Dish persistence and lifecycle state.
- Add create, get-by-id, search, and deprecate use cases.
- Keep default search focused on active dishes.
- Add explicit shared contracts for Dish API payloads.
- Add BFF read/search routes for frontend consumption.
- Add a minimal Angular Dish client/search-select foundation that can be reused by post creation.

**Non-Goals:**

- Implement Place foundation.
- Implement Food Post creation or media upload.
- Implement saved-dish ownership; UserProfile already owns saved-item relationships and this change only provides canonical Dish records.
- Implement vector search, dedicated search infrastructure, AI ranking, or autocomplete enrichment.
- Build an admin frontend for Dish management.

## Decisions

### 1. Implement Dish as the next service-owned vertical slice

**Decision:** Add the Dish foundation inside the existing `Backend/src/Dish/*` projects, following the current clean-architecture service layout.

**Rationale:** The service boundary already exists and the architecture docs explicitly assign canonical dish identity to Dish. Extending the existing service avoids cross-service leakage and keeps Post creation dependent on a real source-of-truth boundary.

### 2. Use a compact canonical Dish model for MVP

**Decision:** Persist Dish with id, name, normalized name, description, category, cuisine type, aliases, ingredients, dietary flags, lifecycle status, and timestamps.

**Rationale:** This covers the domain-model and database-design fields that matter for the first posting/search dependency while avoiding premature nutrition, enrichment, or provider metadata complexity.

### 3. Keep default search deterministic and PostgreSQL-first

**Decision:** Implement bounded search over normalized name, aliases, category, and cuisine type using EF Core/LINQ-friendly PostgreSQL queries and indexes. Trigram/vector/AI search remain future enhancements unless existing repo patterns already make trigram cheap and safe.

**Rationale:** The architecture requires PostgreSQL-first search and explicitly defers heavyweight search infrastructure. A deterministic search path is enough to support dish selection in post creation.

### 4. Expose service API first, BFF read/search second

**Decision:** Dish.Api owns canonical service endpoints, while BFF exposes only browser-facing read/search routes in this change.

**Rationale:** Create/deprecate are management operations and do not need an Angular user-facing path yet. Read/search unlock public discovery and post-create dish selection without exposing internal service topology.

Management mutation endpoints require an explicit Dish management credential in this foundation slice. This keeps deprecation and creation off the public path while avoiding a broader admin UI or role-policy implementation before the management workflow is prioritized.

### 5. Add frontend search/select foundation without full post creation

**Decision:** Add typed Angular transport and a small search/select component or feature-local foundation, but do not implement publish flow.

**Rationale:** It validates BFF integration and prepares the required Dish selector for Food Post creation while keeping this slice focused.

## Risks / Trade-offs

- **[Risk] Search quality may be basic at first** → **Mitigation:** keep search deterministic and indexed; add trigram/AI-assisted behavior later behind the search/recommendation direction when needed.
- **[Risk] Dish duplication can require richer moderation/curation later** → **Mitigation:** normalize names now and keep creation duplicate-safe enough for MVP, while leaving merge workflows out of scope.
- **[Risk] Management authorization may differ from future admin UI needs** → **Mitigation:** keep create/deprecate API policy explicit and service-local; expose only read/search through the BFF now.
- **[Risk] Frontend Dish selector may need UX changes during Post creation** → **Mitigation:** keep it small, typed, and reusable rather than designing the full post form prematurely.

## Migration Plan

1. Add Dish domain behavior and tests.
2. Add application commands/queries/stores and tests.
3. Add EF Core model, migration, and indexes for the Dish-owned database.
4. Add Dish.Api endpoints with explicit request/response contracts and validation.
5. Add shared contracts and BFF read/search facade tests.
6. Add Angular Dish client/search-select foundation and frontend tests.
7. Update `docs/context/current_build_status.md` and run backend/frontend verification.

Rollback strategy:

- Revert application/API/BFF/frontend code if the feature is not deployed.
- Revert the additive Dish migration if no subsequent feature depends on created Dish rows.
- Keep the service boundary intact; this change does not alter Identity/UserProfile/Post data.

## Open Questions

- Should Dish creation be allowed to trusted authenticated users in beta, or restricted to admin/internal callers until moderation workflows exist?
- Should aliases be user-editable in the first management API, or only persisted for future enrichment?
