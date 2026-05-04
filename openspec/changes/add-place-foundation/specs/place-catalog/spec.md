## ADDED Requirements

### Requirement: Place is a canonical service-owned entity
The system SHALL maintain canonical Place records inside the Place service boundary. Place records MUST be distinct from posts, dishes, user profiles, and search/recommendation projections.

#### Scenario: Canonical place is created
- **WHEN** a valid Place create request is accepted
- **THEN** the Place service stores a canonical active Place record with a stable identifier, normalized name, address/location metadata, cuisine types, price range, lifecycle status, and timestamps

#### Scenario: Invalid place name is rejected
- **WHEN** a Place create request omits a name or provides only whitespace
- **THEN** the system rejects the request with a validation-oriented failure and does not persist a Place record

#### Scenario: Duplicate normalized place creation remains safe
- **WHEN** a Place create request uses a name and location scope that normalize to an existing canonical Place in the same service-owned scope
- **THEN** the system MUST NOT create an ambiguous duplicate active Place record

### Requirement: Place can be retrieved by id
The system SHALL allow callers to retrieve a canonical Place by identifier using an explicit Place response contract.

#### Scenario: Existing place is retrieved
- **WHEN** a caller requests an existing Place by id
- **THEN** the response includes the Place id, name, description, address, city, area, country, optional coordinates, cuisine types, price range, lifecycle status, and timestamps

#### Scenario: Unknown place fails explicitly
- **WHEN** a caller requests a Place id that does not exist
- **THEN** the system returns a standardized not-found response instead of an empty success payload

### Requirement: Place search is bounded and PostgreSQL-first
The system SHALL provide bounded Place search over canonical Place data using the Place-owned PostgreSQL store as the MVP source of truth. Search MUST support query text and structured filters for city, area, cuisine type, and price range.

#### Scenario: Active places are searched by query
- **WHEN** a caller searches for Place records with query text matching a name or address field
- **THEN** the system returns a bounded list of active matching Place summaries ordered deterministically

#### Scenario: Search filters by city and area
- **WHEN** a caller supplies city or area filters
- **THEN** the system applies those filters inside the Place service boundary and returns only matching Place summaries

#### Scenario: Search filters by cuisine type and price range
- **WHEN** a caller supplies cuisine type or price range filters
- **THEN** the system applies those filters inside the Place service boundary and returns only matching Place summaries

#### Scenario: Closed or suspended places are excluded by default
- **WHEN** a caller searches Place records without explicitly requesting inactive results
- **THEN** closed or suspended Place records are excluded from the default search results

### Requirement: Place lifecycle supports status changes
The system SHALL allow an authorized management path to mark a Place as active, closed, or suspended without deleting the canonical record.

#### Scenario: Active place is closed
- **WHEN** an authorized caller closes an active Place
- **THEN** the Place status changes to closed and the record remains retrievable by id

#### Scenario: Inactive place is not returned in default search
- **WHEN** a Place has been closed or suspended
- **THEN** default search omits that Place unless the caller uses an explicit include-inactive option

### Requirement: BFF exposes browser-facing Place read/search access
The system SHALL expose browser-facing BFF routes for Place read/search access so Angular does not call the Place service directly.

#### Scenario: BFF forwards Place search
- **WHEN** the Angular app calls the BFF Place search route
- **THEN** the BFF forwards the request to the Place service, preserves supported query parameters, and returns the upstream Place search contract

#### Scenario: BFF preserves Place not-found behavior
- **WHEN** the Place service returns a not-found ProblemDetails response for a Place lookup
- **THEN** the BFF preserves the status and safe problem details for the browser-facing response

### Requirement: Frontend can search and select a Place
The Angular frontend SHALL include a typed Place search/select foundation that uses the BFF Place read/search routes and models loading, empty, error, selected, and stale-response states explicitly.

#### Scenario: User searches for a place
- **WHEN** a user enters a supported place query or location filter in the Place selector
- **THEN** the frontend requests Place search results through the BFF client and displays matching Place summaries

#### Scenario: Search returns no places
- **WHEN** the BFF returns an empty Place result list
- **THEN** the frontend shows an explicit empty state rather than appearing broken

#### Scenario: User selects a place
- **WHEN** the user selects a Place result
- **THEN** the component exposes the selected Place using a typed output that future post creation flow can consume

#### Scenario: Older search responses arrive late
- **WHEN** an older Place search response resolves after a newer search has already started
- **THEN** the frontend ignores the stale response and preserves the newer search state
