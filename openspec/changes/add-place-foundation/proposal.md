## Why

The MVP requires users to search by restaurant/place and location, and food posts may optionally reference a canonical Place. Dish Foundation now provides the required canonical dish dependency, but `Place` remains an earlier-stage slice. Without a real Place foundation, Food Post creation, location-aware discovery, saved places, and place search would require placeholders or later rework.

## What Changes

- Implement the first canonical `Place` vertical slice across the existing Place service boundary.
- Add application behavior for creating places, retrieving place details, searching active places, and changing lifecycle status.
- Persist Place records in the Place-owned PostgreSQL database with explicit indexes for MVP restaurant/location search paths.
- Add shared contracts and a BFF read/search facade so Angular can use stable browser-facing Place APIs.
- Add a minimal Angular Place client and search/select foundation to prepare optional place linkage in post creation without implementing full post publishing in this change.

## Capabilities

### New Capabilities

- `place-catalog`: canonical Place ownership, persistence, read/search API, lifecycle handling, and browser-facing read/search facade.

### Modified Capabilities

- None.

## Impact

- Affected code: `Backend/src/Place/*`, `Backend/src/FoodSocial.Contracts/*`, `Backend/src/FoodSocial.Bff/*`, `Backend/tests/*`, and `Frontend/src/app/*` for the minimal Place client/search-select foundation.
- Affected APIs: `GET /api/places/{placeId}`, `GET /api/places/search`, `POST /api/places`, `PATCH /api/places/{placeId}/status`, plus BFF equivalents for browser read/search.
- Affected systems: PostgreSQL `placedb`, AppHost service discovery for `place-api`, and Angular BFF-backed transport code.
- Dependencies and constraints: `Place` remains its own first-class service boundary, PostgreSQL remains the source of truth, BFF remains the frontend edge, and this change does not implement Food Post creation, map UI, AI ranking, or heavyweight geo/search infrastructure.
