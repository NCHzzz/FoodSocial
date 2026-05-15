# Kubernetes Deployment Templates

FoodSocial targets Kubernetes for deployment, while Aspire remains the local orchestration model. The manifests in this directory are templates to adapt per service and environment; they are not a complete production stack. Production ingress should send `/api`, `/signin-oidc`, and `/signout-callback-oidc` to `foodsocial-api-gateway`; `/` stays on `foodsocial-web`.

## Probe contract

Shared service defaults define two HTTP endpoints:

- `/alive` for liveness probes: checks the process can respond.
- `/health` for readiness-style health checks: use this for traffic routing. Identity and UserProfile currently include PostgreSQL and RabbitMQ readiness checks; earlier-stage services only expose the shared self-check until they gain real dependencies.

Current `ServiceDefaults` maps these endpoints in development and when `ServiceDefaults__ExposeHealthEndpoints=true` is set. The deployment template enables that setting explicitly so Kubernetes probes do not depend on the local-development environment name.

## OTLP contract

Services should export telemetry by setting:

```yaml
OTEL_EXPORTER_OTLP_ENDPOINT: http://otel-collector.observability.svc.cluster.local:4317
ServiceDefaults__ExposeHealthEndpoints: "true"
```

This matches the existing `FoodSocial.ServiceDefaults` OTLP exporter behavior and avoids application code changes.

## Template usage

Use `service-deployment-template.yaml` once per deployable Gateway/API/BFF service, replacing placeholders such as `SERVICE_NAME`, `IMAGE`, and port values. Keep PostgreSQL, RabbitMQ, Redis, Keycloak, and service credentials in environment-specific configuration or Kubernetes Secrets; do not commit real values.

CI validates this template for YAML syntax only and validates that service images can be built from the committed Dockerfiles. Those checks are useful for preventing malformed assets and broken container build paths, but they do not prove registry availability, namespace policy, service account permissions, ingress/TLS, autoscaling, network policy, secret wiring, rollout safety, or production readiness.

Suggested initial service names from AppHost:

- `bff`
- `api-gateway`
- `identity-api`
- `userprofile-api`
- `dish-api`
- `place-api`
- `post-api`

## Dependencies

The template intentionally does not provision PostgreSQL, RabbitMQ, Redis, or Keycloak. Those choices are environment-specific and should be managed by platform infrastructure, operators, or separate IaC.

Treat this directory as starter deployment guidance until environment-specific infrastructure, dependency ownership, readiness checks, and rollback procedures are designed and reviewed.
