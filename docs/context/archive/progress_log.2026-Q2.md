# progress_log.2026-Q2.md

Older milestone entries moved out of `docs/context/progress_log.md` to keep the active context file small.

## 2026-04-06

- Added a dedicated delivery-spec artifact for AI-agent workflow alignment.
- Clarified the repository direction toward separating current-state context from delivery artifacts.
- This unblocked migration of plan-like notes out of `docs/context/`.

## 2026-04-07

- Moved local Keycloak setup into the Aspire-managed backend platform direction.
- Added initial AppHost, BFF, and Identity.Api auth bootstrap code for a local Keycloak-driven development flow.
- This unblocked the next phase of realm/client bootstrap and Identity persistence wiring.

## 2026-04-08

- Added the first local Keycloak realm import with test clients and a demo user.
- Replaced initial Identity and Contracts scaffolding with auth contracts, a claims-based Identity endpoint, and EF Core persistence bootstrap.
- This unblocked the first Identity migration and database update before business logic implementation.

## 2026-04-09

- Completed the browser-facing Keycloak direction at the BFF edge with login, logout, and session endpoints.
- Split local auth validation into browser OIDC through BFF and API token validation through a dedicated CLI client.
- This unblocked end-to-end auth verification aligned with the repository architecture.

## 2026-04-10

- Stabilized the local pre-service platform so AppHost brings up PostgreSQL, Redis, RabbitMQ, and Keycloak together.
- Verified live Keycloak token issuance and BFF login redirect against the imported `foodsocial` realm.
- Completed the first production-path Identity slice across BFF, Identity, UserProfile, and AppHost verification.
- Revalidated browser auth, trusted-service authorization, outbox/bootstrap flow, and backend build/test health after fixing stale Identity wiring and startup sequencing issues.
- Hardened local auth by removing committed local secrets from the active bootstrap path and replacing them with Aspire secret parameters and Keycloak environment variables.
- This unblocked service implementation on top of a working auth, identity, and bootstrap-event foundation.

## 2026-04-11

- Hardened the local platform setup with backend-wide central package version management under `Backend/src/Directory.Packages.props`.
- Reduced auth-edge fragility by centralizing Keycloak claim extraction into shared contract helpers and tightening BFF login redirect handling.
- Updated docs to describe Redis and RabbitMQ at their actual maturity level.

## 2026-04-12

- Upgraded the Identity EF Core stack to EF Core 10.0.5 and Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1 through central package management.
- Verified backend restore and full AppHost build after the EF/Npgsql alignment.
- This removed the earlier EF 9.x versus net10 version mismatch from the active backend setup.

## 2026-04-13

- Added the first Angular auth-aware FoodSocial shell with guest login entry, authenticated logout entry, and BFF session-state wiring.
- Added local Angular dev-server proxy support plus matching local Keycloak callback/origin bootstrap entries.
- This unblocked frontend-authenticated UX work on top of the verified BFF auth edge.

## 2026-04-24

- Added a browser-facing UserProfile facade in the BFF and a typed Angular UserProfile client with a minimal public profile route foundation.
- Hardened BFF session handling for blocked Identity lifecycle states and explicit browser cookie settings.
- Fixed hosted UserProfile startup by passing Keycloak realm/audience configuration through AppHost and hardened RabbitMQ poison-message DLQ handling.
- This unblocked profile-edit flow and launch product vertical slices on top of a verified Identity/UserProfile/BFF foundation.
