## Why

The repo already has the `Identity` and `UserProfile` boundaries plus an identity-driven bootstrap flow, but the `UserProfile` service still stops at creating a minimal shell record. The MVP discovery docs require application-owned profile data, manual taste-profile editing, follow relationships, and saved-item ownership, so this change is needed to turn the partial `UserProfile` slice into the real user/profile graph service the product depends on.

## What Changes

- Expand the existing `UserProfile` service from bootstrap-only shell creation into an MVP-ready profile boundary that owns profile details, follow graph data, and saved-item ownership.
- Add API contracts and endpoints for reading and updating a user's application profile, including the basic bio and manual taste-profile fields approved for MVP.
- Add persistence and application workflows for follow/unfollow behavior and profile-owned saved posts, dishes, and places.
- Expose profile-owned views needed by the wider platform, including follow counts and saved-item retrieval, without moving authentication responsibilities out of `Identity`.
- Preserve the current Keycloak + `Identity` authentication flow and the existing bootstrap event path so authenticated users still receive app-owned profile records through the established boundary.

## Capabilities

### New Capabilities
- `user-profile-management`: application-owned profile creation completion, retrieval, and update behavior for the `UserProfile` service, including basic bio, display fields, and manual taste-profile editing.
- `user-profile-graph`: follow relationships, follow counts, and saved-item ownership for posts, dishes, and places inside the `UserProfile` boundary.

### Modified Capabilities
- None.

## Impact

- Affected code: `Backend/src/UserProfile/*`, `Backend/src/FoodSocial.Contracts/*`, and likely `Backend/src/FoodSocial.Bff/*` for edge-facing composition if profile endpoints are surfaced through the BFF.
- Affected APIs: user/profile endpoints described in `docs/project-discovery/08_api_spec.md`, plus follow and saved-item retrieval flows that depend on the user/profile boundary.
- Affected systems: PostgreSQL `userprofiledb`, RabbitMQ-backed identity bootstrap continuity, and AppHost wiring for the existing `userprofile-api` service.
- Dependencies and constraints: Keycloak remains the external identity provider, `Identity` keeps authentication and account authorization primitives, and `UserProfile` stays responsible only for application-owned profile/graph data.
