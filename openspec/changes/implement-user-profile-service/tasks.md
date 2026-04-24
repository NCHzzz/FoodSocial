## 1. Domain and persistence foundation

- [x] 1.1 Extend the `UserProfile` domain model beyond `UserProfileShell` to represent MVP profile details, follow relationships, and saved-item ownership.
- [x] 1.2 Update `UserProfileDbContext` and persistence mappings to add additive tables/entities for profile details, follows, saved posts, saved dishes, and saved places while preserving the existing bootstrap shell table.
- [x] 1.3 Generate and review the EF Core migration for the new `UserProfile` schema additions.

## 2. Application services and stores

- [x] 2.1 Add application/store abstractions and persistence implementations for retrieving and updating application-owned profiles.
- [x] 2.2 Add follow-graph application workflows for follow, unfollow, followers, and following reads with idempotency and self-follow protection.
- [x] 2.3 Add saved-item application workflows for saving, unsaving, and retrieving saved posts, dishes, and places with per-user uniqueness rules.

## 3. API surface and authorization

- [x] 3.1 Add `UserProfile.Api` endpoints for profile retrieval and update that return explicit request/response models and standardized failure behavior.
- [x] 3.2 Add `UserProfile.Api` endpoints for follow/unfollow actions plus follower/following list retrieval.
- [x] 3.3 Add `UserProfile.Api` endpoints for owner-scoped saved-item retrieval plus save/unsave operations for posts, dishes, and places.
- [x] 3.4 Enforce explicit ownership and authorization checks for profile mutation and private saved-item access.

## 4. Edge integration and contracts

- [x] 4.1 Add or update shared contracts needed by the new `UserProfile` API surface without moving authentication contracts out of `Identity`.
- [x] 4.2 Wire BFF-side profile composition only where the current frontend path requires edge-shaped access to the new `UserProfile` capabilities.

## 5. Verification and regression coverage

- [x] 5.1 Add unit and integration tests for bootstrap continuity plus profile retrieval/update validation and authorization behavior.
- [x] 5.2 Add tests for follow-graph idempotency, self-follow rejection, saved-item idempotency, and owner-only saved-item reads.
- [x] 5.3 Run the relevant backend test suites, migration verification, and solution build checks for the changed `UserProfile`, contracts, and any BFF paths.
