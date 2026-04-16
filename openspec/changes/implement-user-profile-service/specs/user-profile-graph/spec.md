## ADDED Requirements

### Requirement: Users can follow and unfollow other users
The system SHALL allow an authenticated user to create and remove follow relationships inside the `UserProfile` boundary. Follow relationships MUST be stored idempotently and MUST prevent a user from following themselves.

#### Scenario: Follow relationship is created
- **WHEN** an authenticated user follows another existing user they do not already follow
- **THEN** the system stores the follow relationship and updates the profile-owned follow counts for both sides

#### Scenario: Duplicate follow request remains idempotent
- **WHEN** an authenticated user repeats a follow request for a user they already follow
- **THEN** the system preserves a single follow relationship and does not create duplicates

#### Scenario: Self-follow is rejected
- **WHEN** an authenticated user attempts to follow their own user profile
- **THEN** the system rejects the request with an explicit validation or business-rule error

#### Scenario: Unfollow removes the relationship
- **WHEN** an authenticated user unfollows a user they currently follow
- **THEN** the system removes the follow relationship and updates the profile-owned follow counts accordingly

### Requirement: Follow graph can be queried for profile views
The system SHALL provide follow-graph reads required by MVP profile and social views. These reads MUST include follower lists, following lists, and profile-owned follow counts.

#### Scenario: Follower list can be retrieved
- **WHEN** a caller requests the followers for a user profile
- **THEN** the system returns a bounded list of follower identities shaped for profile or social views together with the current total count

#### Scenario: Following list can be retrieved
- **WHEN** a caller requests the accounts followed by a user profile
- **THEN** the system returns a bounded list of followed identities shaped for profile or social views together with the current total count

### Requirement: Users can save and unsave supported entities
The system SHALL allow an authenticated user to save and unsave posts, dishes, and places inside the `UserProfile` boundary. Saved-item ownership MUST remain local to the user/profile graph service and each saved relationship MUST be unique per user and target.

#### Scenario: User saves a post
- **WHEN** an authenticated user saves a post they have not saved before
- **THEN** the system records a saved-post relationship for that user and target post

#### Scenario: User saves a dish or place
- **WHEN** an authenticated user saves a dish or place they have not saved before
- **THEN** the system records the saved relationship under the same user-owned saved-item boundary

#### Scenario: Duplicate save request remains idempotent
- **WHEN** an authenticated user repeats a save request for the same target
- **THEN** the system preserves a single saved relationship for that user and target

#### Scenario: Unsave removes the saved relationship
- **WHEN** an authenticated user unsaves a previously saved post, dish, or place
- **THEN** the system removes the corresponding saved relationship from the user's saved-item set

### Requirement: Saved items can be retrieved for the owner profile
The system SHALL allow a user's saved items to be retrieved for MVP saved-content views. Saved-item retrieval MUST separate target types clearly enough for downstream profile or BFF composition flows to render saved posts, dishes, and places without guessing target shape.

#### Scenario: Saved items response is grouped by supported target type
- **WHEN** the owner retrieves their saved items
- **THEN** the response includes saved posts, saved dishes, and saved places in explicit target-type groupings or equivalent explicit typed entries

#### Scenario: Non-owner cannot read private saved items without policy
- **WHEN** a caller requests another user's private saved-item view without an approved policy
- **THEN** the system rejects the request with an explicit authorization failure
