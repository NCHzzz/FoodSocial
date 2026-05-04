## Why

The MVP requires food-first posts to reference a canonical dish before publishing, but the current build status still marks `Dish` as an earlier-stage service slice. Without a real Dish foundation, post creation, search, discovery, saved dishes, and recommendation work would either need temporary placeholders or later rework.

## What Changes

- Implement the first canonical `Dish` vertical slice across the existing Dish service boundary.
- Add application behavior for creating dishes, retrieving dish details, searching active dishes, and deprecating dishes.
- Persist Dish records in the Dish-owned PostgreSQL database with explicit indexes for the MVP search path.
- Add shared contracts and a BFF read/search facade so Angular can use stable browser-facing Dish APIs.
- Add a minimal Angular Dish client and search/select foundation to prepare the post creation flow without implementing full post publishing in this change.

## Capabilities

### New Capabilities

- `dish-catalog`: canonical Dish ownership, persistence, read/search API, lifecycle handling, and browser-facing read/search facade.

### Modified Capabilities

- None.

## Impact

- Affected code: `Backend/src/Dish/*`, `Backend/src/FoodSocial.Contracts/*`, `Backend/src/FoodSocial.Bff/*`, `Backend/tests/*`, and `Frontend/src/app/*` for the minimal Dish client/search-select foundation.
- Affected APIs: `GET /api/dishes/{dishId}`, `GET /api/dishes/search`, `POST /api/dishes`, `PATCH /api/dishes/{dishId}/deprecate`, plus BFF equivalents for browser read/search.
- Affected systems: PostgreSQL `dishdb`, AppHost service discovery for `dish-api`, and Angular BFF-backed transport code.
- Dependencies and constraints: `Dish` remains its own first-class service boundary, PostgreSQL remains the source of truth, BFF remains the frontend edge, and this change does not implement Place or Post creation.
