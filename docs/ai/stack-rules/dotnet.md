# dotnet.md

## Purpose

This file defines backend engineering rules for .NET 10 microservices in this repository.

## Service and Architecture Rules

- One service owns one business capability and one PostgreSQL database boundary
- Never read another service's database directly
- Keep HTTP, messaging, persistence, and infrastructure concerns outside the application and domain core
- Prefer clear vertical slices over generic abstraction-heavy layering inside a service
- Use synchronous calls only when immediate user-visible results are required; prefer RabbitMQ events for background or eventual-consistency workflows

## API Rules

- Keep frontend composition at the API Gateway / BFF layer
- Use stable, resource-oriented HTTP APIs with predictable request and response shapes
- Return standardized ProblemDetails for API errors
- Validate all external input at the boundary
- Keep endpoints organized by bounded capability instead of building a large catch-all transport layer

## Data and Persistence Rules

- Keep writes inside the owning service boundary
- Use explicit projections for read APIs instead of returning persistence models directly
- Use `AsNoTracking` for read-only queries unless tracking is required
- Avoid lazy loading
- Paginate or cursor every list/feed endpoint intentionally
- Design indexes around real query patterns and verify query performance with PostgreSQL execution plans when needed
- Treat search/read models as rebuildable when source-of-truth data remains authoritative

## Async and Integration Rules

- Use async APIs for all I/O-bound work end-to-end
- Never block on tasks with `.Result`, `.Wait()`, or equivalent sync-over-async patterns
- Pass cancellation tokens through meaningful async boundaries
- Keep event consumers idempotent and resilient to duplicate delivery
- Prefer additive event contract evolution over silent semantic changes

## Observability and Reliability Rules

- Emit structured logs, traces, and metrics from every service
- Use correlation IDs across HTTP and messaging boundaries
- Define timeout, retry, and fallback behavior for downstream calls explicitly
- Expose meaningful health/readiness endpoints
- Keep AI-assisted or non-critical enrichment off the critical path where possible

## Security Rules

- Trust Keycloak for authentication but keep application authorization explicit inside platform services
- Never leak secrets, tokens, internal topology, or stack traces in API responses
- Keep secrets out of code and source control

## Backend Performance Rules

- Prefer efficient projections over loading full graphs by default
- Avoid N+1 query behavior and hidden database round-trips
- Measure before optimizing and use traces, query plans, and metrics to justify performance changes
- Design for graceful degradation when downstream services or AI providers fail
