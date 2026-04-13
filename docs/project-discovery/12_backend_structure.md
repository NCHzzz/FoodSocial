# 12_backend_structure.md

## Status

- Reviewed working source

## Service Boundaries

Approved MVP direction: microservices.

## Backend Decision Summary

This file describes backend implications of the approved architecture.

Canonical cross-system architecture decisions live in `10_architecture.md`.

The canonical system component and service map lives in `10_architecture.md`.

## Chosen backend technology direction

- service implementation: .NET 10
- AI-oriented services may use Python where model-heavy execution is a better fit
- orchestration and local composition: .NET Aspire
- datastore model: PostgreSQL owned by each service
- Redis may be used selectively for cache or short-lived state where justified
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
- user/profile graph should own profile data, taste preferences, saved-item ownership, and follow relationships
- posts/media should own publishing and content retrieval primitives
- dish and place services should own canonical entity records and normalization rules
- feed should assemble user-facing feed results from source services
- search/recommendation should provide AI-assisted discovery and recommendation outputs
- notifications should own notification center state and notification retrieval
- AI integration should be hidden behind internal contracts, not spread ad hoc across services

These notes clarify backend ownership implications; they do not replace the canonical component map in `10_architecture.md`.

Boundary clarification:

- feed owns feed-serving projections and timeline assembly
- search/recommendation owns retrieval, ranking, recommendation, and AI-assisted discovery logic

AI runtime direction:

- keep public business-facing contracts and orchestration in .NET
- allow Python services behind internal contracts for model-heavy AI execution such as query understanding, enrichment, or reranking

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

The API edge decision itself is canonical in `10_architecture.md`.

## Internal service communication direction

Approved direction:

- Hybrid REST + async events

Implications:

- synchronous REST calls should be limited to interactions that truly require immediate consistency or immediate responses
- async events should be used for background workflows, read-model updates, and non-blocking cross-service reactions
- clean architecture boundaries should keep transport concerns outside application core logic

The system-wide communication decision itself is canonical in `10_architecture.md`.

### MVP event reliability expectations

- workflows that publish user-trust-sensitive events should use a reliability pattern that avoids silent write-then-publish loss
- retry, dead-letter, and replay behavior should be defined for moderation, projection refresh, and enrichment flows that affect user-visible outcomes
- event handlers must be idempotent and safe under duplicate delivery
- additive event evolution is preferred so service boundaries do not become brittle as the platform grows

## Job Queue / Scheduler

Likely needed for:

- recommendation refresh or signal processing
- moderation rule checks
- media processing
- canonical dish/place enrichment if implemented asynchronously
- search projection refresh and enrichment pipelines
- cleanup or retention workflows such as derived media cleanup, stale upload handling, or deferred deletion tasks

Messaging backbone for MVP:

- RabbitMQ

## Media and Asset Handling Direction

Approved direction:

- application services should treat binary media as external object-style storage concerns rather than PostgreSQL blob concerns
- posts/media should own attachment validation, media metadata, lifecycle orchestration, and cleanup signaling
- derived assets such as thumbnails should be treated as secondary artifacts that may be regenerated or cleaned up independently of the original asset when the design allows it

Implications:

- media-serving URLs and access rules should be bounded by the platform's delivery model rather than left fully implicit
- media-processing work should stay off the critical publish path whenever possible
- deletion semantics must account for both metadata records and stored media objects

Canonical media direction lives in `10_architecture.md`; this section exists only for backend implications.

## Geo-Discovery Backend Direction

Approved direction:

- nearby and area-aware discovery should be supported through PostgreSQL-friendly location querying before introducing a separate search or map infrastructure layer
- place retrieval should combine canonical place identity with city, area, and coordinates when available

Implications:

- the Search/Recommendation boundary may consume geo-relevant read models, but Place ownership of canonical location data should remain explicit
- geo relevance should remain one ranking signal among several rather than fully replacing food relevance or lexical relevance in MVP

Canonical geo-discovery direction lives in `10_architecture.md`; this section exists only for backend implications.

## Feature Control and Runtime Configuration

Approved direction:

- backend capabilities should support lightweight runtime controls for AI assistance, provider routing, rollout gating, and kill switches

Implications:

- feature controls should be explicit enough that operators can disable degraded or risky capabilities without requiring coordinated full redeploys
- config-driven behavior should not become a hidden business-logic layer; important rules must still remain visible in contracts and service code

Canonical rollout and feature-control direction lives in `10_architecture.md` and `05_non_functional_requirements.md`.

## Data Lifecycle and Deletion Direction

Approved direction:

- soft-delete, hard-delete, and cleanup behavior should be intentionally differentiated for posts, comments, media references, and moderation-sensitive records
- retention-sensitive workflows should be treated as backend capabilities, not only database housekeeping

Implications:

- user deletion and content deletion must be compatible with auditability, moderation history, and storage cleanup expectations
- rebuildable read models should not become an excuse to leave deletion side effects undefined

Canonical lifecycle direction lives in `10_architecture.md`; this section exists only for backend implications.

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
- beta-stage backend operations benefit from a small number of explicit runbook-worthy failure modes rather than an implicit assumption that infrastructure defaults will be sufficient
- Redis should be treated as an optimization or ephemeral state tool, not as a replacement for PostgreSQL or RabbitMQ
