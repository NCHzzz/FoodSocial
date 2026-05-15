# 14_risks_open_questions.md

## Status

- Working draft

## Risks

### R-01 Product scope is broad

The product includes social posting, feed, search, social graph, AI recommendation, moderation, media lifecycle, and operations, which may create delivery risk.

This risk is amplified because the current plan is a solo build. Maturity staging in `16_maturity_roadmap.md` should prevent all mature-social capabilities from being treated as same-phase work.

### R-02 Product differentiation may be too abstract if trust/authenticity is not operationalized

The promise of more authentic food content needs concrete product and moderation mechanisms.

### R-03 Mixed content model complexity

Food posts, restaurant posts, and recipe posts may require different schemas and UX patterns.

This risk is partially reduced by the clarified content strategy: hybrid model, but Food-first.

### R-04 AI recommendation may be underdefined

Recommendation depth, data inputs, and launch expectations are not yet fully defined.

### R-05 Feed ranking can become overcomplicated too early

The chosen direction includes separate Following and Discover feeds with hybrid ranking. This is good for product quality, but implementation should evolve through query composition, read models, then dedicated timeline infrastructure only when justified.

### R-06 AI-native ambition may exceed solo capacity if interpreted too literally

There is a material difference between:

- an AI-native user experience, and
- deeply AI-heavy infrastructure before the product has enough usage signals

This distinction must be made explicit to keep staged delivery realistic.

### R-07 "AI agents should work well with it" is still ambiguous

This could mean very different scopes, such as:

- AI-assisted UX for human users
- external agents consuming platform APIs
- internal agents performing moderation, recommendation, or concierge actions
- autonomous agent participation patterns

These options have very different architectural costs.

This has now been partially clarified:

- Launch priority: A + B
- Later phase: C

### R-08 First-class place modeling increases launch delivery complexity

Making `Place` a canonical entity at launch improves long-term discovery and agent compatibility, but it increases implementation scope for post creation, search, and data quality handling.

### R-09 First-class dish modeling further increases launch delivery complexity

Making `Dish` a canonical entity at launch strongly supports the Food-first AI-native direction, but it adds complexity in canonical naming, duplicate handling, matching, and post creation UX.

### R-10 Microservices plus PostgreSQL-per-service increases integration overhead

Service-owned databases improve boundary clarity, but they make feed assembly, reporting, search projection, moderation views, notification creation, and consistency management harder for a solo builder.

### R-11 Multiple AI providers increase behavior and cost complexity

Provider flexibility improves resilience and future routing options, but it introduces output inconsistency, prompt portability issues, cost control needs, and observability burden.

### R-11B Polyglot AI services increase operational and contract complexity

Using Python for AI-oriented services is a strong technical fit, but it adds another runtime, packaging flow, observability path, and contract boundary to the platform.

### R-12 Self-hosted Keycloak increases launch operational burden

Choosing self-hosted Keycloak improves control and reduces vendor dependency, but it adds deployment, upgrade, backup, and operational complexity for a solo builder.

### R-13 API Gateway / BFF adds an extra layer but reduces frontend coupling

The gateway/BFF adds another deployable component, but it is justified because it reduces Angular coupling to microservice topology and centralizes aggregation and auth-aware edge behavior.

### R-14 Kubernetes deployment adds significant launch operational complexity

Kubernetes fits a microservices direction structurally, but it increases infrastructure, deployment, observability, and troubleshooting overhead for a solo builder on a 2-month timeline.

### R-15 Multi-tenant architecture increases isolation and operational complexity

Choosing multi-tenant architecture adds complexity to identity, authorization, data partitioning, moderation, analytics, and service contracts for launch.

### R-16 Broad external agent actions increase security and audit complexity

Allowing external AI agents to perform wider system actions in the long term increases the need for strong authorization, action scoping, auditability, and abuse prevention.

### R-17 Media lifecycle complexity is easy to underestimate

Media-heavy user-generated products require more than upload support. Storage cleanup, derivative handling, moderation visibility, and deletion behavior can create hidden scope if not documented early.

### R-18 Async messaging correctness can fail silently without explicit reliability rules

RabbitMQ is a practical launch choice, but event publication, retries, dead-letter behavior, and replay expectations must stay explicit or the architecture may look simpler on paper than it is in production.

Current implementation state for RabbitMQ and outbox-backed publishing belongs in `docs/context/current_build_status.md`.

### R-19 Geo-discovery expectations may exceed the current search definition

The product promise includes dish, place, and location-aware discovery. If nearby relevance is important, the launch search model must still define practical geo-query behavior even without a dedicated geo/search platform.

### R-20 Product operations may remain underdefined

The current docs acknowledge monitoring, backups, and graceful degradation, but the product still needs a clear operator model for outages, queue backlog, degraded AI behavior, moderation backlog, media failure, restore confidence, and public-launch promotion.

### R-21 Social safety controls can be inconsistently enforced

Block, mute, visibility, hidden content, notifications, and feed ranking affect multiple surfaces. If these controls are implemented as isolated endpoint behavior, users may still see unsafe or unwanted interactions.

### R-22 Analytics can become invasive or unreliable

FoodSocial needs product events for activation, retention, search, recommendation, moderation, and media learning. If event capture blocks user paths, leaks unnecessary PII, or lacks stable ownership, reporting will either hurt reliability or become untrustworthy.

## Unknowns

- budget
- public-launch user cap and operating model
- exact meaning of "AI-native" in product scope
- exact production hardening plan after constrained beta
- exact threshold for introducing dedicated cache, vector, or heavier search infrastructure

## Current Clarifications

- dish identity requirement is canonical in `04_functional_requirements.md`
- saved-content scope is canonical in `04_functional_requirements.md`
- launch agent-facing exposure direction is canonical in `10_architecture.md`
- maturity staging is canonical in `16_maturity_roadmap.md`

## Release Readiness Direction

- moving from constrained beta to broader public release should be a multi-factor decision
- the most important factors are active/retained real users and technical stability

## Agent Capability Direction

Canonical agent capability and exposure direction live in `10_architecture.md`.

## Clarified Constraints

Canonical delivery constraints already live in `00_project_overview.md`.

## Assumptions Needing Validation

Keep this section focused on unresolved assumptions only.

Canonical approved directions for architecture, moderation, search, launch market, and feed behavior already live in discovery source-of-truth docs and should not be restated here in full.

Current implementation state for Redis belongs in `docs/context/current_build_status.md`.

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
- AI-native implementation depth
- block/mute and visibility policy
- service extraction criteria for feed, search/recommendation, notification, moderation, media, and analytics
