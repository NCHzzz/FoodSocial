# current_build_status.md

## Purpose

This file records the current implementation setup status of the repository.

## Maintenance Rule

Update this file whenever implementation status or current blockers materially change.

This file is a current-state snapshot, not a history log.

- overwrite outdated status instead of appending history
- keep only the current setup state, current blockers, and current priority signals
- remove stale completed details when they no longer help near-term implementation
- keep this file short enough to load as working context without unnecessary token cost

## Current Setup Completed

### Repository and documentation

- project-discovery documentation has been heavily reviewed and cleaned up
- AI operating docs have been simplified and aligned to the repository structure
- architecture, backend, API, and domain docs now reflect the current direction more accurately

### Backend skeleton

- backend solution exists at `Backend/FoodSocial.slnx`
- backend source-of-truth code lives under `Backend/src/`
- the duplicate backend solution folder was removed as a source-of-truth and reduced to leftover Visual Studio artifacts only

### Backend project structure

- `FoodSocial.AppHost`
- `FoodSocial.ServiceDefaults`
- `FoodSocial.Contracts`
- `FoodSocial.Bff`
- `Identity/Api`, `Identity/Application`, `Identity/Domain`, `Identity/Infrastructure`
- `UserProfile/Api`, `UserProfile/Application`, `UserProfile/Domain`, `UserProfile/Infrastructure`
- `Dish/Api`, `Dish/Application`, `Dish/Domain`, `Dish/Infrastructure`
- `Post/Api`, `Post/Application`, `Post/Domain`, `Post/Infrastructure`

### Backend architecture state

- clean architecture layering exists for the first four services
- BFF exists as a separate API edge project
- `ServiceDefaults` is referenced by the API projects
- default WeatherForecast sample files were removed
- Identity now has its first concrete persistence bootstrap with EF Core and PostgreSQL
- FoodSocial.Contracts now contains initial auth and identity boundary contracts

### Local orchestration state

- Aspire AppHost is present
- PostgreSQL resource is configured in AppHost
- logical PostgreSQL databases are configured for:
  - `identitydb`
  - `userprofiledb`
  - `dishdb`
  - `postdb`
- RabbitMQ resource is configured in AppHost
- Redis resource is configured in AppHost
- Keycloak is configured as an Aspire-managed local resource with import-driven realm bootstrap for development

### Authentication bootstrap state

- BFF contains an Aspire Keycloak JWT bearer authentication skeleton
- BFF now also contains the browser-facing Keycloak OpenID Connect login/session/logout edge flow
- Identity.Api contains an Aspire Keycloak JWT bearer authentication skeleton
- service-side auth now relies on Aspire Keycloak resource wiring instead of a hardcoded localhost authority
- Identity.Api exposes an authenticated current-user bootstrap endpoint for claims validation

### Frontend skeleton

- Angular 21 application exists at `Frontend/foodsocial-web`
- TailwindCSS is integrated using the current Angular/Tailwind flow
- frontend folder structure includes:
  - `core/`
  - `shared/`
  - `features/`
  - feature folders for discover, following, create-post, profile, notifications, dishes, and places

### Verification already completed

- backend solution builds successfully
- AppHost builds successfully
- BFF builds successfully after Aspire Keycloak client integration
- Identity.Api builds successfully after Aspire Keycloak client integration
- initial Identity EF Core migration was generated successfully
- initial Identity database update was applied successfully against the live Aspire PostgreSQL container
- AppHost brings up PostgreSQL, Redis, RabbitMQ, and Keycloak successfully for local development
- Keycloak issues access tokens successfully through the imported `foodsocial` realm
- BFF login now redirects correctly to the Keycloak login page through the local auth edge
- frontend Angular app structure exists, but local build still depends on installed frontend packages in the working environment

## Important Clarifications

- Redis is available as a selective cache or short-lived state layer, not a source-of-truth replacement
- PostgreSQL remains the source-of-truth datastore per service
- RabbitMQ remains the async messaging backbone
- Keycloak remains the chosen external identity provider and is now configured as a local Aspire-managed dependency for development

## Current Blockers

- Service-level persistence wiring is still missing for UserProfile, Dish, and Post
- Core contracts, messaging conventions, and runtime reliability conventions are still at scaffold level

## Current Priority

- begin service implementation on top of the now-runnable local platform, starting with the next planned service bootstrap or business logic slice

## Active Delivery Artifacts

- Workflow alignment spec: `docs/delivery/specs/2026-04-06-ai-agent-workflow-alignment-spec.md`
- Backend setup plan: `docs/delivery/plans/2026-04-06-backend-setup-before-identity-plan.md`
- Local auth and identity bootstrap runbook: `docs/delivery/plans/2026-04-07-local-auth-and-identity-bootstrap-runbook.md`

Do not treat this file as an implementation plan. Detailed execution artifacts belong in `docs/delivery/`.
