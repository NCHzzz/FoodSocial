## Context

FoodSocial's product truth treats `Dish` and `Place` as first-class MVP entities. `docs/project-discovery/04_functional_requirements.md` makes place linkage optional for food-first posts, but it also requires search by restaurant and location. `07_domain_model.md` and `10_architecture.md` assign canonical place identity, normalization, and practical PostgreSQL-native area-aware retrieval to the Place boundary.

Dish Foundation now closes the mandatory dish dependency. The next smallest useful dependency for Food Post creation and discovery is a canonical Place foundation that can represent restaurants, home-cooking locations at an appropriate public-safe granularity, and area-aware search inputs without introducing map-heavy or geo-platform complexity.

Constraints that shape the design:

- Place must remain a separate first-class service boundary.
- PostgreSQL is the Place service source of truth.
- Angular should call BFF-facing routes rather than direct service URLs.
- Search must stay PostgreSQL-first, bounded, and structured by restaurant/location fields.
- This change must not introduce Food Post publishing, dedicated geo infrastructure, map UI, AI ranking, or heavyweight search infrastructure.

## Goals / Non-Goals

**Goals:**

- Add canonical Place persistence and lifecycle state.
- Add create, get-by-id, search, and lifecycle status update use cases.
- Keep default search focused on active places.
- Support structured filters for city, area/district, cuisine type, and price range where present.
- Add explicit shared contracts for Place API payloads.
- Add BFF read/search routes for frontend consumption.
- Add a minimal Angular Place client/search-select foundation that can be reused by post creation.

**Non-Goals:**

- Implement Food Post creation or media upload.
- Implement map UI, routing, radius search, or distance sorting beyond storing optional coordinates.
- Implement saved-place ownership; UserProfile owns saved-item relationships and this change only provides canonical Place records.
- Implement vector search, dedicated search infrastructure, AI ranking, or autocomplete enrichment.
- Build an admin frontend for Place management.

## Decisions

### 1. Implement Place as the next service-owned vertical slice

**Decision:** Add the Place foundation inside the existing `Backend/src/Place/*` projects, following the same clean-architecture service layout as Dish.

**Rationale:** The service boundary already exists and the architecture docs explicitly assign canonical place identity to Place. Extending the existing service avoids cross-service leakage and keeps Post creation dependent on a real source-of-truth boundary.

### 2. Use a compact canonical Place model for MVP

**Decision:** Persist Place with id, name, normalized name, description, address, city, area, country, optional latitude/longitude, cuisine types, price range, lifecycle status, and timestamps.

**Rationale:** This covers restaurant/location search and optional post linkage while avoiding premature opening-hours, contact, rating aggregation, map, or partner-portal complexity.

### 3. Keep default search deterministic and PostgreSQL-first

**Decision:** Implement bounded search over normalized name, address, city, area, cuisine types, and price range using EF Core/LINQ-friendly PostgreSQL queries and indexes. Trigram/vector/AI search remain future enhancements unless usage justifies them.

**Rationale:** The architecture requires PostgreSQL-first search and explicitly defers heavyweight search and geo infrastructure. A deterministic search path is enough to support place selection in post creation and basic location-aware discovery.

### 4. Expose service API first, BFF read/search second

**Decision:** Place.Api owns canonical service endpoints, while BFF exposes only browser-facing read/search routes in this change.

**Rationale:** Create/status changes are management operations and do not need an Angular user-facing path yet. Read/search unlock public discovery and post-create place selection without exposing internal service topology.

Management mutation endpoints require an explicit Place management credential in this foundation slice. This keeps lifecycle operations off the public path while avoiding a broader admin UI or role-policy implementation before the management workflow is prioritized.

### 5. Add frontend search/select foundation without full post creation

**Decision:** Add typed Angular transport and a small search/select component, but do not implement publish flow.

**Rationale:** It validates BFF integration and prepares the optional Place selector for Food Post creation while keeping this slice focused.

## Risks / Trade-offs

- **[Risk] Place search quality may be basic at first** → **Mitigation:** keep search deterministic and indexed; add trigram/AI-assisted behavior later behind the search/recommendation direction when needed.
- **[Risk] Duplicate place records may need richer moderation/merge flows later** → **Mitigation:** normalize name and location fields now and keep creation duplicate-safe enough for MVP, while leaving merge workflows out of scope.
- **[Risk] Location privacy can be mishandled if home-cooked posts use exact addresses** → **Mitigation:** Place model supports optional address/coordinates, but post creation can later choose public-safe granularity; this change does not force exact public locations.
- **[Risk] Management authorization may differ from future admin UI needs** → **Mitigation:** keep create/status API policy explicit and service-local; expose only read/search through the BFF now.
- **[Risk] Frontend Place selector may need UX changes during Post creation** → **Mitigation:** keep it small, typed, and reusable rather than designing the full post form prematurely.

## Migration Plan

1. Add Place domain behavior and tests.
2. Add application commands/queries/stores and tests.
3. Add EF Core model, migration, and indexes for the Place-owned database.
4. Add Place.Api endpoints with explicit request/response contracts and validation.
5. Add shared contracts and BFF read/search facade tests.
6. Add Angular Place client/search-select foundation and frontend tests.
7. Update `docs/context/current_build_status.md` and run backend/frontend verification.

Rollback strategy:

- Revert application/API/BFF/frontend code if the feature is not deployed.
- Revert the additive Place migration if no subsequent feature depends on created Place rows.
- Keep the service boundary intact; this change does not alter Identity/UserProfile/Dish/Post data.

## Open Questions

- Should Place creation be limited to admin/internal callers for beta, or eventually allow trusted user-suggested places with moderation?
- Which public-safe location granularity should post creation default to for home-cooked content: area/city only, or no canonical Place linkage?
