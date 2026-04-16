## ADDED Requirements

### Requirement: Application profile is owned inside the UserProfile boundary
The system SHALL maintain an application-owned user profile inside the `UserProfile` service for every authenticated identity that completes the trusted bootstrap flow. The profile MUST remain distinct from external authentication state and MUST use the existing bootstrap shell as the local ownership anchor.

#### Scenario: Bootstrap shell becomes the local profile anchor
- **WHEN** the `UserProfile` service receives a trusted identity bootstrap event for an authenticated user
- **THEN** it creates or reuses a local profile anchor tied to the identity id and external subject without requiring the caller to provision profile data directly inside the authentication boundary

#### Scenario: Duplicate bootstrap delivery remains safe
- **WHEN** the same identity bootstrap event is delivered more than once
- **THEN** the `UserProfile` service returns the existing local profile anchor and MUST NOT create duplicate profile ownership records

### Requirement: User profile can be retrieved
The system SHALL allow a user profile to be retrieved by user identifier and SHALL return application-owned profile data needed by MVP profile views. The returned profile MUST include display information, bio, taste-profile fields, and follow counts.

#### Scenario: Profile retrieval returns MVP fields
- **WHEN** a caller retrieves a user profile that exists
- **THEN** the response includes the user's display name, avatar URL when present, bio, manual taste-profile data, follower count, following count, and the identifier of the local user profile record

#### Scenario: Unknown profile retrieval fails explicitly
- **WHEN** a caller retrieves a user profile that does not exist
- **THEN** the system returns a standardized not-found response instead of an empty success payload

### Requirement: User profile can be updated with MVP-safe fields
The system SHALL allow the owning authenticated user to update the MVP-safe profile fields that belong to the `UserProfile` boundary. MVP-safe fields MUST include basic bio, display name, avatar URL, preferred cuisines, disliked ingredients, and dietary restrictions.

#### Scenario: Owner updates manual taste profile
- **WHEN** the owning authenticated user submits a valid profile update request with manual taste-profile changes
- **THEN** the system persists the updated profile fields and returns the updated application-owned profile representation

#### Scenario: Non-owner cannot update another profile
- **WHEN** an authenticated user attempts to update a profile they do not own without an approved administrative policy
- **THEN** the system rejects the request with an explicit authorization failure

#### Scenario: Invalid profile update is rejected
- **WHEN** a profile update request contains invalid field values or violates required validation rules
- **THEN** the system rejects the request with a validation-oriented error response and leaves the existing profile unchanged

### Requirement: Profile views expose authored-content access points without owning post data
The system SHALL expose profile-facing data needed by the MVP profile page while preserving service boundaries. The `UserProfile` service MUST expose identifiers and counts it owns directly, and MUST NOT take ownership of post content data that belongs to the post boundary.

#### Scenario: Profile response preserves post-boundary ownership
- **WHEN** a caller requests a user profile for rendering the MVP profile page
- **THEN** the response includes profile-owned fields and profile-owned counters while leaving authored-post retrieval to the post-facing API flow defined elsewhere
