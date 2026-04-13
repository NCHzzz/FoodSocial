# Backend Setup Before Identity Plan

> Status: archived reference
> Date: 2026-04-06
> Scope: backend platform setup before Identity Service business implementation

## Goal

Finish the remaining backend platform setup work required before implementing Identity Service business logic.

## Why this plan exists

The repository already has PostgreSQL, RabbitMQ, Redis, Aspire AppHost, BFF, and core backend projects in place.

What is still missing is the platform glue that makes the backend ready for real bounded-service implementation:

- Aspire-managed local auth orchestration
- auth wiring
- persistence bootstrap
- contracts bootstrap
- messaging conventions
- reliability and error conventions
- local runbook discipline

## Affected areas

- `Backend/src/FoodSocial.Bff/`
- `Backend/src/Identity/`
- `Backend/src/FoodSocial.Contracts/`
- `Backend/src/FoodSocial.ServiceDefaults/`
- `Backend/src/FoodSocial.AppHost/`
- `docs/context/current_build_status.md`

## Ordered implementation steps

### 1. Standardize configuration shape

Define stable configuration sections for backend services and BFF:

- `ConnectionStrings`
- `Authentication`
- `Keycloak`
- `Messaging`
- `Caching`
- `Features`

### 2. Wire authentication boundaries

Add Keycloak to the Aspire AppHost as part of the local development platform:

1. add the Aspire-managed Keycloak resource in `FoodSocial.AppHost`
2. use a stable local host port for browser and OIDC consistency
3. enable Keycloak data persistence for local development
4. add a realm import directory under AppHost-managed assets
5. make BFF and Identity.Api wait for local Keycloak startup

Why this belongs in Aspire:

- Keycloak is a core local dependency like PostgreSQL, RabbitMQ, and Redis
- one-command startup reduces local drift and onboarding friction
- local auth behavior should be reproducible from the same orchestration layer as the rest of the backend

### 3. Wire authentication boundaries

Set up the authentication skeleton before Identity business logic:

- realm and client bootstrap path
- BFF authentication middleware
- backend token validation
- explicit authorization entry points

### 4. Bootstrap Identity persistence

Implement the baseline persistence pattern for `Identity` first:

- `DbContext`
- `UseNpgsql(...)`
- migration strategy
- `AddInfrastructure(...)`
- configuration binding

### 5. Replace placeholder contracts

Add the first real contract groups in `FoodSocial.Contracts` for:

- authenticated user responses
- identity mapping or provisioning requests and responses
- early integration event contracts if needed

### 6. Establish messaging conventions

Decide and apply one consistent messaging pattern for:

- publisher registration
- consumer registration
- queue and exchange naming
- retry and dead-letter behavior
- idempotency expectations
- correlation propagation

### 7. Standardize reliability and observability behavior

Build on `FoodSocial.ServiceDefaults` to make runtime behavior consistent:

- correlation IDs
- request logging expectations
- health and readiness expectations
- timeout and retry boundaries

### 8. Standardize error behavior

Define service-wide `ProblemDetails` behavior for:

- validation failures
- unauthorized and forbidden responses
- not-found responses
- conflicts

### 9. Add lightweight runtime controls

Prepare simple feature-toggle support for bootstrap-safe rollout and kill-switch scenarios.

### 10. Write the local runbook

Document how to:

- start Aspire
- verify Keycloak starts through AppHost
- load realm and client configuration
- apply migrations
- verify health endpoints
- verify auth wiring

## Validation expectations

Before this plan is considered complete:

- backend builds cleanly
- Keycloak is AppHost-managed for local development
- BFF auth wiring is present
- Identity persistence wiring is present
- configuration shape is explicit and consistent
- docs/context reflects the new current reality

## Lifecycle note

When this work is completed, blocked, or replaced, update the status above and either archive this file or supersede it with a new plan.
