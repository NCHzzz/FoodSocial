# 05_non_functional_requirements.md

## Status

- Working draft

## Non-Functional Decision Summary

This file is the canonical quality-attribute contract for FoodSocial as a real product.

Maturity staging lives in `16_maturity_roadmap.md`; this file defines quality expectations that must remain true as the product grows.

## Performance

- prioritize responsive feed and search interactions for responsive web users
- keep AI-assisted features off the critical rendering path when possible
- use caching and precomputed read models selectively for feed and discovery
- treat caching as a targeted optimization tool rather than a mandatory platform dependency before real hotspots are measured
- core feed and search pages should remain usable even when AI assistance is unavailable
- AI-assisted endpoints should have strict timeout policies and fallback behavior
- public product readiness requires defined latency targets for feed, search, post detail, auth/session, media read, and write-heavy actions
- dedicated feed, search, cache, or queue scaling work must be triggered by measured latency, throughput, reliability, or ownership problems, not speculation

## Security

- secure service-to-service communication within the microservices environment
- store secrets and provider credentials through Aspire-compatible configuration and production secret management
- do not expose AI provider credentials to the Angular client
- validate and authorize all cross-service and admin operations
- use an external identity provider to reduce custom authentication risk
- apply edge-aware protections such as rate limiting, bot-friction, WAF, or equivalent abuse controls for public-facing APIs
- keep admin and moderation capabilities behind stricter authorization boundaries than normal end-user flows
- define an explicit service-to-service trust model rather than leaving internal authorization implicit
- support secure cookie/session behavior for browser traffic
- preserve a path to MFA, external identity providers, account recovery, and device/session review without requiring custom identity implementation
- enforce block, mute, profile visibility, owner-only data, moderator/admin, and trusted-service boundaries consistently

## Trust and Moderation Direction

Approved trust direction is reflected in the product and architecture docs; this file keeps the quality and operational implications.

### Required trust/moderation capabilities

- user reporting
- admin review workflow
- rule-based spam and seeding detection
- warning, strike, account restriction, and suspension capability
- no automatic enforcement unless review, appeal, audit, and rollback controls exist
- moderation remains manual-review-first for enforcement decisions until automation is mature
- moderation queues and evidence should remain auditable enough to support later appeal or secondary review workflows
- block and mute controls must reduce user-to-user harm without requiring moderator intervention
- moderation operations must define owner, SLA, escalation, and backlog thresholds for public launch

### Later-phase trust capabilities

- automated suspicious behavior scoring
- trust score or reputation scoring
- stronger anti-seeding intelligence
- intelligent auto-hide based on report type and trust/risk scoring
- formal appeal workflows and secondary review

## Scalability

- optimize for correctness and bounded complexity first
- support horizontal scaling of the most expensive services later, especially feed, search/recommendation, notification, moderation, media, and AI-integrated workloads
- keep service count low until ownership, scale, or correctness requires extraction
- avoid introducing heavyweight search, cache, vector, or streaming infrastructure as default dependencies before product evidence requires them
- define extraction criteria for Feed, Search/Recommendation, Notification, Moderation, and Media services before splitting runtime ownership
- avoid cross-service database access; scale via contracts, events, read models, and service-owned storage

## Reliability

- graceful degradation is mandatory for AI-assisted behavior
- if AI providers fail, core feed, posting, and deterministic search must continue to work
- cross-service timeouts, retries, and fallback behavior must be explicit
- queue backlog, downstream outage, and event replay scenarios should have documented operator responses
- critical async workflows should define retry and dead-letter expectations where failure would affect user trust or moderation correctness
- production readiness requires documented RTO/RPO expectations for service-owned PostgreSQL data, Keycloak state, RabbitMQ topology/state, Redis if used for active flows, and media storage
- public launch requires tested rollback for application deployment and documented decision rules for database rollback versus forward repair

## Logging

- structured logs for all services
- correlation IDs across service boundaries
- AI request/response metadata logging with redaction of sensitive content where required
- audit logs for moderation-sensitive actions
- log retention for operational and moderation-sensitive events should be intentional rather than left entirely to infrastructure defaults
- auth, trust, moderation, deletion, and admin actions must produce enough logs to reconstruct what happened without exposing unnecessary private user content

## Monitoring

- health checks for all services
- OpenTelemetry-compatible tracing and metrics
- visibility into downstream PostgreSQL, AI provider, RabbitMQ, Redis, media storage, and service-to-service latency
- alerting for provider failures, degraded search, and service unavailability
- Kubernetes-oriented service health and readiness behavior should be considered part of operational design
- the product environment should have a small, explicit dashboard and alert set for queue backlog, outbox backlog, failed async jobs, degraded search/recommendation, media-processing failures, moderation backlog, and auth failures
- SLOs and alert thresholds should become formal before broad public launch

## Backup and Recovery

- backup strategy must exist per service-owned PostgreSQL datastore
- recovery procedures should be documented at least for posts, identities, dishes, places, and moderation data
- search/read models may be rebuilt from source-of-truth data if designed that way
- restore confidence should be treated as more important than backup existence alone
- media storage recovery expectations should be documented separately from PostgreSQL recovery where media is stored outside the database
- product readiness should include realistic restore drills for core user-trust data paths

## Compliance

- minimize unnecessary PII sharing with external AI providers
- document which AI workflows send data externally
- prepare for privacy and content-governance requirements if launch geography expands
- define baseline retention and deletion expectations for user content, moderation records, and derived media assets
- make user-facing deletion behavior and internal cleanup behavior compatible enough that later compliance work does not require a redesign of core entities
- define user data export, account deletion, content deletion, moderation record retention, and media cleanup semantics before broad public launch
- AI workflows must document whether user content leaves the platform boundary and how prompts/responses are logged, retained, and redacted

## Feature Control and Rollout Safety

- use lightweight runtime feature controls for AI-assisted capabilities, rollout gating, and kill switches
- keep configuration and feature changes auditable enough that operators can understand why a capability was enabled, disabled, or rerouted
- risky capabilities such as AI assist, media upload, recommendation variants, moderation automation, and high-write actions must have rollback or disable controls

## Analytics and Product Learning

- capture enough product and operational events to evaluate activation, retention, search usage, recommendation usage, and moderation volume
- keep analytics requirements lightweight at first, but define a minimal event taxonomy so later reporting does not require rethinking core contracts
- required event families: signup/session, post create/read, feed impression, search query/result click, recommendation impression/click/save, follow/block/mute, engagement, report/moderation action, media upload/cleanup, and feature flag state
- analytics ingestion must not block user-facing write paths

## Geo-Discovery Quality

- nearby and area-based discovery should use a clear location relevance strategy rather than relying only on plain text matching
- the product should prefer practical city, district, area, and nearby usefulness before map-heavy interaction complexity

## Accessibility

- responsive web should follow practical accessibility standards for keyboard navigation, contrast, labels, and form usability
- AI-assisted UI should not be the only path to complete a user task

## Localization

- initial product language can be Vietnamese-first if needed
- text, prompts, and UI strings should still be structured so localization can be added later

## Initial Notes

The repo’s architecture and stack rules already encode many implementation implications; keep this file focused on explicit non-functional direction and acceptance-level quality expectations.
