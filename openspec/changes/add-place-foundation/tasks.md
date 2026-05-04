## 1. OpenSpec and scope

- [x] 1.1 Create the `add-place-foundation` OpenSpec change with proposal, design, tasks, and Place requirements.
- [x] 1.2 Keep this change scoped to Place foundation and explicitly defer Post creation, map UI, AI ranking, radius/distance ranking, and heavyweight search.

## 2. Domain and application foundation

- [x] 2.1 Add Place domain model behavior for creation, normalization, lifecycle status, and status changes.
- [x] 2.2 Add application commands/queries for create, get by id, search, and status update.
- [x] 2.3 Add direct domain/application tests for validation, normalization, active/default search behavior, location filters, and lifecycle changes.

## 3. Persistence and API

- [x] 3.1 Add Place EF Core DbContext/model configuration with additive migration and indexes for normalized name, city, area, cuisine types, price range, and status.
- [x] 3.2 Add Place.Api endpoints for get by id, search, create, and status update using explicit request/response models and ProblemDetails failures.
- [x] 3.3 Add API/persistence tests for create, get, search filters, default inactive exclusion, duplicate safety, and not-found behavior.

## 4. Contracts and BFF edge

- [x] 4.1 Add shared Place contracts in `FoodSocial.Contracts` for summaries, details, search, creation, and lifecycle responses.
- [x] 4.2 Add BFF read/search facade endpoints for Angular-facing Place access.
- [x] 4.3 Add BFF tests covering forwarding, query preservation, upstream not-found/problem preservation, and safe upstream failure handling.

## 5. Frontend foundation

- [x] 5.1 Add typed Angular Place models/client that call the BFF read/search routes.
- [x] 5.2 Add a minimal Place search/select foundation with loading, empty, error, selected, and stale-response-guard states.
- [x] 5.3 Add frontend tests for the Place client/search-select behavior.

## 6. Documentation and verification

- [x] 6.1 Update `docs/context/current_build_status.md` with the new Place implementation state and any remaining blockers.
- [x] 6.2 Run relevant .NET diagnostics/build/tests and frontend tests/build for all changed areas.
