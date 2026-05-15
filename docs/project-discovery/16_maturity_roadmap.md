# 16_maturity_roadmap.md

## Status

- Working draft

## Purpose

This document defines how FoodSocial grows from the current product baseline into a real public social product without adding unnecessary infrastructure before it is justified.

It does not replace product scope, functional requirements, or architecture truth:

- product scope lives in `03_product_scope.md`
- functional requirements live in `04_functional_requirements.md`
- non-functional requirements live in `05_non_functional_requirements.md`
- architecture decisions live in `10_architecture.md`
- backend implications live in `12_backend_structure.md`

## Maturity Principles

- Build real product controls before chasing scale: identity, trust, moderation, backup, media lifecycle, observability, and rollback matter before advanced infrastructure.
- Keep service boundaries explicit even when runtime extraction is staged.
- Extract services only when ownership, latency, correctness, or operational risk makes extraction cheaper than composition.
- Keep AI useful but non-critical: deterministic paths must keep core posting, feed, search, and recommendation usable.
- Treat social safety as core product behavior: follow, block, mute, report, restriction, and audit are not optional for a public social product.

## Stage 0: Current Baseline

Goal: preserve the existing runnable product slice while documentation and implementation align to real-product needs.

Expected state:

- BFF/API edge exists.
- Identity, UserProfile, Dish, Place, and Post boundaries exist.
- PostgreSQL per service remains source of truth.
- RabbitMQ and outbox-backed publishing exist for current event paths.
- Redis is a selective cache, not source of truth.
- Post/UserProfile own the current feed, search, report, interaction, and notification basics until extraction is justified.

Exit criteria:

- docs no longer describe FoodSocial as only an MVP prototype
- current gaps are represented as maturity gates, not ignored
- production readiness checklist matches actual service set and active dependencies

## Stage 1: Production-Like Beta

Goal: run with real users in a constrained environment while core product and operations are accountable.

Required product capabilities:

- food-first posts with canonical dish and optional place linkage
- Following and Discover feed surfaces
- like, comment, save, share, repost
- profile and saved items
- follow, block, and mute
- report intake and manual moderation queue
- notification center for follow and interaction activity
- PostgreSQL-first search by dish, place, post, and location context
- deterministic recommendation fallback
- object-style media storage decision and cleanup process

Required operational capabilities:

- production-like Keycloak realm/client/role configuration
- target-environment migrations for all service-owned PostgreSQL databases
- environment-owned secrets
- ingress/TLS
- logs, metrics, traces, dashboards, and alert route
- backup and restore drill for core user-trust data
- media recovery policy
- rollback path
- named owner for moderation, incidents, and support

Do not require yet:

- native mobile apps
- chat/direct messaging
- heavyweight search platform
- vector search
- platform-wide cache dependency
- deeply automated moderation
- heavyweight experimentation platform

## Stage 2: Public Launch

Goal: expose FoodSocial beyond invite-only beta with stronger safety, reliability, and support.

Required product capabilities:

- stable onboarding and account recovery path
- public/limited profile visibility policy
- consistent block/mute enforcement across profile, feed, notification, and interaction paths
- report outcomes for users where appropriate
- account/system notifications
- improved search relevance and typo tolerance
- recommendation explanations safe enough for users
- moderation actions for warn, restrict, suspend, hide, delete, and dismiss
- basic analytics for activation, retention, posting, feed, search, recommendation, moderation, and media lifecycle

Required operational capabilities:

- public-launch SLOs for feed, search, post detail, auth/session, and media read paths
- alert thresholds for service health, latency, queue/outbox backlog, moderation backlog, media cleanup failure, degraded search, and AI provider failure
- restore drills scheduled and recorded
- incident runbooks for auth failure, queue backlog, media failure, degraded search, degraded AI, and moderation backlog
- secret rotation owner and cadence
- WAF, bot-friction, or equivalent edge protection

Extraction triggers:

- create Feed service/read models when BFF/Post feed composition cannot meet latency, ranking, freshness, or replay needs
- create Notification service when notification ownership, account/system notifications, or delivery adapters become cross-cutting
- create Moderation service when report volume, audit needs, or enforcement workflows outgrow Post/UserProfile ownership
- create Search/Recommendation service when search/recommendation logic needs independent ownership or derived projections

## Stage 3: Social Scale

Goal: handle growth, high-fanout behavior, stronger personalization, and larger moderation workload.

Expected capability upgrades:

- dedicated Feed service with timeline storage, fanout strategy, ranking cache, replay/backfill, and high-fanout account handling
- dedicated Search/Recommendation service with projection refresh and optional dedicated search engine
- dedicated Notification service with push/email/realtime adapters if needed
- dedicated Moderation/Trust service with appeal workflow and stronger anti-abuse rules
- analytics/event pipeline with stable event taxonomy and reporting jobs
- media service or worker pool for derivative generation, cleanup, and delivery optimization
- stronger recommendation signal store and model evaluation workflow

Infrastructure may be introduced when justified:

- OpenSearch/Elasticsearch for search relevance and indexing scale
- vector search when semantic retrieval has measured user value
- heavier event streaming when RabbitMQ plus outbox/read models cannot meet replay, partitioning, or retention needs
- CDN integration for media delivery once access and deletion rules are clear
- platform-wide experimentation only after runtime feature controls are insufficient

## Stage 4: Platform Expansion

Goal: expand the business model and product surface after social retention and operations are healthy.

Candidate expansions:

- native mobile apps
- restaurant/creator partner workflows
- food delivery or ordering integrations
- richer recipe workflows
- premium features
- affiliate or ordering commission
- broader external agent actions
- internal AI agents for curation, moderation assistance, or concierge flows

Entry criteria:

- retention and content creation are healthy enough to justify expansion
- moderation and support load are manageable
- production incident process is working
- data, media, identity, and backup responsibilities are stable

## Service Extraction Decision Rules

Extract a service when at least one of these is true:

- one team or operator needs clear ownership of the capability
- current query composition cannot meet agreed latency or freshness targets
- the capability needs its own read models, workers, scaling profile, or failure isolation
- event replay/backfill becomes a regular operational need
- the current service boundary creates authorization, moderation, or data lifecycle ambiguity

Do not extract a service only because mature social apps often have one.

## Current Priority

For the next maturity step, prioritize:

1. production media storage and cleanup policy
2. block/mute and visibility policy
3. public endpoint authorization matrix
4. event taxonomy for product-critical async flows
5. production readiness checklist alignment with actual services
6. feed/search/notification/moderation extraction criteria
