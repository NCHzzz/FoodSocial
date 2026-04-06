# Backend Setup Before Identity Plan

> Status: active
> Date: 2026-04-06
> Related spec: `docs/delivery/specs/2026-04-06-ai-agent-workflow-alignment-spec.md`
> Scope: backend platform setup before Identity Service business implementation

## Goal

Finish the remaining backend platform setup work required before implementing Identity Service business logic.

## Why this plan exists

The repository already has PostgreSQL, RabbitMQ, Redis, Aspire AppHost, BFF, and core backend projects in place.

What is still missing is the platform glue that makes the backend ready for real bounded-service implementation:

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

Set up the authentication skeleton before Identity business logic:

- local Keycloak runtime
- realm and client bootstrap path
- BFF authentication middleware
- backend token validation
- explicit authorization entry points

### 3. Bootstrap Identity persistence

Implement the baseline persistence pattern for `Identity` first:

- `DbContext`
- `UseNpgsql(...)`
- migration strategy
- `AddInfrastructure(...)`
- configuration binding

### 4. Replace placeholder contracts

Add the first real contract groups in `FoodSocial.Contracts` for:

- authenticated user responses
- identity mapping or provisioning requests and responses
- early integration event contracts if needed

### 5. Establish messaging conventions

Decide and apply one consistent messaging pattern for:

- publisher registration
- consumer registration
- queue and exchange naming
- retry and dead-letter behavior
- idempotency expectations
- correlation propagation

### 6. Standardize reliability and observability behavior

Build on `FoodSocial.ServiceDefaults` to make runtime behavior consistent:

- correlation IDs
- request logging expectations
- health and readiness expectations
- timeout and retry boundaries

### 7. Standardize error behavior

Define service-wide `ProblemDetails` behavior for:

- validation failures
- unauthorized and forbidden responses
- not-found responses
- conflicts

### 8. Add lightweight runtime controls

Prepare simple feature-toggle support for bootstrap-safe rollout and kill-switch scenarios.

### 9. Write the local runbook

Document how to:

- start Aspire
- start Keycloak
- load realm and client configuration
- apply migrations
- verify health endpoints
- verify auth wiring

## Validation expectations

Before this plan is considered complete:

- backend builds cleanly
- BFF auth wiring is present
- Identity persistence wiring is present
- configuration shape is explicit and consistent
- docs/context reflects the new current reality

## Lifecycle note

When this work is completed, blocked, or replaced, update the status above and either archive this file or supersede it with a new plan.
