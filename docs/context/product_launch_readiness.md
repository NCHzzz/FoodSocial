# Product Launch Readiness

## Purpose

This file tracks the remaining environment-owned work required before FoodSocial can run as a real product in a production-like environment.

Last recorded repo verification was green, but production launch still needs operator-owned infrastructure, secrets, approvals, and live-environment checks.

## Current Verdict

- Code readiness: mostly ready for constrained beta validation based on the last recorded verification baseline.
- Production readiness: not complete until the items in this file are done in the target environment.
- Launch posture: use deterministic recommendation fallback unless an AI provider, model, budget, and rollback owner are approved.

## Last Recorded Repository Checks

- Backend build passes with `dotnet build Backend/FoodSocial.slnx`.
- Frontend production build passes with `npm run build` from `Frontend/`.
- Frontend unit tests pass with `npm test -- --watch=false` from `Frontend/`.
- service-owned migrations exist for Identity, UserProfile, Dish, Place, and Post.
- Kubernetes, Helm, and observability starter assets exist under `ops/`.

## Launch Gate Checklist

| Gate | Status | Owner action |
| --- | --- | --- |
| Service-owned database migrations | Blocked on target PostgreSQL access | Apply Identity, UserProfile, Dish, Place, and Post EF migrations in staging, verify app startup, then repeat for production. |
| Keycloak realm/client/roles | Blocked on identity environment | Configure realm, clients, redirect URIs, audiences, signing keys, and `user`, `moderator`, `admin` role mapping outside Aspire. |
| Object media storage | Decision required | Choose S3-compatible storage, Azure Blob, GCS, or another object store; wire credentials as environment secrets. |
| Media derivative and cleanup workers | Decision required | Approve launch behavior for thumbnail generation, stale upload cleanup, and deleted media cleanup. |
| Block/mute and visibility policy | Decision required | Define how block, mute, public profile, hidden content, notifications, and feed surfaces interact. |
| AI recommendation provider | Decision required | Either approve provider/model/budget/kill switch or launch with deterministic fallback only. |
| Beta and public-launch thresholds | Decision required | Define launch region, user cap, moderation SLA, incident severity, rollback trigger, support owner, and promotion criteria. |
| Kubernetes secrets | Blocked on platform | Create environment-owned secrets for databases, Keycloak browser/service clients, RabbitMQ, Redis, media storage, AI provider, and alert receivers. |
| Ingress and TLS | Blocked on platform | Configure DNS, ingress class, TLS certificates, hostnames, and secure cookie/domain settings. |
| Backup and restore | Blocked on platform | Configure per-service PostgreSQL backups, Keycloak database backups, media backup policy, and restore drill. |
| Logs, metrics, traces, alerts | Blocked on platform | Deploy OTEL Collector, Prometheus, Grafana, Loki, Tempo, Alertmanager, and receiver secrets; verify a test alert. |

## Migration Commands

For the canonical migration service map and commands, see `Backend/MIGRATIONS.md`.

Run migrations from the repository root only after target connection strings are set through the deployment environment or a safe local secret store. For staging and production, prefer the deployment pipeline or a controlled release job over ad hoc local execution.

## Keycloak Minimum Configuration

- Realm: environment-specific FoodSocial realm.
- Browser client: Angular/BFF login flow with approved redirect/logout URIs.
- API audience: matches backend `Keycloak:Audience` configuration.
- Roles: `user`, `moderator`, and `admin` mapped into access tokens or userinfo claims consumed by services.
- Token policy: short-lived access tokens, refresh behavior aligned with BFF cookie session behavior.
- Operations: backup realm export and Keycloak database before beta traffic.

## Media Storage Decision

Recommended constrained-beta default:

- Use an S3-compatible object store.
- Keep original uploads private by default.
- Serve attached public post media only through approved delivery URLs.
- Run cleanup for stale pending uploads and deleted media at least daily.
- Treat missing thumbnails as degraded UX, not a publish blocker.

## AI Launch Decision

Recommended constrained-beta default:

- Launch with deterministic recommendation fallback enabled.
- Keep provider-backed AI disabled until provider, model, monthly budget, prompt/data policy, latency target, and kill switch owner are approved.
- Add provider-backed AI only behind runtime configuration and safe fallback reason reporting.

## Beta Threshold Proposal

- Region: Vietnam-first, one city/area focus for the first cohort.
- User cap: 50 invited users until moderation and support load is known.
- Moderation SLA: review user reports within one business day during beta.
- Incident rollback: disable risky writes, AI assist, or media upload when error rate or abuse reports exceed owner-defined thresholds.
- Support owner: one named person owns daily review of reports, failed jobs, and alerts during beta.

## Public Launch Promotion Gates

- block/mute and visibility behavior is defined and verified across profile, feed, notification, and interaction flows.
- account/system notifications have an owner and recovery behavior.
- moderation queue, backlog alerting, and report outcome handling are documented.
- product event taxonomy covers activation, retention, posting, feed, search, recommendation, engagement, moderation, and media lifecycle.
- production SLOs and alert thresholds are defined for feed, search, post detail, auth/session, media read, queue/outbox backlog, and moderation backlog.

## Production Infra Minimum

- Secrets: no committed credentials; all environment-owned values stored in Kubernetes Secrets or the chosen secret manager.
- TLS: all public hosts use HTTPS; cookies configured for secure production domains.
- Backups: PostgreSQL per service, Keycloak state, and media storage recovery policy documented and tested.
- Observability: `/health`, `/alive`, OTLP telemetry, starter Grafana dashboard, and at least one paging or notification route verified.
- Rollback: image rollback command and database rollback policy documented before launch.
