# 10_architecture.md

## Status

- Reviewed working source

## Architecture Direction

FoodSocial is a production-bound, food-first social platform. It should be documented and built as a real product with staged maturity, not as a disposable MVP.

Approved backend architecture direction: microservices with pragmatic service extraction. The codebase may initially group some responsibilities where this reduces delivery risk, but the product architecture must keep boundaries explicit enough to extract Feed, Search/Recommendation, Notification, Moderation, and Media services when ownership, scale, or correctness justifies it.

Maturity staging from beta to public product to scale lives in `16_maturity_roadmap.md`.

## Architecture Decision Summary

The following decisions are the most important architecture anchors for quick review:

- API edge: API Gateway / BFF
- Internal communication: Hybrid REST + async events
- Messaging backbone: RabbitMQ until throughput or replay needs justify heavier streaming infrastructure
- Search: PostgreSQL-first lexical search with trigram, full-text, structured filters, and a staged path to dedicated search
- Geo-discovery: practical PostgreSQL-native nearby and area-aware retrieval before dedicated geo/search infrastructure
- AI integration: provider-agnostic abstraction with strict timeouts and deterministic fallback
- Media handling: object-style storage plus explicit delivery, derivative, deletion, and cleanup lifecycle
- Caching: selective and measured, not a platform-wide source of truth
- Feature control: lightweight runtime controls for rollout gating, provider routing, moderation rules, and kill switches
- Reliability: outbox-backed publishing where needed, idempotent consumers, and explicit retry, dead-letter, replay, and operator response
- Moderation posture: manual-review-first for enforcement decisions until automation has review, appeal, audit, and rollback controls
- Social safety: follow, block, mute, visibility, rate-limit, and report flows are core social controls

Chosen stack direction:

- .NET 10 backend services
- Python is allowed for AI-oriented services where model-heavy execution justifies it
- Angular frontend
- PostgreSQL owned by each service
- Aspire for local orchestration, composition, and observability support
- multiple AI providers behind an internal abstraction layer
- self-hosted Keycloak as the external identity provider
- Kubernetes-based deployment target
- Redis as selective cache or short-lived state, not source of truth

## Main Components

- responsive web frontend
- API gateway or BFF layer
- identity service
- user/profile graph service
- post service
- media lifecycle capability, initially inside Post and extractable later
- dish service
- place service
- feed capability or service
- search/recommendation capability or service
- notification capability or service
- AI integration gateway or provider abstraction layer
- optional Python AI capability services behind internal contracts
- moderation/trust capability or service
- object media storage
- analytics/event pipeline capability
- structured domain and API contracts that remain usable by AI agents

## Data Flow

High-level product flow:

1. Users create structured food-first posts linked to canonical dishes and optional canonical places.
2. Post/media stores content metadata and media lifecycle state while binary media lives in object-style storage.
3. User/Profile owns profile, saved items, follow, block, mute, and visibility relationship state.
4. Feed assembly uses source-service data first, then evolves toward feed-serving projections and timelines when scale or ranking complexity requires it.
5. Search/Recommendation assembles deterministic search, AI-assisted suggestions, ranking, and recommendation outputs through structured contracts.
6. Notification state is created from follow, interaction, moderation, and account/system events.
7. Moderation receives reports, records decisions, preserves evidence, and controls enforcement actions.
8. Analytics captures product and operational events without blocking core user paths.

## Service Responsibility Matrix

- BFF/API Gateway: browser-facing contracts, auth/session orchestration, CSRF enforcement, request shaping, aggregation, and safe ProblemDetails responses
- Identity service: Keycloak integration, identity mapping, lifecycle state, roles, and authentication-related boundaries
- User/Profile graph service: profile data, taste preferences, saved items, follow/block/mute relationships, relationship-state reads, and profile visibility state
- Post service: post content, comments, likes, reposts, report intake, post-owned notifications, and public content retrieval primitives
- Media capability/service: upload validation, original and derivative assets, storage references, delivery policy, cleanup, and deletion coordination
- Dish service: canonical dish identity, normalization, aliases, lifecycle, and dish search primitives
- Place service: canonical place identity, normalization, location metadata, lifecycle, and place search primitives
- Feed capability/service: following/discover timelines, feed projections, fanout strategy, ranking inputs, replay/backfill, and cache invalidation when extracted
- Search/Recommendation capability/service: retrieval, ranking, AI-assisted discovery, recommendation explanations, and future dedicated search integration
- Notification capability/service: notification records, read state, account/system notifications, and future push/email/realtime delivery adapters
- Moderation/Trust capability/service: report lifecycle, review queue, actions, evidence, auditability, appeal readiness, spam/seeding rules, and future automation
- Analytics/Event pipeline: product event taxonomy, non-blocking collection, reporting feeds, and operational learning signals
- Python AI services: model-heavy helper capabilities behind internal contracts only

## Why This Architecture

Responsive web first focuses the product surface while still supporting real user adoption.

Microservices are an intentional trade-off that preserves explicit service boundaries despite higher early-product complexity. The product should avoid speculative infrastructure, but it must keep the boundaries a real social product needs: identity, profile graph, post/media, feed, search/recommendation, notification, moderation/trust, analytics, and operations.

The architecture should optimize for both:

- AI-assisted experiences for end users
- structured contracts for AI-agent consumption

It should not require full internal multi-agent behavior until authorization, policy, audit, and operations maturity exist.

## Search Architecture

The recommended product search architecture is:

- PostgreSQL full-text search as the first lexical engine
- trigram similarity for typo-tolerant matching on key dish/place/post fields
- structured filters for dish, place, location, cuisine, category, price, and related food metadata
- AI-assisted query understanding and suggestion generation above deterministic retrieval
- dedicated OpenSearch/Elasticsearch-style search only when PostgreSQL-first relevance, latency, indexing, or operational ownership becomes insufficient
- optional later hybrid/vector search when semantic retrieval has measurable product value

This preserves AI-native UX while avoiding a heavyweight search platform before it is justified.

## Feed Architecture

The feed direction must support staged maturity:

- first production beta: bounded Post/UserProfile-backed query composition through BFF or Post
- public product: feed read models and event-driven projection refresh where query composition is not enough
- mature scale: dedicated feed service with timeline storage, fanout-on-write/read strategy, ranking cache, replay/backfill, and special handling for high-fanout accounts

Following and Discover remain separate surfaces. Following prioritizes followed users; Discover is the primary recommendation surface.

Feed ranking should combine recency, engagement, food relevance, relationship signals, location context, user interests, diversity, and safety filters. Ranking inputs must remain observable enough for debugging, moderation review, and product learning.

## Social Graph Direction

The social graph must support:

- follow/unfollow
- block
- mute
- relationship-state reads for profiles, feeds, notifications, and interaction authorization
- basic visibility controls for public profile/content behavior
- idempotent mutations and clear ownership in User/Profile

Friendship, close-friend circles, and full privacy-account semantics are later features unless product direction proves they are needed earlier.

## Geo-Discovery Direction

The recommended geo-discovery direction is:

- keep place coordinates and canonical place metadata in the Place boundary
- support practical city, area, district, and nearby discovery without introducing a separate geo platform first
- use PostgreSQL-native geo-friendly querying patterns for radius, bounding, or distance-aware retrieval when location precision is needed
- combine geo relevance with lexical relevance and food relevance rather than treating distance as the only ranking signal

## AI Strategy

The recommended AI integration strategy is:

- one internal provider-agnostic AI abstraction or gateway
- capability-based adapters for multiple providers
- config-driven routing, timeouts, and fallback rules
- graceful degradation to deterministic behavior when providers fail or time out
- public contracts remain in .NET even if model-heavy AI capability services use Python
- provider calls must avoid unnecessary PII and must document logging, redaction, and retention behavior

## Media Storage and Delivery Direction

The recommended media direction is:

- posts/media owns application-level media lifecycle for uploads, attachment validation, moderation touchpoints, and deletion coordination
- binary media is stored in object-style storage rather than PostgreSQL
- original uploads and derived assets such as thumbnails or resized images are distinct
- image derivatives may be generated asynchronously when needed
- video-first workflows and transcoding-heavy pipelines are deferred until usage proves they matter
- media delivery supports controlled access patterns such as signed URLs, gateway-issued access, CDN-backed delivery with bounded exposure, or equivalent controls
- media deletion or post deletion must include cleanup expectations for derived assets and storage references

## Notification Direction

The notification direction must support:

- follow, like, comment, repost, moderation outcome, and account/system notifications
- read-state management and user-facing retrieval
- graceful partial-source behavior while notifications remain split across services
- extraction to a dedicated notification service when cross-service notification ownership, push/email/realtime delivery, or account/system notifications become difficult to operate inside current boundaries

Push notifications, email, and realtime delivery are delivery adapters, not substitutes for canonical notification records.

## Moderation and Trust Direction

The moderation/trust direction must support:

- user reports for posts, users, and comments
- report queue and manual review
- dismiss, hide/delete content, warn, restrict, suspend, and audit actions
- evidence retention and moderation history
- block and mute as user safety controls
- rate limits and obvious spam/seeding rules
- appeal readiness even if appeals are staged later

Deep automation, trust scoring, shadow banning, and auto-hide should not be introduced until review, appeal, audit, rollback, and operator capacity are defined.

## Caching and Read-Model Direction

The recommended caching direction is:

- treat PostgreSQL and service-owned read models as the default source of truth
- allow selective caching for measured hotspots such as feed fragments, canonical entity lookups, session-adjacent state, or expensive AI-assist responses
- require cache ownership, TTL, invalidation, and degradation behavior to remain explicit per use case
- do not use Redis as source of truth for canonical social data

## Feature Control and Runtime Configuration Direction

The recommended feature-control direction is:

- use configuration and lightweight feature flags for rollout gating, beta cohorts, kill switches, provider routing, ranking variants, and safety fallback behavior
- keep runtime feature control simple enough that it does not require a heavyweight experimentation platform before product scale requires it
- ensure operationally sensitive toggles can be disabled without redeploying all services
- keep feature changes auditable enough that operators can understand why a capability was enabled, disabled, or rerouted

## Trade-offs

### Benefits

- clear service ownership
- stronger contract boundaries
- practical first production stack
- lower frontend coupling through the BFF
- staged path from query-based features to dedicated read models and services

### Costs

- higher operational complexity than a monolith
- slower feature iteration during validation
- more cross-service consistency risk
- added complexity from first-class Dish and Place modeling
- added burden from self-hosted identity, multi-provider AI routing, and media lifecycle operations

## Environments

Required environments:

- local multi-service development environment
- staging environment
- production environment

Chosen deployment direction: Kubernetes-based deployment.

Aspire should be treated as the default local development composition layer, not as the production hosting decision by itself.

## Agent Exposure Direction

- Product APIs should be structured enough for controlled agent consumption.
- External agent actions require explicit authorization, scoping, policy enforcement, and auditability.
- Broad external agent actions are not public-launch defaults.

## Operational Readiness Direction

The architecture should distinguish between production-like beta, public launch, and mature scale.

Minimum public product expectations:

- production monitoring for service health, downstream latency, queue/outbox backlog, degraded AI behavior, degraded search, moderation backlog, and media lifecycle failures
- documented restore path and tested restore drill for each service-owned PostgreSQL datastore that matters to user trust
- documented recovery expectations for Keycloak, RabbitMQ, Redis if used for active flows, and media storage
- documented operator actions for provider outage, queue backlog, media pipeline failure, degraded search quality, moderation backlog, and auth failure
- feature controls that allow high-risk capabilities to be disabled quickly
- rollback path for application deployments and documented decision rules for database forward repair versus rollback

Broader scale maturity should additionally require:

- formal SLOs and alert thresholds
- stronger abuse defenses and confidence that moderation operations can keep up with incoming volume
- feed, search, notification, moderation, analytics, and media extraction where current ownership becomes unclear or operationally risky

## Monetization Readiness Direction

- The product should validate retention and trust before active monetization.
- Architecture and data contracts should remain extensible for later monetization models such as affiliate/ordering commission, premium features, ads, or mixed revenue.
- The primary long-term monetization direction should favor affiliate/ordering commission within a mixed model.

## Async Messaging Direction

- chosen event bus: RabbitMQ
- suitable for moderation triggers, search projection refresh, recommendation signals, notification creation, media lifecycle, analytics events, and enrichment workflows
- heavier streaming infrastructure should be introduced only when throughput, replay, partitioning, or long-retention event needs exceed RabbitMQ plus outbox/read-model patterns

### Event reliability expectations

- critical cross-service events should be published through an explicit reliability pattern such as an outbox
- consumers must remain idempotent and safe under duplicate delivery
- retries, dead-letter handling, and replay expectations should be documented for workflows that affect user-visible state or moderation-sensitive behavior
- event contracts should evolve additively where possible to reduce cross-service breakage risk
- event ownership, schema version, producer, consumer, retry policy, and replay strategy should be visible for product-critical events

## Tenant Direction

Approved direction: multi-tenant capable.

Architectural implication:

- tenant isolation must be reflected in identity, service contracts, persistence, and moderation workflows
- this improves long-term platform flexibility, but it increases complexity

Current clarification:

- tenant semantics are not yet exercised as a primary business model
- the current intent is to preserve future multi-tenant capability rather than require rich tenant-specific product behavior from day one
- tenant should be treated primarily as an architectural isolation concern until a real partner or multi-operator business model appears

## Explicitly Deferred Until Evidence Changes

The following capabilities should remain deferred unless real usage, product learning, or measured bottlenecks justify them:

- heavyweight dedicated search platform beyond PostgreSQL-first search
- platform-wide cache dependency
- vector-search infrastructure or embedding-first retrieval as a default dependency
- heavyweight experimentation platform
- deeply automated moderation that bypasses manual review, appeal readiness, audit, or rollback controls
- chat/direct messaging, presence, typing indicators, and read receipts
- video-first posting and heavy transcoding pipeline
- native mobile apps before web retention and operational stability justify the investment

This deferred list protects the product from accidental architecture inflation while preserving a real path to social-platform maturity.
