# progress_log.md

## Purpose

This file records meaningful implementation progress over time.

Use it to append concise progress notes when the repository materially changes.

## Update Rule

Append an entry when any of the following happens:

- a new service, app, or major module is created
- a major architectural decision is implemented in code
- a vertical slice becomes runnable end to end
- local orchestration or infrastructure setup changes materially
- a major feature moves from planned to implemented

Do not append minor churn, repetitive edits, or every small refactor.

This file is for milestone-level progress only.

## Size Control Rule

- keep only recent and still-useful milestones in this file
- when the log becomes large, move older entries into `docs/context/archive/` using archive files such as `progress_log.2026-Q2.md`
- do not load full historical progress by default when current implementation state is enough

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
