# 05_non_functional_requirements.md

## Status

- Working draft

## Non-Functional Decision Summary

This file is the canonical quality-attribute contract for MVP.

## Performance

- prioritize responsive feed and search interactions for responsive web users
- keep AI-assisted features off the critical rendering path when possible
- use caching and precomputed read models selectively for feed and discovery
- treat caching as a targeted optimization tool rather than a mandatory platform dependency before real hotspots are measured


- core feed and search pages should remain usable even when AI assistance is unavailable
- AI-assisted endpoints should have strict timeout policies and fallback behavior

## Security

- secure service-to-service communication within the microservices environment
- store secrets and provider credentials through Aspire-compatible configuration and secret management
- do not expose AI provider credentials to the Angular client
- validate and authorize all cross-service and admin operations
- use an external identity provider to reduce custom authentication risk in MVP
- apply edge-aware protections such as rate limiting, bot-friction, or equivalent abuse controls for public-facing APIs
- keep admin and moderation capabilities behind stricter authorization boundaries than normal end-user flows
- define a lightweight but explicit service-to-service trust model rather than leaving internal authorization fully implicit

## Trust and Moderation Direction

Approved trust direction is already reflected in the product and architecture docs; this file keeps the quality and operational implications only.

### MVP trust/moderation capabilities

- user reporting
- admin review workflow
- rule-based spam / seeding detection
- warning / strike / account restriction capability
- no automatic content hiding in MVP; moderation remains manual-review-first
- moderation queues and evidence should remain auditable enough to support later appeal or secondary review workflows

### Later-phase trust capabilities

- automated suspicious behavior scoring
- trust score or reputation scoring
- stronger anti-seeding intelligence
- intelligent auto-hide based on report type and trust/risk scoring

## Scalability

- optimize for correctness and bounded complexity first
- support horizontal scaling of the most expensive services later, especially feed, search/recommendation, and AI-integrated workloads
- keep service count low even under a microservices strategy
- avoid introducing heavyweight search, cache, or vector infrastructure as default dependencies before MVP evidence requires them

## Reliability

- graceful degradation is mandatory for AI-assisted behavior
- if AI providers fail, core feed, posting, and deterministic search must continue to work
- cross-service timeouts, retries, and fallback behavior must be explicit
- queue backlog, downstream outage, and event replay scenarios should have at least a minimal documented operator response
- critical async workflows should define retry and dead-letter expectations where failure would affect user trust or moderation correctness

## Logging

- structured logs for all services
- correlation IDs across service boundaries
- AI request/response metadata logging with redaction of sensitive content where required
- audit logs for moderation-sensitive actions
- log retention for operational and moderation-sensitive events should be intentional rather than left entirely to infrastructure defaults

## Monitoring

- health checks for all services
- OpenTelemetry-compatible tracing and metrics
- visibility into downstream PostgreSQL, AI provider, and service-to-service latency
- alerting for provider failures, degraded search, and service unavailability
- Kubernetes-oriented service health and readiness behavior should be considered part of operational design
- the beta environment should have a small, explicit dashboard and alert set for queue backlog, failed async jobs, and media-processing failures where applicable

## Backup and Recovery

- backup strategy must exist per service-owned PostgreSQL datastore
- recovery procedures should be documented at least for posts, identities, dishes, places, and moderation data
- search/read models may be rebuilt from source-of-truth data if designed that way
- restore confidence should be treated as more important than backup existence alone
- media storage recovery expectations should be documented separately from PostgreSQL recovery where media is stored outside the database
- beta readiness should include at least one realistic restore drill for core user-trust data paths

## Compliance

- minimize unnecessary PII sharing with external AI providers
- document which AI workflows send data externally
- prepare for privacy and content-governance requirements if launch geography expands
- define baseline retention and deletion expectations for user content, moderation records, and derived media assets
- make user-facing deletion behavior and internal cleanup behavior compatible enough that later compliance work does not require a redesign of core entities

## Feature Control and Rollout Safety

- use lightweight runtime feature controls for AI-assisted capabilities, rollout gating, and kill switches during beta
- keep configuration and feature changes auditable enough that operators can understand why a capability was enabled, disabled, or rerouted

## Analytics and Product Learning

- capture enough product and operational events to evaluate activation, retention, search usage, recommendation usage, and moderation volume
- keep analytics requirements lightweight for MVP, but define a minimal event taxonomy so later reporting does not require rethinking core contracts

## Geo-Discovery Quality

- nearby and area-based discovery should use a clear location relevance strategy rather than relying only on plain text matching
- the first release should prefer practical city, district, area, and nearby usefulness over map-heavy interaction complexity

## Accessibility

- responsive web MVP should follow practical accessibility standards for keyboard navigation, contrast, labels, and form usability
- AI-assisted UI should not be the only path to complete a user task

## Localization

- initial launch language can be Vietnamese-first if needed
- text, prompts, and UI strings should still be structured so localization can be added later

## Initial Notes

The repo’s architecture and stack rules already encode many implementation implications; keep this file focused on explicit non-functional direction and acceptance-level quality expectations.
