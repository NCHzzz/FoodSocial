# MVP Launch Readiness

## Purpose

This file tracks the remaining environment-owned work required before an MVP beta launch.

Repo verification is green, but production launch still needs operator-owned infrastructure, secrets, approvals, and live-environment checks.

## Current Verdict

- Code readiness: mostly ready for controlled beta.
- Production readiness: not complete until the items in this file are done in the target environment.
- Launch posture: use deterministic recommendation fallback unless an AI provider, model, budget, and rollback owner are approved.

## Completed Repository Checks

- Backend build passes with `dotnet build Backend/FoodSocial.slnx`.
- Frontend production build passes with `npm run build` from `Frontend/`.
- Frontend unit tests pass with `npm test -- --watch=false` from `Frontend/`.
- Post migrations exist in `Backend/src/Post/Infrastructure/Persistence/Migrations/`.
- UserProfile migrations exist in `Backend/src/UserProfile/Infrastructure/Persistence/Migrations/`.
- Kubernetes, Helm, and observability starter assets exist under `ops/`.

## Launch Gate Checklist

| Gate | Status | Owner action |
| --- | --- | --- |
| Post database migrations | Blocked on target PostgreSQL access | Apply Post EF migrations in staging, verify app startup, then repeat for production. |
| UserProfile database migrations | Blocked on target PostgreSQL access | Apply UserProfile EF migrations in staging, verify app startup, then repeat for production. |
| Keycloak realm/client/roles | Blocked on identity environment | Configure realm, clients, redirect URIs, audiences, signing keys, and `user`, `moderator`, `admin` role mapping outside Aspire. |
| Object media storage | Decision required | Choose S3-compatible storage, Azure Blob, GCS, or another object store; wire credentials as environment secrets. |
| Media derivative and cleanup workers | Decision required | Approve launch behavior for thumbnail generation, stale upload cleanup, and deleted media cleanup. |
| AI recommendation provider | Decision required | Either approve provider/model/budget/kill switch or launch with deterministic fallback only. |
| Beta thresholds | Decision required | Define launch region, user cap, moderation SLA, incident severity, rollback trigger, and support owner. |
| Kubernetes secrets | Blocked on platform | Create environment-owned secrets for databases, internal trust keys, Keycloak, RabbitMQ, Redis, media storage, AI provider, and alert receivers. |
| Ingress and TLS | Blocked on platform | Configure DNS, ingress class, TLS certificates, hostnames, and secure cookie/domain settings. |
| Backup and restore | Blocked on platform | Configure per-service PostgreSQL backups, Keycloak database backups, media backup policy, and restore drill. |
| Logs, metrics, traces, alerts | Blocked on platform | Deploy OTEL Collector, Prometheus, Grafana, Loki, Tempo, Alertmanager, and receiver secrets; verify a test alert. |

## Migration Commands

Run these from the repository root only after target connection strings are set through the deployment environment or a safe local secret store.

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project "Backend/src/Post/Infrastructure/FoodSocial.Post.Infrastructure.csproj" \
  --startup-project "Backend/src/Post/Api/FoodSocial.Post.Api.csproj" \
  --context FoodSocial.Post.Infrastructure.Persistence.PostDbContext
dotnet tool run dotnet-ef database update \
  --project "Backend/src/UserProfile/Infrastructure/FoodSocial.UserProfile.Infrastructure.csproj" \
  --startup-project "Backend/src/UserProfile/Api/FoodSocial.UserProfile.Api.csproj" \
  --context FoodSocial.UserProfile.Infrastructure.Persistence.UserProfileDbContext
```

For staging and production, prefer the deployment pipeline or a controlled release job over ad hoc local execution.

## Keycloak Minimum Configuration

- Realm: environment-specific FoodSocial realm.
- Browser client: Angular/BFF login flow with approved redirect/logout URIs.
- API audience: matches backend `Keycloak:Audience` configuration.
- Roles: `user`, `moderator`, and `admin` mapped into access tokens or userinfo claims consumed by services.
- Token policy: short-lived access tokens, refresh behavior aligned with BFF cookie session behavior.
- Operations: backup realm export and Keycloak database before beta traffic.

## Media Storage Decision

Recommended beta default:

- Use an S3-compatible object store.
- Keep original uploads private by default.
- Serve attached public post media only through approved delivery URLs.
- Run cleanup for stale pending uploads and deleted media at least daily.
- Treat missing thumbnails as degraded UX, not a publish blocker.

## AI Launch Decision

Recommended beta default:

- Launch with deterministic recommendation fallback enabled.
- Keep provider-backed AI disabled until provider, model, monthly budget, prompt/data policy, latency target, and kill switch owner are approved.
- Add provider-backed AI only behind runtime configuration and safe fallback reason reporting.

## Beta Threshold Proposal

- Region: Vietnam-first, one city/area focus for the first cohort.
- User cap: 50 invited users until moderation and support load is known.
- Moderation SLA: review user reports within one business day during beta.
- Incident rollback: disable risky writes, AI assist, or media upload when error rate or abuse reports exceed owner-defined thresholds.
- Support owner: one named person owns daily review of reports, failed jobs, and alerts during beta.

## Production Infra Minimum

- Secrets: no committed credentials; all environment-owned values stored in Kubernetes Secrets or the chosen secret manager.
- TLS: all public hosts use HTTPS; cookies configured for secure production domains.
- Backups: PostgreSQL per service, Keycloak state, and media storage recovery policy documented and tested.
- Observability: `/health`, `/alive`, OTLP telemetry, starter Grafana dashboard, and at least one paging or notification route verified.
- Rollback: image rollback command and database rollback policy documented before launch.

