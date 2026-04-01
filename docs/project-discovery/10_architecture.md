# 10_architecture.md

## Status

- Working draft

## Proposed Architecture

Current direction is to design for a responsive web-first MVP.

Approved backend architecture direction: microservices.

Chosen stack direction:

- .NET 10 backend services
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
- social graph service
- post and media service
- dish service
- place service
- feed service
- search / recommendation service
- AI integration gateway or provider abstraction layer
- moderation / trust service
- media storage
- structured domain and API contracts that remain usable by AI agents

## Data Flow

Initial high-level flow:

1. Users create structured food-first posts
2. Posts are linked to canonical dishes and places when possible
3. The post service stores content while resolving dish/place references through canonical services or shared contracts
4. The feed service retrieves content based on follow graph and discovery rules
5. The search / recommendation service assembles AI-assisted discovery results
6. User interactions create additional ranking and recommendation signals across service boundaries

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

### Chosen multi-provider AI strategy

The recommended MVP AI integration strategy is:

- one internal provider-agnostic AI abstraction or gateway
- capability-based adapters for multiple providers
- config-driven routing and fallback rules
- graceful degradation to deterministic behavior when providers fail or time out

### Recommendation evolution direction

- MVP recommendation depth: medium personalization
- architecture should preserve a clean upgrade path toward stronger personalization later
- recommendation signals, user-interest data, and read models should be designed to evolve without rewriting core service boundaries

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

## Monetization Readiness Direction

- MVP should focus on product validation rather than immediate monetization
- architecture and data contracts should remain extensible for later monetization models such as ads, affiliate/ordering commission, premium features, or mixed revenue strategies
- the primary long-term monetization direction should favor affiliate / ordering commission within a mixed model

## Async Messaging Direction

- chosen event bus for MVP: RabbitMQ
- suitable for moderation triggers, search projection refresh, recommendation signals, and enrichment workflows

## Tenant Direction

Approved direction: multi-tenant.

Architectural implication:

- tenant isolation must be reflected in identity, service contracts, persistence, and moderation workflows
- this improves long-term platform flexibility, but it increases MVP complexity significantly

Current clarification:

- tenant semantics are not yet exercised as a primary MVP business model
- the current intent is to preserve future multi-tenant capability rather than require rich tenant-specific product behavior from day one
