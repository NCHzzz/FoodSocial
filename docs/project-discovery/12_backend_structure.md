# 12_backend_structure.md

## Status

- Working draft

## Service Boundaries

Approved MVP direction: microservices.

Recommended initial service boundaries:

- identity and auth
- profiles and social graph
- posts and media
- dishes
- places
- feed and discovery
- search and recommendation
- moderation and trust

## Chosen backend technology direction

- service implementation: .NET 10
- orchestration and local composition: .NET Aspire
- datastore model: PostgreSQL owned by each service
- search model: PostgreSQL-first search with derived read models where required
- AI provider support: internal provider-agnostic abstraction over multiple external providers

## Service Boundary Notes

- The initial service count should be kept as low as possible even under a microservices strategy.
- Core user journeys should avoid unnecessary distributed orchestration.
- Dish and Place ownership must remain explicit to reduce duplication and contract drift.
- Search/recommendation should consume structured contracts rather than directly owning all source-of-truth data.
- Cross-service joins should not happen through direct database access.

## Service Responsibilities

Moderation/trust responsibilities should include:

- receiving user reports
- applying basic rule-based abuse checks
- exposing moderation queues for admin review
- recording moderation actions for auditability

Additional cross-service responsibility notes:

- identity/auth should own authentication and account-level authorization primitives
- posts/media should own publishing and content retrieval primitives
- dish and place services should own canonical entity records and normalization rules
- feed should assemble user-facing feed results from source services
- search/recommendation should provide AI-assisted discovery and recommendation outputs
- AI integration should be hidden behind internal contracts, not spread ad hoc across services

## Validation

Microservice validation should include:

- contract validation between services
- clear fallback behavior when a dependent service is unavailable
- local development workflow validation
- provider-agnostic AI contract validation for structured outputs where AI is used

## Error Handling

Error handling must explicitly define:

- timeout behavior
- retry policy boundaries
- partial failure behavior for feed and recommendation paths
- user-facing fallback responses when downstream services fail
- fallback behavior when an AI provider fails, times out, or returns unusable output

## Auth / Authorization

Approved direction:

- use an external identity provider for authentication
- propagate authenticated identity into service-level authorization decisions
- use self-hosted Keycloak as the external identity provider for MVP

Need confirmation:

- whether service-to-service authorization needs to exist in MVP or can stay lightweight

## Frontend-facing API direction

Approved direction:

- use an API Gateway / BFF layer for Angular-facing application APIs

Implications:

- the frontend should not call multiple microservices directly for composite user flows
- gateway/BFF should handle response shaping for feed, search, profile, and recommendation scenarios
- authentication context handling is simplified at the edge compared with direct multi-service frontend integration

## Internal service communication direction

Approved direction:

- Hybrid REST + async events

Implications:

- synchronous REST calls should be limited to interactions that truly require immediate consistency or immediate responses
- async events should be used for background workflows, read-model updates, and non-blocking cross-service reactions
- clean architecture boundaries should keep transport concerns outside application core logic

## Job Queue / Scheduler

Likely needed for:

- recommendation refresh or signal processing
- moderation rule checks
- media processing
- canonical dish/place enrichment if implemented asynchronously
- search projection refresh and enrichment pipelines

Messaging backbone for MVP:

- RabbitMQ

## Initial Notes

- recommendation generation may eventually require background processing
- moderation workflows may require admin-only backend modules
- backend contracts should remain structured and explicit enough for future AI-agent compatibility
- MVP backend design should explicitly prioritize user-facing AI support and agent-friendly interfaces before internal agent orchestration
- rule-based moderation checks may be implemented synchronously or through lightweight background processing, depending on stack choices
- this microservices choice should be documented as a deliberate complexity trade-off, not as the simplest path to MVP
- Aspire should reduce local composition friction, but services must still be designed to survive production realities outside local orchestration
- recommendation-related signals and contracts should be stored in a way that allows later expansion toward stronger personalization without breaking existing APIs
- moderation decision records should be stored in a way that allows future appeal and secondary review workflows without redesigning core moderation entities
- agent-exposed actions should eventually support broader capabilities, but only under explicit authorization, auditing, and policy enforcement boundaries
