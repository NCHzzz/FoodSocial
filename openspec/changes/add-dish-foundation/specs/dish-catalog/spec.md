## ADDED Requirements

### Requirement: Dish is a canonical service-owned entity
The system SHALL maintain canonical Dish records inside the Dish service boundary. Dish records MUST be distinct from posts, places, user profiles, and search/recommendation projections.

#### Scenario: Canonical dish is created
- **WHEN** a valid Dish create request is accepted
- **THEN** the Dish service stores a canonical active Dish record with a stable identifier, normalized name, category, cuisine type, lifecycle status, and timestamps

#### Scenario: Invalid dish name is rejected
- **WHEN** a Dish create request omits a name or provides only whitespace
- **THEN** the system rejects the request with a validation-oriented failure and does not persist a Dish record

#### Scenario: Duplicate normalized dish creation remains safe
- **WHEN** a Dish create request uses a name that normalizes to an existing canonical Dish name in the same service-owned scope
- **THEN** the system MUST NOT create an ambiguous duplicate active Dish record

### Requirement: Dish can be retrieved by id
The system SHALL allow callers to retrieve a canonical Dish by identifier using an explicit Dish response contract.

#### Scenario: Existing dish is retrieved
- **WHEN** a caller requests an existing Dish by id
- **THEN** the response includes the Dish id, name, description, category, cuisine type, aliases, ingredients, dietary information, lifecycle status, and timestamps

#### Scenario: Unknown dish fails explicitly
- **WHEN** a caller requests a Dish id that does not exist
- **THEN** the system returns a standardized not-found response instead of an empty success payload

### Requirement: Dish search is bounded and PostgreSQL-first
The system SHALL provide bounded Dish search over canonical Dish data using the Dish-owned PostgreSQL store as the MVP source of truth. Search MUST support query text and structured filters for category and cuisine type.

#### Scenario: Active dishes are searched by query
- **WHEN** a caller searches for Dish records with query text matching a name or alias
- **THEN** the system returns a bounded list of active matching Dish summaries ordered deterministically

#### Scenario: Search filters by category and cuisine type
- **WHEN** a caller supplies category or cuisine type filters
- **THEN** the system applies those filters inside the Dish service boundary and returns only matching Dish summaries

#### Scenario: Deprecated dishes are excluded by default
- **WHEN** a caller searches Dish records without explicitly requesting deprecated results
- **THEN** deprecated Dish records are excluded from the default search results

### Requirement: Dish lifecycle supports deprecation
The system SHALL allow an authorized management path to mark a Dish as deprecated without deleting the canonical record.

#### Scenario: Active dish is deprecated
- **WHEN** an authorized caller deprecates an active Dish
- **THEN** the Dish status changes to deprecated and the record remains retrievable by id

#### Scenario: Deprecated dish is not returned in default search
- **WHEN** a Dish has been deprecated
- **THEN** default search omits that Dish unless the caller uses an explicit include-deprecated option

### Requirement: BFF exposes browser-facing Dish read/search access
The system SHALL expose browser-facing BFF routes for Dish read/search access so Angular does not call the Dish service directly.

#### Scenario: BFF forwards Dish search
- **WHEN** the Angular app calls the BFF Dish search route
- **THEN** the BFF forwards the request to the Dish service, preserves supported query parameters, and returns the upstream Dish search contract

#### Scenario: BFF preserves Dish not-found behavior
- **WHEN** the Dish service returns a not-found ProblemDetails response for a Dish lookup
- **THEN** the BFF preserves the status and safe problem details for the browser-facing response

### Requirement: Frontend can search and select a Dish
The Angular frontend SHALL include a typed Dish search/select foundation that uses the BFF Dish read/search routes and models loading, empty, error, and selected states explicitly.

#### Scenario: User searches for a dish
- **WHEN** a user enters a supported dish query in the Dish selector
- **THEN** the frontend requests Dish search results through the BFF client and displays matching Dish summaries

#### Scenario: Search returns no dishes
- **WHEN** the BFF returns an empty Dish result list
- **THEN** the frontend shows an explicit empty state rather than appearing broken

#### Scenario: User selects a dish
- **WHEN** the user selects a Dish result
- **THEN** the component exposes the selected Dish using a typed output that future post creation flow can consume
