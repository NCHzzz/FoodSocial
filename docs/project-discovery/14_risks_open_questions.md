# 14_risks_open_questions.md

## Status

- Working draft

## Risks

### R-01 MVP scope is broad

The current MVP includes social posting, feed, search, social graph, AI recommendation, and moderation, which may create delivery risk.

This risk is amplified because the current plan is a solo build with a 2-month target timeline.

### R-02 Product differentiation may be too abstract if trust/authenticity is not operationalized

The promise of more authentic food content needs concrete product and moderation mechanisms.

### R-03 Mixed content model complexity

Food posts, restaurant posts, and recipe posts may require different schemas and UX patterns.

This risk is partially reduced by the clarified content strategy: hybrid model, but Food-first.

### R-04 AI recommendation may be underdefined

Recommendation depth, data inputs, and MVP expectations are not yet fully defined.

### R-05 Feed ranking can become overcomplicated for a solo MVP

The chosen direction includes a separate Discover feed with hybrid ranking. This is good for product quality, but the MVP implementation should stay lightweight.

### R-06 AI-native ambition may exceed solo MVP capacity if interpreted too literally

There is a material difference between:

- an AI-native user experience, and
- a deeply AI-heavy infrastructure from day one

This distinction must be made explicit to keep the 2-month plan realistic.

### R-07 "AI agents should work well with it" is still ambiguous

This could mean very different scopes, such as:

- AI-assisted UX for human users
- external agents consuming platform APIs
- internal agents performing moderation, recommendation, or concierge actions
- autonomous agent participation patterns

These options have very different architectural costs.

This has now been partially clarified:

- MVP priority: A + B
- Later phase: C

### R-08 First-class place modeling increases MVP delivery complexity

Making `Place` a canonical entity in MVP improves long-term discovery and agent compatibility, but it increases implementation scope for post creation, search, and data quality handling.

### R-09 First-class dish modeling further increases MVP delivery complexity

Making `Dish` a canonical entity in MVP strongly supports the Food-first AI-native direction, but it adds complexity in canonical naming, duplicate handling, matching, and post creation UX.

### R-10 Microservices plus PostgreSQL-per-service increases integration overhead

Service-owned databases improve boundary clarity, but they make feed assembly, reporting, search projection, and consistency management harder for a solo builder.

### R-11 Multiple AI providers increase behavior and cost complexity

Provider flexibility improves resilience and future routing options, but it introduces output inconsistency, prompt portability issues, cost control needs, and observability burden.

### R-11B Polyglot AI services increase operational and contract complexity

Using Python for AI-oriented services is a strong technical fit, but it adds another runtime, packaging flow, observability path, and contract boundary to the platform.

### R-12 Self-hosted Keycloak increases operational burden for MVP

Choosing self-hosted Keycloak improves control and reduces vendor dependency, but it adds deployment, upgrade, backup, and operational complexity for a solo builder.

### R-13 API Gateway / BFF adds an extra layer but reduces frontend coupling

The gateway/BFF adds another deployable component, but it is justified because it reduces Angular coupling to microservice topology and centralizes aggregation and auth-aware edge behavior.

### R-14 Kubernetes deployment adds significant operational complexity for MVP

Kubernetes fits a microservices direction structurally, but it increases infrastructure, deployment, observability, and troubleshooting overhead for a solo builder on a 2-month timeline.

### R-15 Multi-tenant architecture increases isolation and operational complexity

Choosing multi-tenant architecture adds complexity to identity, authorization, data partitioning, moderation, analytics, and service contracts for MVP.

### R-16 Broad external agent actions increase security and audit complexity

Allowing external AI agents to perform wider system actions in the long term increases the need for strong authorization, action scoping, auditability, and abuse prevention.

### R-17 Media lifecycle complexity is easy to underestimate

Media-heavy user-generated products require more than upload support. Storage cleanup, derivative handling, moderation visibility, and deletion behavior can create hidden scope if not documented early.

### R-18 Async messaging correctness can fail silently without explicit reliability rules

RabbitMQ is a practical MVP choice, but event publication, retries, dead-letter behavior, and replay expectations must stay explicit or the architecture may look simpler on paper than it is in production.

### R-19 Geo-discovery expectations may exceed the current search definition

The product promise includes dish, place, and location-aware discovery. If nearby relevance is important, the MVP search model must still define practical geo-query behavior even without a dedicated geo/search platform.

### R-20 Beta operations may remain underdefined

The current docs acknowledge monitoring, backups, and graceful degradation, but a limited beta still needs a clearer minimum operator model for outages, queue backlog, degraded AI behavior, and restore confidence.

## Unknowns

- timeline
- budget
- exact meaning of "AI-native" in MVP scope
- exact production hardening plan after limited beta stage
- exact threshold for introducing dedicated cache, vector, or heavier search infrastructure

## Current Clarifications

- dish identity is required at publish time for food posts
- saved content in MVP includes posts, dishes, and places
- MVP agent-facing APIs use private or controlled-access exposure

## Release Readiness Direction

- moving from limited beta to broader public release should be a multi-factor decision
- the most important factors are active/retained real users and technical stability

## Agent Capability Direction

- long-term direction: broader external agent actions are allowed if properly authorized
- MVP should remain more conservative than the long-term target
- MVP API exposure should stay private or controlled-access rather than fully public
- exact real-world meaning of tenant beyond future extensibility intent

## Clarified Constraints

- Timeline: 2 months
- Delivery model: solo builder
- Development approach: AI-assisted / vibe coding

## Assumptions Needing Validation

- responsive web first is acceptable for MVP
- native mobile apps can wait
- ordering integration is not part of MVP
- AI recommendation in MVP can start simple
- moderate trust controls are sufficient for MVP if later-phase stronger trust mechanisms are planned
- first-class place modeling is worth the added MVP complexity because place-aware discovery is important to the product vision
- first-class dish modeling is worth the added MVP complexity because dish-aware discovery is central to the Food-first AI-native vision
- PostgreSQL-first search is a sufficient MVP compromise before dedicated search infrastructure is considered
- a thin internal AI provider abstraction is enough for MVP multi-provider support
- self-hosted Keycloak is acceptable only if the product owner prioritizes control over lower operational burden
- Kubernetes deployment is acceptable only if the product owner intentionally accepts higher operational burden in exchange for deployment standardization
- multi-tenant architecture is acceptable only if the product owner intentionally accepts higher isolation and data-partitioning complexity in MVP
- current tenant decision is future-oriented and should not force unnecessary MVP workflow complexity before tenant semantics are defined
- RabbitMQ is the most practical async messaging choice for MVP compared with heavier event-streaming infrastructure
- MVP can defer monetization only if later monetization extensibility is preserved in domain and platform contracts
- mixed monetization is acceptable because affiliate / ordering commission is the clearest primary long-term direction
- no appeal flow in MVP is acceptable only if moderation records remain extensible enough for future appeal workflows
- manual-review-first moderation is acceptable for MVP only if future automation hooks remain possible
- Vietnam-first launch scope is acceptable for MVP and reduces geographic modeling ambiguity
- limited mixing in Following feed is acceptable only if UX and ranking keep the follower-priority clear
- MVP can defer vector-search infrastructure unless real semantic retrieval needs or proven search-quality gaps justify it
- selective caching is sufficient for MVP if cache ownership and invalidation stay explicit per use case rather than becoming an implicit platform crutch

## Decision Log Candidates

- content modeling strategy
- recommendation approach
- moderation strategy
- platform rollout strategy
- search implementation strategy
- discover feed ranking strategy
- media storage and delivery strategy
- event reliability strategy
- deletion and retention strategy
- AI-native implementation depth for MVP
