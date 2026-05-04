## 1. OpenSpec and scope

- [x] 1.1 Create the `add-dish-foundation` OpenSpec change with proposal, design, tasks, and Dish requirements.
- [x] 1.2 Keep this change scoped to Dish foundation and explicitly defer Place, Post creation, AI ranking, and heavyweight search.

## 2. Domain and application foundation

- [x] 2.1 Add Dish domain model behavior for creation, normalization, lifecycle status, and deprecation.
- [x] 2.2 Add application commands/queries for create, get by id, search, and deprecate.
- [x] 2.3 Add direct domain/application tests for validation, normalization, active/default search behavior, and deprecation.

## 3. Persistence and API

- [x] 3.1 Add Dish EF Core DbContext/model configuration with additive migration and indexes for normalized name, category, cuisine type, and status.
- [x] 3.2 Add Dish.Api endpoints for get by id, search, create, and deprecate using explicit request/response models and ProblemDetails failures.
- [x] 3.3 Add API/persistence tests for create, get, search filters, default deprecated exclusion, and not-found behavior.

## 4. Contracts and BFF edge

- [x] 4.1 Add shared Dish contracts in `FoodSocial.Contracts` for summaries, details, search, create, and lifecycle responses.
- [x] 4.2 Add BFF read/search facade endpoints for Angular-facing Dish access.
- [x] 4.3 Add BFF tests covering forwarding, query preservation, upstream not-found/problem preservation, and safe upstream failure handling.

## 5. Frontend foundation

- [x] 5.1 Add typed Angular Dish models/client that call the BFF read/search routes.
- [x] 5.2 Add a minimal Dish search/select foundation with loading, empty, error, and selected states.
- [x] 5.3 Add frontend tests for the Dish client/search-select behavior.

## 6. Documentation and verification

- [x] 6.1 Update `docs/context/current_build_status.md` with the new Dish implementation state and any remaining blockers.
- [x] 6.2 Run relevant .NET diagnostics/build/tests and frontend tests/build for all changed areas.
