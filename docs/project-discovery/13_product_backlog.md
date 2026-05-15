# 13_product_backlog.md

## Status

- Working draft

## Epics

This file is the product planning and sequencing view for turning the FoodSocial strategy into deliverable slices.

Canonical product behavior and acceptance truth live in `04_functional_requirements.md`.

Canonical architecture and service-shape truth live in `10_architecture.md`, `11_frontend_structure.md`, and `12_backend_structure.md`.

### Epic 1: Account and identity

Goal: support guest browsing and authenticated user identity through an external identity provider.

Detailed profile and taste-profile behavior lives in `04_functional_requirements.md` and `11_frontend_structure.md`.

### Epic 2: Content creation and publishing

Goal: let users create food-first posts, place-aware posts, and structured recipe posts with minimal posting friction.

Potential stories:

- As a user, I can create a food post with media, dish identity, and caption/review text
- As a user, I can optionally attach canonical or fallback place information to a post
- As a user, I can create a recipe post with ingredients, steps, and media
- As a user, I can repost or reshare content with an optional short caption
- As the system, I can manage uploaded media, derived assets, and cleanup expectations without blocking the entire product on heavyweight media infrastructure

### Epic 3: Discovery feed and engagement

Goal: support social engagement through comments, likes, saves, external sharing, and in-app repost/reshare behavior.

Repost direction:

- in-app repost or reshare may include a short caption before heavier quote-post behavior is needed

### Epic 4: Search and food discovery

Goal: help users find dishes, places, and relevant food content through structured and AI-assisted discovery.

Detailed search and discovery behavior lives in `04_functional_requirements.md`.

Search and geo-discovery technical direction lives in `10_architecture.md`.

### Epic 5: Social graph and personalization

Goal: support follow, block, mute, profile visibility, and saved items that help users return to posts, dishes, and places safely.

Additional product direction:

- follow, like, and comment actions should create notifications visible in a dedicated notification center
- notification design should support moderation or account-level notifications as maturity grows
- repost or reshare actions should also create notifications
- block and mute should be respected by profile, feed, notification, and interaction surfaces

Execution emphasis:

- profile ownership, social graph, taste preferences, and saved-item ownership should stay together until extraction is justified

### Epic 6: AI recommendation

Goal: recommend dishes first, then places, in a way that supports the Food-first AI-native product direction.

Detailed recommendation behavior lives in `04_functional_requirements.md`.

### Epic 7: Agent-friendly platform capabilities

Goal: keep platform contracts structured enough for controlled-access agent consumption.

Potential stories:

- As a platform owner, I can expose controlled-access APIs for agent retrieval and safe actions
- As the system, I can keep agent-facing contracts auditable and authorization-aware

### Epic 8: Moderation and reporting

Goal: support trustworthy community behavior through reporting, manual review, user safety controls, and moderate trust controls.

Detailed moderation behavior lives in `04_functional_requirements.md`.

Operational trust and auditability direction lives in `05_non_functional_requirements.md`.

### Epic 9: Service infrastructure and contracts

Goal: make the chosen microservices architecture operable enough for real product use without overwhelming delivery.

Detailed infrastructure and contract direction lives in `10_architecture.md` and `12_backend_structure.md`.

## User Stories

Initial prioritized examples:

- As a guest, I can browse basic public food content before signing in
- As a registered user, I can create a food-first post tied to a dish and optionally a place
- As a user, I can save posts, dishes, and places for later
- As a user, I can follow creators and receive follow/interaction notifications
- As a user, I can block or mute another user and have that relationship respected across product surfaces
- As a user, I can discover dish recommendations in the Discover feed
- As a moderator, I can review reports and take content-level or user-level actions

## Acceptance Criteria

Acceptance truth for product behavior belongs in `04_functional_requirements.md`.

Use this file to plan sequencing and epic grouping, not to redefine canonical acceptance rules.

## Priority

Primary product outcome:

- achieve real user engagement, operational stability, and a clear path from constrained beta to public launch and scale maturity

## Dependencies

Initial dependency direction:

- identity and auth precede authenticated social actions
- canonical dish and place support improve publishing, search, and recommendation quality
- feed and recommendation depend on content creation plus interaction signals
- moderation depends on reporting plus admin-facing review capabilities
- notification center depends on follow and interaction event generation
- media handling depends on a clear object-storage and cleanup direction even if the underlying tooling stays lightweight
- reliable read-model refresh and non-blocking workflows depend on explicit async delivery expectations rather than transport choice alone
- public launch depends on block/mute and visibility policy, product event taxonomy, backup/restore drills, alert routing, and support ownership
