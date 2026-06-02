# 12_backend_structure.md

## Status

- Reviewed working source

## Service Boundaries

Approved architecture direction: microservices with staged extraction.

The backend should represent real product boundaries even when the first implementation keeps some capabilities inside existing services for delivery speed. Runtime extraction should follow measured pressure, ownership clarity, and correctness needs rather than speculative service count.

## Backend Decision Summary

This file describes backend implications of the approved architecture.

Canonical cross-system architecture decisions live in `10_architecture.md`.

Maturity staging lives in `16_maturity_roadmap.md`.

## Chosen Backend Technology Direction

- service implementation: .NET 10
- AI-oriented services may use Python where model-heavy execution is a better fit
- orchestration and local composition: .NET Aspire
- datastore model: PostgreSQL owned by each service
- Redis may be used selectively for cache or short-lived state where justified
- search model: PostgreSQL-first search with derived read models where required
- AI provider support: internal provider-agnostic abstraction over multiple external providers
- messaging backbone: RabbitMQ with outbox/idempotent-consumer patterns for product-critical events

## Boundary Rules

- Core user journeys should avoid unnecessary distributed orchestration.
- Dish and Place ownership must remain explicit to reduce duplication and contract drift.
- Search/recommendation should consume structured contracts and read models rather than directly owning all source-of-truth data.
- Cross-service joins must not happen through direct database access.
- Public browser flows should go through the BFF.
- Dedicated services should be extracted when current ownership makes latency, reliability, moderation, feed ranking, notification delivery, or operator responsibility unclear.

## Service Responsibilities

Identity/auth should own:

- external identity-provider integration
- account lifecycle state
- roles and app-owned authorization state
- trusted service authorization boundaries

User/Profile graph should own:

- profile data
- taste preferences
- saved-item ownership
- follow, block, and mute relationships
- profile/content visibility state
- relationship-state reads for profile, feed, notification, and interaction behavior

Posts should own:

- publishing
- post retrieval primitives
- comments
- likes
- reposts/reshares
- report intake for post-owned targets while moderation is not extracted
- content state such as active, hidden, deleted, and cleanup-pending

Media should own or be prepared to own:

- upload validation
- media metadata
- original and derivative asset references
- delivery policy
- stale-upload cleanup
- deleted-media cleanup
- lifecycle coordination with post deletion and moderation visibility

Dish and Place should own:

- canonical entity records
- normalization and alias rules
- lifecycle state
- food/place search primitives
- location metadata in the Place boundary

Feed should own when extracted:

- following and discover feed-serving projections
- timeline assembly
- fanout-on-write/read strategy
- ranking inputs
- replay/backfill
- cache invalidation
- high-fanout account handling

Search/Recommendation should own when extracted:

- retrieval and ranking
- recommendation generation
- AI-assisted discovery outputs
- search projection refresh
- query understanding and reranking
- explanation payloads

Notifications should own when extracted:

- notification center state
- read-state management
- account/system notifications
- delivery adapters for push, email, or realtime channels

Moderation/trust should own when extracted:

- report lifecycle
- moderation queues
- rule-based abuse checks
- moderation actions and evidence
- audit records
- appeal readiness
- future automation and trust scoring controls

Analytics/events should own when extracted:

- product event taxonomy
- non-blocking event collection
- reporting feeds for activation, retention, search, recommendation, moderation, and media lifecycle

AI integration should remain behind internal contracts and not spread ad hoc across services.

## Validation

Microservice validation should include:

- contract validation between services
- clear fallback behavior when a dependent service is unavailable
- local development workflow validation
- provider-agnostic AI contract validation for structured outputs where AI is used
- event contract validation for product-critical events
- block/mute/visibility behavior across profile, feed, notification, and interaction flows

## Error Handling

Error handling must explicitly define:

- timeout behavior
- retry policy boundaries
- partial failure behavior for feed, search, notification, moderation, media, and recommendation paths
- user-facing fallback responses when downstream services fail
- fallback behavior when an AI provider fails, times out, or returns unusable output
- idempotent behavior for user-triggered writes such as follow, block, mute, like, save, report, and repost

## Auth / Authorization

Approved direction:

- use an external identity provider for authentication
- propagate authenticated identity into service-level authorization decisions
- use self-hosted Keycloak as the external identity provider
- require explicit trusted-service authorization for internal privileged calls
- keep admin and moderator capabilities behind stricter policies than normal user flows
- enforce owner-only and relationship-based authorization for saved items, notifications, profile mutations, block/mute, and moderation-sensitive reads

## Frontend-Facing API Direction

Approved direction:

- use an API Gateway / BFF layer for Angular-facing application APIs

Implications:

- the frontend should not call multiple microservices directly for composite user flows
- gateway/BFF should handle response shaping for feed, search, profile, notifications, moderation, and recommendation scenarios
- authentication context handling is simplified at the edge compared with direct multi-service frontend integration
- BFF should protect cookie-backed state-changing browser endpoints with CSRF checks

The API edge decision itself is canonical in `10_architecture.md`.

## Component Internal Layering

Approved internal shape per backend component:

### Domain services (Identity, UserProfile, Dish, Place, Post)

Domain services use the four-layer Clean Architecture shape described in
`docs/ai/stack-rules/dotnet.md`: `Api/`, `Application/`, `Domain/`, and
`Infrastructure/`. This shape is required because domain services own real
business invariants, lifecycle state, and service-owned persistence. Domain
logic must remain independent of transport, messaging, and persistence
concerns.

### BFF (FoodSocial.Bff)

The BFF is a transport adapter and session aggregator. It owns no domain
invariants, no business rules, and no persistence. For that reason the BFF
**MUST** be organized as **vertical slices by bounded context**, not as a
four-layer shape:

```text
FoodSocial.Bff/
├── Authentication/   # OIDC flow, CSRF filter, request conventions, internal service token
├── Caching/          # Catalog response cache (Redis + memory fallback)
├── Endpoints/        # Facades grouped by bounded context (BffDishEndpoints, BffPostEndpoints, ...)
├── HttpClients/      # Typed downstream clients per service
├── RateLimiting/     # BFF-level fixed-window rate limit policies
├── Recommendations/  # Discover/assist providers and models
├── Results/          # Upstream to browser response translation
└── Security/         # Data protection, security headers
```

Rationale:

- The BFF has no Domain layer because it owns no business invariants.
- Bounded-context folders keep related files together (auth flow, cache,
  rate limit policy, downstream client, endpoints) instead of scattering
  them across four layers.
- BFF endpoints are short, read-mostly facades over downstream service APIs,
  so they read naturally as vertical slices.
- The BFF still follows all other rules in `docs/ai/stack-rules/dotnet.md`
  (ProblemDetails, async end-to-end, environment-driven config, no secrets
  in code, explicit input validation, rate limiting, observability, security
  headers, and tests).

### API Gateway (FoodSocial.ApiGateway)

The API Gateway is a single-purpose edge proxy. It owns YARP cluster config,
security headers, CORS, forwarded-headers, request body limits, correlation
IDs, and edge rate limiting. It is **not** a backend service and has no
business logic. The folder shape is a flat collection of middleware and
configuration helpers; the four-layer shape does not apply.

This layering decision is part of the approved architecture direction; the
canonical cross-system architecture lives in `10_architecture.md`.
## Internal Service Communication Direction

Approved direction:

- Hybrid REST + async events

Implications:

- synchronous REST calls should be limited to interactions that truly require immediate consistency or immediate responses
- async events should be used for background workflows, read-model updates, analytics, notification creation, and non-blocking cross-service reactions
- clean architecture boundaries should keep transport concerns outside application core logic

The system-wide communication decision itself is canonical in `10_architecture.md`.

### Event Reliability Expectations

- workflows that publish user-trust-sensitive events should use a reliability pattern that avoids silent write-then-publish loss
- retry, dead-letter, and replay behavior should be defined for notification creation, moderation, projection refresh, media lifecycle, analytics, and enrichment flows that affect user-visible outcomes
- event handlers must be idempotent and safe under duplicate delivery
- additive event evolution is preferred so service boundaries do not become brittle as the platform grows
- product-critical events should document owner, schema version, producer, consumer, retry policy, DLQ behavior, and replay strategy

## Job Queue / Scheduler

Likely needed for:

- feed projection refresh and timeline backfill
- recommendation refresh or signal processing
- moderation rule checks
- media derivative generation
- media cleanup and deferred deletion tasks
- canonical dish/place enrichment if implemented asynchronously
- search projection refresh and enrichment pipelines
- notification fanout and delivery adapters
- analytics event export or aggregation

Messaging backbone:

- RabbitMQ until throughput, replay retention, partitioning, or long-lived event-stream needs justify heavier streaming infrastructure

## Media and Asset Handling Direction

Approved direction:

- application services should treat binary media as external object-style storage concerns rather than PostgreSQL blob concerns
- posts/media should own attachment validation, media metadata, lifecycle orchestration, and cleanup signaling
- derived assets such as thumbnails should be treated as secondary artifacts that may be regenerated or cleaned up independently of the original asset when the design allows it
- production media storage should use environment-owned object storage, not local filesystem or ephemeral pod storage

Implications:

- media-serving URLs and access rules should be bounded by the platform's delivery model rather than left implicit
- media-processing work should stay off the critical publish path whenever possible
- deletion semantics must account for both metadata records and stored media objects
- CDN integration is a delivery optimization after storage, access, and deletion rules are clear

Canonical media direction lives in `10_architecture.md`; this section exists only for backend implications.

## Feed Backend Direction

Approved direction:

- query-composed feeds may be acceptable for early production beta
- public product readiness should define latency, freshness, and fallback targets for Following and Discover
- dedicated feed service/read models become necessary when BFF/Post composition cannot meet latency, ranking, replay, or ownership needs

Implications:

- feed projections must be rebuildable from source events or source-of-truth data
- fanout strategy should be documented before high-fanout accounts or creator growth makes naive querying unreliable
- ranking inputs should be observable and debuggable

## Geo-Discovery Backend Direction

Approved direction:

- nearby and area-aware discovery should be supported through PostgreSQL-friendly location querying before introducing a separate search or map infrastructure layer
- place retrieval should combine canonical place identity with city, area, and coordinates when available

Implications:

- the Search/Recommendation boundary may consume geo-relevant read models, but Place ownership of canonical location data should remain explicit
- geo relevance should remain one ranking signal among several rather than fully replacing food relevance or lexical relevance

## Feature Control and Runtime Configuration

Approved direction:

- backend capabilities should support lightweight runtime controls for AI assistance, provider routing, rollout gating, moderation rules, and kill switches

Implications:

- feature controls should be explicit enough that operators can disable degraded or risky capabilities without requiring coordinated full redeploys
- config-driven behavior should not become a hidden business-logic layer; important rules must still remain visible in contracts and service code

## Data Lifecycle and Deletion Direction

Approved direction:

- soft-delete, hard-delete, and cleanup behavior should be intentionally differentiated for posts, comments, media references, user relationships, and moderation-sensitive records
- retention-sensitive workflows should be treated as backend capabilities, not only database housekeeping

Implications:

- user deletion and content deletion must be compatible with auditability, moderation history, relationship cleanup, notification cleanup, analytics minimization, and storage cleanup expectations
- rebuildable read models should not become an excuse to leave deletion side effects undefined
- block and mute state should be retained or deleted according to the account deletion and safety policy

## Initial Notes

- recommendation generation may require background processing and dedicated signal stores
- moderation workflows may require admin-only backend modules before a dedicated moderation service exists
- backend contracts should remain structured and explicit enough for future AI-agent compatibility
- rule-based moderation checks may be implemented synchronously or through lightweight background processing, depending on stack choices
- this microservices choice should be documented as a deliberate complexity trade-off, not as the simplest path to delivery
- Aspire should reduce local composition friction, but services must still be designed to survive production realities outside local orchestration
- recommendation-related signals and contracts should be stored in a way that allows later expansion toward stronger personalization without breaking existing APIs
- moderation decision records should be stored in a way that allows future appeal and secondary review workflows without redesigning core moderation entities
- agent-exposed actions should eventually support broader capabilities, but only under explicit authorization, auditing, and policy enforcement boundaries
- product operations benefit from a small number of explicit runbook-worthy failure modes rather than an implicit assumption that infrastructure defaults will be sufficient
- Redis should be treated as an optimization or ephemeral state tool, not as a replacement for PostgreSQL or RabbitMQ
