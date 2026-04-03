# 10_architecture.md

## Status

- Working draft

## Proposed Architecture

Current direction is to design for a responsive web-first MVP.

Approved backend architecture direction: microservices.

## Architecture Decision Summary

The following decisions are the most important architecture anchors for quick review:

- API edge: API Gateway / BFF
- Internal communication: Hybrid REST + async events
- Messaging backbone: RabbitMQ
- Search MVP: PostgreSQL-first lexical search with trigram and structured filters
- Geo-discovery MVP: practical PostgreSQL-native nearby and area-aware retrieval before dedicated geo infrastructure
- AI integration: provider-agnostic abstraction with graceful fallback
- Media handling: object-style storage plus explicit delivery and cleanup lifecycle
- Caching: selective and measured, not a mandatory platform dependency
- Feature control: lightweight runtime controls for rollout gating and kill switches
- Reliability: idempotent consumers with explicit retry, dead-letter, and replay expectations
- Moderation posture: manual-review-first in MVP
- Explicitly deferred: heavyweight search platform, vector-first retrieval, platform-wide cache dependency, heavyweight experimentation tooling

Chosen stack direction:

- .NET 10 backend services
- Python is allowed for AI-oriented services where model-heavy execution justifies it
- Angular frontend
- PostgreSQL owned by each service
- Aspire for local orchestration, composition, and observability support
- multiple AI providers behind an internal abstraction layer
- self-hosted Keycloak as the external identity provider for MVP
- Kubernetes-based deployment target

## Main Components

- responsive web frontend
- API gateway or BFF layer
- identity service
- user/profile graph service
- post and media service
- dish service
- place service
- feed service
- search / recommendation service
- notification service
- AI integration gateway or provider abstraction layer
- Python AI capability services
- moderation / trust service
- media storage
- structured domain and API contracts that remain usable by AI agents

## Data Flow

Initial high-level flow:

1. Users create structured food-first posts
2. Posts are linked to canonical dishes and places when possible
3. The post service stores content while resolving dish/place references through canonical services or shared contracts
4. The feed service retrieves content based on follow graph and discovery rules
5. The search / recommendation service assembles AI-assisted discovery results and may call Python AI capability services for model-heavy execution
6. Notification state is updated from follow and interaction events
7. User interactions create additional ranking and recommendation signals across service boundaries

## Service Responsibility Matrix

- BFF/API Gateway: frontend-facing contracts, auth-aware request shaping, and response aggregation
- Identity service: Keycloak integration, identity mapping, and authentication-related boundary concerns
- User/Profile graph service: profile data, taste preferences, follow graph, and saved-item ownership
- Post and media service: posts, comments, likes, and repost/reshare behavior
- Dish service: canonical dish identity and normalization
- Place service: canonical place identity and normalization
- Feed service: timeline and feed-serving projections
- Search / recommendation service: search retrieval, ranking, recommendation, and AI-assisted discovery behavior
- Notification service: notification center state and retrieval
- Moderation / trust service: report lifecycle, review, moderation outcomes, and auditability
- Python AI services: model-heavy helper capabilities behind internal contracts only

## Why This Architecture

Responsive web first is currently the preferred delivery strategy because it reduces scope compared with launching web and native mobile apps at the same time.

This direction is especially appropriate because the project is currently planned as a solo build with a 2-month target timeline.

The user has explicitly chosen microservices despite the higher delivery and operational risk for MVP.

At the same time, the architecture should preserve the product's AI-native direction by isolating search/recommendation logic so it can evolve independently.

It should also avoid overly UI-centric modeling so that core platform capabilities remain consumable by AI agents in the future.

The approved MVP architecture should optimize for A + B:

- AI-assisted experiences for human users
- structured platform contracts for AI-agent consumption

It should not require full internal multi-agent behavior in the first release.

The chosen API interaction style for MVP is an API Gateway / BFF so the Angular client does not need to integrate directly with multiple backend services.

The chosen internal service communication style is Hybrid REST + async events, which is the most appropriate fit for a social platform using microservices and clean architecture.

### Chosen search architecture

The recommended MVP search architecture is:

- PostgreSQL full-text search as the primary lexical engine
- trigram similarity for typo-tolerant matching on key dish/place fields
- structured filters for dish, place, location, and related food metadata
- AI-assisted query understanding and suggestion generation above deterministic retrieval
- optional later evolution to hybrid search with embeddings if MVP traction justifies it

This approach is chosen to preserve AI-native UX while avoiding a heavyweight dedicated search platform too early.

### Chosen geo-discovery direction

The recommended MVP geo-discovery direction is:

- keep place coordinates and canonical place metadata in the Place boundary
- support practical location queries such as city, area, district, and nearby discovery without introducing a separate geo platform
- use PostgreSQL-native geo-friendly querying patterns that are sufficient for radius, bounding, or distance-aware retrieval if location precision is needed in MVP
- combine geo relevance with lexical relevance and food relevance rather than treating map distance as the only ranking signal
- keep the first release biased toward useful nearby discovery instead of map-heavy exploration UX

This approach keeps location-aware food discovery viable for Vietnam-first launch while avoiding premature dedicated search or mapping infrastructure.

### Chosen multi-provider AI strategy

The recommended MVP AI integration strategy is:

- one internal provider-agnostic AI abstraction or gateway
- capability-based adapters for multiple providers
- config-driven routing and fallback rules
- graceful degradation to deterministic behavior when providers fail or time out
- keep public platform contracts in .NET even if some AI capability services are implemented in Python

### Chosen media storage and delivery direction

The recommended MVP media direction is:

- posts/media owns the application-level media lifecycle for uploads, attachment validation, moderation touchpoints, and deletion orchestration
- binary media should be stored in object-style storage rather than inside PostgreSQL
- the platform should support a clear distinction between original uploads and derived assets such as thumbnails or resized images
- image derivatives may be generated asynchronously when needed, while the MVP should avoid overcomplicated video workflows unless usage proves they matter
- media delivery should support controlled access patterns such as signed URLs, gateway-issued access, or equivalent bounded exposure instead of treating storage as a public free-for-all by default
- media deletion or post deletion must also trigger cleanup expectations for derived assets and storage references

This direction acknowledges that FoodSocial is media-heavy by nature and therefore needs an explicit media-serving model even in MVP.

### Recommendation evolution direction

- MVP recommendation depth: medium personalization
- architecture should preserve a clean upgrade path toward stronger personalization later
- recommendation signals, user-interest data, and read models should be designed to evolve without rewriting core service boundaries

### Chosen caching and read-model direction

The recommended MVP caching direction is:

- treat PostgreSQL and service-owned read models as the default source of truth
- allow selective caching only for measured hotspots such as feed fragments, canonical entity lookups, or expensive AI-assist responses
- avoid making a dedicated cache platform a mandatory dependency before production bottlenecks are proven
- require cache ownership and invalidation expectations to remain explicit per use case

This keeps the architecture operationally lighter for a solo MVP while preserving a path toward targeted acceleration later.

### Feature control and runtime configuration direction

The recommended MVP feature-control direction is:

- use configuration and lightweight feature-flagging for launch gating, beta rollout, kill switches, provider routing, and safety fallback behavior
- keep runtime feature control simple enough that it does not require a heavyweight experimentation platform in MVP
- ensure operationally sensitive toggles such as AI assistance, ranking variants, and moderation rules can be disabled without redeploying all services

This helps a limited beta evolve safely while keeping the architecture compatible with later experimentation needs.

### Honest framing of this choice

Microservices is not the fastest or safest path for this MVP. It is an intentional trade-off that prioritizes explicit service separation and future extraction readiness over delivery simplicity.

Given the current scope and timeline, the architectural priority should still be:

- end-to-end debuggability
- clear domain ownership
- minimal cross-service complexity where possible
- graceful degradation when AI or downstream services fail

## Trade-offs

### Benefits

- clearer service ownership around core domains such as Dish, Place, Feed, and Recommendation
- better long-term alignment with agent-friendly contracts and future extraction needs
- stronger explicit boundaries if the platform grows beyond MVP
- Aspire improves local orchestration, dependency wiring, and observability consistency
- PostgreSQL-first search keeps the search stack simpler for MVP
- an API Gateway / BFF reduces frontend coupling to microservice topology
- Hybrid REST + async events reduces synchronous coupling while preserving simple request-response behavior where needed

### Costs

- not a full native mobile experience at launch
- mobile-specific engagement patterns may be weaker than native apps
- first-class place modeling increases MVP complexity and data management overhead
- first-class dish modeling adds additional complexity in normalization, matching, and search design
- significantly higher operational and coordination overhead for a solo 2-month MVP
- more difficult local development, debugging, observability, and failure handling
- cross-service consistency and contract drift risk
- slower feature iteration during product validation
- multiple AI providers introduce behavior drift, routing complexity, and additional cost controls
- self-hosting Keycloak adds additional operational and deployment burden for a solo MVP

## Environments

Likely required environments if microservices is retained:

- local multi-service development environment
- staging environment
- production environment

Chosen deployment direction: Kubernetes-based deployment.

Aspire should be treated as the default local development composition layer, not as the full production hosting decision by itself.

## Need Confirmation

- Whether microservices means true independently deployable services or a lighter service-shaped deployment model for MVP

## Agent Exposure Direction

- MVP should remain conservative and structured
- long-term direction allows broader external agent actions when authorization and auditing controls are mature enough
- MVP agent-facing APIs should use private or controlled-access exposure rather than public open developer exposure

## Feed Mixing Direction

- Following feed should prioritize followed-user content
- limited mixed content is allowed in Following feed
- Discover remains the primary surface for stronger recommendation behavior

## Delivery Quality Target

- 2-month target: limited real-user beta
- later direction: evolve toward production-ready public launch

Public launch readiness should be evaluated using a combination of product usefulness, technical stability, and operational confidence rather than a single criterion.

Current priority factors for release expansion:

- active and retained real users
- technical stability

## Operational Readiness Direction

The architecture should distinguish between limited beta readiness and broader public-launch readiness.

Minimum beta-ready architecture expectations:

- production-like monitoring for service health, downstream latency, and degraded AI behavior
- at least one documented restore path for each service-owned PostgreSQL datastore that matters to user trust
- documented operator actions for provider outage, queue backlog, media pipeline failure, and degraded search quality
- feature controls that allow high-risk product capabilities to be disabled quickly during beta learning

Broader public-launch readiness should additionally require:

- more formal alert thresholds and operational dashboards
- restore testing discipline rather than backup existence alone
- stronger abuse defenses and confidence that moderation operations can keep up with incoming volume

## Monetization Readiness Direction

- MVP should focus on product validation rather than immediate monetization
- architecture and data contracts should remain extensible for later monetization models such as ads, affiliate/ordering commission, premium features, or mixed revenue strategies
- the primary long-term monetization direction should favor affiliate / ordering commission within a mixed model

## Async Messaging Direction

- chosen event bus for MVP: RabbitMQ
- suitable for moderation triggers, search projection refresh, recommendation signals, and enrichment workflows

### MVP event reliability direction

The architecture should treat async delivery as an explicit reliability concern, not only a transport choice.

Minimum expectations for MVP:

- critical cross-service events should be published through an explicit reliability pattern such as an outbox or another clearly documented equivalent
- consumers must remain idempotent and safe under duplicate delivery
- retries, dead-letter handling, and replay expectations should be documented for workflows that affect user-visible state or moderation-sensitive behavior
- event contracts should evolve additively where possible to reduce cross-service breakage risk

This keeps the microservices choice honest by acknowledging delivery correctness and recovery as part of the design.

## Tenant Direction

Approved direction: multi-tenant.

Architectural implication:

- tenant isolation must be reflected in identity, service contracts, persistence, and moderation workflows
- this improves long-term platform flexibility, but it increases MVP complexity significantly

Current clarification:

- tenant semantics are not yet exercised as a primary MVP business model
- the current intent is to preserve future multi-tenant capability rather than require rich tenant-specific product behavior from day one
- in MVP implementation, tenant should be treated primarily as an architectural isolation concern rather than a rich product-facing workflow driver

## Explicitly Deferred Until Post-Beta Unless Evidence Changes

The following capabilities should remain explicitly deferred unless real usage, product learning, or measured bottlenecks justify them:

- a heavyweight dedicated search platform beyond the approved PostgreSQL-first search approach
- a platform-wide cache dependency introduced before concrete hotspots are measured
- vector-search infrastructure or embedding-first retrieval as a default MVP dependency
- a heavyweight experimentation platform beyond lightweight runtime feature control
- a deeply automated moderation system that contradicts the manual-review-first MVP stance

This deferred list is meant to protect the product from accidental architecture inflation during solo MVP delivery.
