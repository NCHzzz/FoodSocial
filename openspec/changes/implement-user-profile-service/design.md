## Context

The backend already contains a real `UserProfile` service boundary at `Backend/src/UserProfile/*`, but its current implementation is intentionally narrow. `UserProfileDbContext` only stores `user_profile_shells`, `UserProfileBootstrapService` only creates a minimal shell from `UserProvisionedIntegrationEvent`, and the background consumer publishes a completion event back to `Identity` once that shell exists.

The approved product and architecture docs assign a much broader MVP responsibility to the user/profile graph boundary. `docs/project-discovery/04_functional_requirements.md` requires profile pages, saved items, follow counts, and manual taste-profile editing. `docs/project-discovery/10_architecture.md` and `12_backend_structure.md` assign profile data, taste preferences, follow relationships, and saved-item ownership to the `UserProfile` boundary while keeping authentication and account-level authorization primitives inside `Identity` and Keycloak.

This change therefore extends an existing, partially built service rather than introducing a new generic `UserService`. The main stakeholders are backend developers implementing the next service slice, the BFF layer that may compose profile-facing flows, and downstream frontend profile/saved-content experiences that depend on stable user-profile APIs.

Constraints that shape the design:
- Keycloak remains the external authentication provider.
- `Identity` remains the boundary for subject mapping, lifecycle state, and trusted identity ensure.
- `UserProfile` owns only application-specific profile and graph data.
- PostgreSQL remains the service-owned source of truth.
- Existing RabbitMQ bootstrap flow must keep working without introducing write-then-publish gaps.

## Goals / Non-Goals

**Goals:**
- Expand `UserProfile` from shell bootstrap into the MVP user/profile graph service.
- Preserve the existing identity bootstrap handshake and use it as the creation path for local user-profile ownership.
- Add profile retrieval and update behavior for basic display and manual taste-profile fields.
- Add follow/unfollow behavior, follow-list retrieval, and saved-item ownership for posts, dishes, and places.
- Keep the service boundary explicit so profile data remains local while authentication remains externalized through `Identity` and Keycloak.

**Non-Goals:**
- Replacing Keycloak or moving authentication into `UserProfile`.
- Moving authored post ownership, feed assembly, notifications, or moderation state into `UserProfile`.
- Designing a full recommendation-signal engine, social ranking model, or cross-service graph query engine.
- Introducing a new platform-wide cache, search system, or generalized event-sourcing pattern for this slice.

## Decisions

### 1. Evolve the existing bootstrap shell into a richer profile aggregate root

**Decision:** Keep the existing `UserProfileShell` bootstrap concept, but extend the `UserProfile` boundary with additional domain entities and persistence tables for profile details, follows, and saved items instead of replacing the current bootstrap path.

**Rationale:**
- The current shell already provides the trusted local anchor keyed by `IdentityId` and `Subject`.
- Reusing that anchor avoids a second provisioning path and preserves idempotent bootstrap behavior already implemented in `UserProfileBootstrapService`.
- It keeps migrations additive: the current `user_profile_shells` table remains valid while new tables are added around it.

**Alternatives considered:**
- **Create a brand-new `User` aggregate and discard the shell concept:** rejected because it duplicates the existing verified bootstrap flow and increases migration complexity.
- **Move profile bootstrap into `Identity`:** rejected because discovery docs explicitly separate auth/identity from profile graph ownership.

### 2. Keep a single `UserProfile` service boundary for profile, follow graph, and saved items

**Decision:** Implement profile details, follow relationships, and saved-item ownership inside the existing `UserProfile` service instead of splitting these into separate services.

**Rationale:**
- `docs/project-discovery/13_backlog_mvp.md` explicitly says profile ownership, follow graph, taste preferences, and saved-item ownership should stay together in the same service boundary in MVP.
- This keeps the early microservice count from growing beyond what the current MVP needs.
- It reduces cross-service coordination for common profile views.

**Alternatives considered:**
- **Separate follow graph into its own social-graph service:** rejected as premature for MVP.
- **Store saved items in Post/Dish/Place services:** rejected because the architecture assigns saved-item ownership to the user/profile graph boundary.

### 3. Model saved items as typed relationship tables, not polymorphic opaque blobs

**Decision:** Persist saved posts, saved dishes, and saved places as separate tables or equivalent explicit relational sets inside `UserProfile`.

**Rationale:**
- `docs/project-discovery/08_database_design.md` already points toward `saved_posts`, `saved_dishes`, and `saved_places` with per-user uniqueness constraints.
- Separate tables keep constraints explicit and queries simple.
- This avoids a weakly typed generic saved-target model that would make validation and retrieval harder.

**Alternatives considered:**
- **Single polymorphic saved-item table with target type and target id:** possible, but less aligned with current schema direction and stricter relational integrity.

### 4. Expose UserProfile-owned APIs directly, then compose through the BFF only where necessary

**Decision:** The `UserProfile` API should expose resource-oriented endpoints for profile retrieval/update, follow graph reads/mutations, and saved-item reads/mutations. The BFF may compose these later for frontend-specific shaping, but the service should own the canonical backend contracts.

**Rationale:**
- The discovery docs already define `/api/users/*`-style capabilities.
- The current codebase uses service-local APIs plus BFF orchestration where edge-specific behavior is needed.
- Direct service-owned contracts keep the service independently testable and maintainable.

**Alternatives considered:**
- **Implement profile behavior only inside the BFF:** rejected because it would leak service ownership into the edge and weaken service contracts.

### 5. Use explicit authorization rules based on authenticated identity ownership

**Decision:** Read operations for public profile data can be broader, but mutation of profile-owned data and private saved-item retrieval must require ownership checks derived from the authenticated identity. Cross-user mutation is out of scope unless an explicit administrative policy is added.

**Rationale:**
- `Identity` already provides authenticated identity ids and trusted bootstrap behavior.
- The backend rules require explicit authorization boundaries.
- Saved-item privacy and profile mutation are trust-sensitive user data paths.

**Alternatives considered:**
- **Infer authorization implicitly from route shape:** rejected because it is brittle and obscures trust boundaries.

### 6. Keep notification and feed effects outside the initial service slice

**Decision:** This change will implement the core profile graph writes first and treat notification/feed reactions as downstream concerns, potentially through later events or later BFF/service composition.

**Rationale:**
- The approved responsibility matrix gives notifications and feed-serving to other services.
- Attempting to complete those cross-service flows in the same slice would expand the change beyond the user request.

**Alternatives considered:**
- **Emit full notification-trigger events as part of this change:** deferred until the notification contract path is explicitly prioritized.

## Risks / Trade-offs

- **[Risk] Existing shell-only schema may make aggregate evolution awkward** → **Mitigation:** keep migrations additive, retain `user_profile_shells` as the bootstrap anchor, and add new tables/entities around it rather than rewriting the bootstrap table first.
- **[Risk] Saved-item relationships reference entities owned by other services without local FK enforcement** → **Mitigation:** store external ids as explicit references, validate shape at API boundaries where appropriate, and keep ownership semantics local without cross-service database coupling.
- **[Risk] Route naming could drift between BFF-facing `/api/users/*` language and service-local controller structure** → **Mitigation:** keep service contracts resource-oriented and let the BFF perform edge-specific reshaping only when needed.
- **[Risk] Follow/saved-item behavior may eventually need downstream events for notifications or feed refresh** → **Mitigation:** design application services and persistence boundaries so additive event publication can be introduced later via explicit outbox-backed integration events.
- **[Risk] Public-versus-private profile retrieval rules are not fully documented for every field** → **Mitigation:** default to a conservative split where core profile identity is readable and saved items remain owner-only until future product truth expands visibility.

## Migration Plan

1. Extend the `UserProfile` domain and EF Core model with profile-detail, follow, and saved-item entities.
2. Generate and apply additive `UserProfile` migrations against `userprofiledb`.
3. Add application services and stores for profile updates, follow graph operations, and saved-item operations.
4. Add API controllers/endpoints with explicit validation and authorization.
5. Extend tests to cover bootstrap continuity, profile CRUD behavior, authorization failures, follow idempotency, and saved-item idempotency.
6. If BFF profile composition is needed immediately, add edge wiring after service contracts are verified.

Rollback strategy:
- Revert the deployment and database migration for newly added tables if the change has not been depended on by subsequent releases.
- Preserve the original shell bootstrap path so the verified identity bootstrap flow remains intact even if richer profile features are rolled back.

## Open Questions

- Should profile retrieval be keyed primarily by `IdentityId`, local profile id, or a public-facing user id abstraction at the service API boundary?
- Should avatar URL remain a simple stored field in MVP, or wait for a later media-owned upload flow while exposing it as nullable now?
- Should follow and saved-item writes emit integration events in this slice, or should that be deferred until notification/feed work is explicitly scheduled?
- How much of the `/api/users/*` API surface should be exposed directly by `UserProfile.Api` versus composed by the BFF in the first implementation pass?
