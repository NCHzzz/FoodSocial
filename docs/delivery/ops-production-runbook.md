# FoodSocial Production Operations Runbook

## Scope

This runbook covers the repository-provided production operations baseline: image publishing, Kubernetes deployment, observability, rollback, and incident checks.

## Image publishing

1. Create a SemVer tag such as `v0.1.0`, or trigger `Release Images` manually with a specific image tag.
2. The workflow publishes images to GHCR under `ghcr.io/<owner>/foodsocial/<service>:<tag>`.
3. Confirm all deployable images are present: `api-gateway`, `bff`, `identity-api`, `userprofile-api`, `dish-api`, `place-api`, `post-api`, `web`.

## Deployment

1. Configure a GitHub Environment named `staging` and/or `production`.
2. Add secret `KUBE_CONFIG` containing a kubeconfig with least-privilege deploy access.
3. Update `ops/helm/foodsocial/values.<environment>.yaml` with the real image repository, ingress, and service environment values. Route `/api`, `/signin-oidc`, and `/signout-callback-oidc` to `foodsocial-api-gateway`; route `/` to `foodsocial-web`.
4. Run `Deploy Kubernetes` with the target environment, namespace, and image tag.
5. Confirm rollout status and `/health` for API Gateway, BFF, Identity, UserProfile, Dish, Place, Post, and Web.

## Rollback

1. Use `helm history foodsocial -n <namespace>` to identify the previous revision.
2. Run `helm rollback foodsocial <revision> -n <namespace>`.
3. Confirm rollout status for API Gateway, BFF, Identity, UserProfile, Dish, Place, Post, and Web.
4. Check Prometheus for elevated 5xx or latency alerts after rollback.

## Observability checks

- Prometheus target `otel-collector:9464` is up.
- Grafana FoodSocial dashboard shows request rate and p95 latency.
- Tempo receives traces for API Gateway, BFF, and backend APIs.
- Loki receives application logs.
- Alertmanager routes critical alerts to the configured receiver.

## Known production decisions still owned by operators

- PostgreSQL, RabbitMQ, Redis, and Keycloak provisioning and backups.
- External DNS, ingress controller, TLS certificates, and WAF/bot protection.
- Secret management provider and rotation policy.
- Retention duration for logs, metrics, traces, and backups.
- Incident paging receiver and escalation policy.
