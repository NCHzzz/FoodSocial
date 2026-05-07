# progress_log.md

## Purpose

This file records milestone-level implementation progress.

## Update Rule

Append an entry when any of the following happens:

- a new service, app, or major module is created
- a major architectural decision is implemented in code
- a vertical slice becomes runnable end to end
- local orchestration or infrastructure setup changes materially
- a major feature moves from planned to implemented

Do not append minor churn, repetitive edits, or every small refactor.

## Size Control Rule

- keep only recent and still-useful milestones in this file
- when the log becomes large, move older entries into `docs/context/archive/` using archive files such as `progress_log.2026-Q2.md`
- do not load full historical progress by default when current implementation state is enough
- keep only one entry per day unless multiple milestones are materially different and still useful to future work

## Entry Format

```md
## YYYY-MM-DD

- What changed
- Why it matters
- What is unblocked next
```

## 2026-04-06

- Added a dedicated delivery-spec artifact for AI-agent workflow alignment.
- Clarified the repository direction toward separating current-state context from delivery artifacts.
- This unblocks migration of plan-like notes out of `docs/context/`.

## 2026-04-07

- Moved local Keycloak setup into the Aspire-managed backend platform direction.
- Added initial AppHost, BFF, and Identity.Api auth bootstrap code for a local Keycloak-driven development flow.
- This unblocks the next phase of realm/client bootstrap and Identity persistence wiring.

## 2026-04-08

- Added the first local Keycloak realm import with test clients and a demo user.
- Replaced placeholder Identity and Contracts scaffolding with initial auth contracts, a claims-based Identity endpoint, and EF Core persistence bootstrap.
- This unblocks the first Identity migration and database update before business logic implementation.

## 2026-04-09

- Completed the browser-facing Keycloak direction at the BFF edge with login, logout, and session endpoints.
- Split local auth validation into two clear flows: browser OIDC through BFF and API token validation through a dedicated CLI client.
- This unblocks end-to-end auth verification aligned with the repository architecture instead of a JWT-only partial setup.

## 2026-04-10

- Stabilized the local pre-service platform so AppHost brings up PostgreSQL, Redis, RabbitMQ, and Keycloak together.
- Verified live Keycloak token issuance and BFF login redirect against the imported `foodsocial` realm.
- This makes the local platform usable enough to start real service implementation instead of more bootstrap-only work.

## 2026-04-11

- Hardened the local platform setup with backend-wide central package version management under `Backend/src/Directory.Packages.props`.
- Reduced auth-edge fragility by centralizing Keycloak claim extraction into shared contract helpers and tightening BFF login redirect handling.
- Updated docs to describe Redis and RabbitMQ at their actual maturity level instead of implying deeper usage than the current code provides.

## 2026-04-12

- Upgraded the Identity EF Core stack to the current stable EF Core 10.0.5 line with Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1 through central package management.
- Verified the backend still restores and the full AppHost build remains clean after the EF/Npgsql alignment.
- This removes the previous EF 9.x vs net10 version mismatch from the active backend setup.

## 2026-04-10

- Completed the first production-path Identity slice across BFF, Identity, UserProfile, and AppHost verification.
- Revalidated browser auth, trusted-service authorization, outbox/bootstrap flow, and backend build/test health after fixing stale Identity wiring and startup sequencing issues.
- Hardened local auth by removing committed local secrets from the active bootstrap path and replacing them with Aspire secret parameters and Keycloak environment placeholders.
- This unblocks the next service implementation phase on top of a working auth, identity, and bootstrap-event foundation.

## 2026-04-13

- Added the first Angular auth-aware FoodSocial shell with guest login entry, authenticated logout entry, and BFF session-state wiring in the web frontend.
- Added local Angular dev-server proxy support plus matching local Keycloak callback/origin bootstrap entries so browser auth can round-trip through the BFF during frontend development.
- This unblocks frontend-authenticated UX work on top of the verified BFF auth edge instead of relying on backend URLs alone.

## 2026-04-24

- Added a browser-facing UserProfile facade in the BFF and a typed Angular UserProfile client with a minimal public profile route foundation.
- Hardened BFF session handling for blocked Identity lifecycle states and explicit browser cookie settings, then fixed hosted UserProfile startup by passing its Keycloak realm/audience configuration through AppHost and made RabbitMQ bounded-retry poison messages land deterministically in their DLQs.
- This unblocks the next frontend profile-edit flow and the next MVP product vertical slice on top of a verified Identity/UserProfile/BFF foundation.

## 2026-05-07

- Promoted root `DESIGN.md` as the canonical FoodSocial UI/UX design source and left `docs/ai/stack-rules/frontend-ui-unification.md` as a compatibility shim.
- Reworked the Angular UI surface toward the warm editorial design system: parchment canvas, ivory cards, terracotta CTAs, warm-neutral Tailwind classes, serif-led headings, quieter shell, and ring-based shared primitives.
- Rule-compliance review then removed dynamic Tailwind class binding in the Discover session badge and the remaining cool `rose` badge class.
- Verified the frontend with `npm test -- --watch=false` and `npm run build`.
