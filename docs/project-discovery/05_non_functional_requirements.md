# 05_non_functional_requirements.md

## Status

- Working draft

## Performance

### MVP direction

- prioritize responsive feed and search interactions for responsive web users
- keep AI-assisted features off the critical rendering path when possible
- use caching and precomputed read models selectively for feed and discovery

### Acceptance direction

- core feed and search pages should remain usable even when AI assistance is unavailable
- AI-assisted endpoints should have strict timeout policies and fallback behavior

## Security

### MVP direction

- secure service-to-service communication within the microservices environment
- store secrets and provider credentials through Aspire-compatible configuration and secret management
- do not expose AI provider credentials to the Angular client
- validate and authorize all cross-service and admin operations
- use a managed external identity provider to reduce custom authentication risk in MVP

## Trust and Moderation Direction

Approved direction:

- MVP: Moderate trust system
- Later phase: Strong trust layer

### MVP trust/moderation capabilities

- user reporting
- admin review workflow
- rule-based spam / seeding detection
- warning / strike / account restriction capability
- no automatic content hiding in MVP; moderation remains manual-review-first

### Later-phase trust capabilities

- automated suspicious behavior scoring
- trust score or reputation scoring
- stronger anti-seeding intelligence
- intelligent auto-hide based on report type and trust/risk scoring

## Scalability

### MVP direction

- optimize for correctness and bounded complexity first
- support horizontal scaling of the most expensive services later, especially feed, search/recommendation, and AI-integrated workloads
- keep service count low even under a microservices strategy

## Reliability

### MVP direction

- graceful degradation is mandatory for AI-assisted behavior
- if AI providers fail, core feed, posting, and deterministic search must continue to work
- cross-service timeouts, retries, and fallback behavior must be explicit

## Logging

### MVP direction

- structured logs for all services
- correlation IDs across service boundaries
- AI request/response metadata logging with redaction of sensitive content where required
- audit logs for moderation-sensitive actions

## Monitoring

### MVP direction

- health checks for all services
- OpenTelemetry-compatible tracing and metrics
- visibility into downstream PostgreSQL, AI provider, and service-to-service latency
- alerting for provider failures, degraded search, and service unavailability
- Kubernetes-oriented service health and readiness behavior should be considered part of operational design

## Backup and Recovery

### MVP direction

- backup strategy must exist per service-owned PostgreSQL datastore
- recovery procedures should be documented at least for posts, identities, dishes, places, and moderation data
- search/read models may be rebuilt from source-of-truth data if designed that way

## Compliance

### MVP direction

- minimize unnecessary PII sharing with external AI providers
- document which AI workflows send data externally
- prepare for privacy and content-governance requirements if launch geography expands

## Accessibility

### MVP direction

- responsive web MVP should follow practical accessibility standards for keyboard navigation, contrast, labels, and form usability
- AI-assisted UI should not be the only path to complete a user task

## Localization

### MVP direction

- initial launch language can be Vietnamese-first if needed
- text, prompts, and UI strings should still be structured so localization can be added later

## Initial Notes

- Trust and authenticity appear to be important product values and may need explicit non-functional and operational controls
- Moderation responsiveness may become a key quality attribute
- AI-assisted features should be designed with graceful fallback behavior when AI services are slow, unavailable, or low-confidence
- moderation-related logging and auditability should be treated as important operational requirements
- Aspire is useful for local orchestration and observability, but it does not remove distributed-system complexity
- Multiple AI providers improve resilience and flexibility, but they also increase cost, observability, and behavior-consistency requirements
- moderation data should be auditable enough to support a future appeal workflow even if appeal is not exposed in MVP
- Kubernetes improves long-term deployment structure, but it is a heavy operational choice for a solo MVP
