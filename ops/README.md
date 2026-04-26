# FoodSocial Ops Assets

This directory contains lightweight, repository-level operational assets for the current FoodSocial stack.

## Scope

- `.github/workflows/ci.yml` runs backend and frontend verification without secrets or external infrastructure, validates JSON/YAML assets under `ops/`, and builds service/frontend container images without pushing them.
- `ops/observability/` provides local examples for collecting the existing OpenTelemetry OTLP output and viewing Prometheus/Grafana data.
- `ops/kubernetes/` provides Kubernetes-ready deployment and probe templates aligned with the shared `/alive` and `/health` endpoints.
- `ops/helm/foodsocial/` provides a reusable Helm chart used by the deploy workflow for staging/production-style Kubernetes rollout.

## Current assumptions

- Aspire remains the default local development composition layer.
- Production deployment is Kubernetes-oriented but not yet bound to one cloud provider.
- Service telemetry is already emitted by `FoodSocial.ServiceDefaults` when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.
- `/alive` is the liveness endpoint and `/health` is the readiness-style health endpoint exposed by service defaults in development or when `ServiceDefaults__ExposeHealthEndpoints=true` is configured. Identity and UserProfile add PostgreSQL and RabbitMQ readiness checks; services without concrete infrastructure dependencies still expose only the shared self-check.

## CI notes

The default CI workflow intentionally avoids starting Aspire-managed PostgreSQL, RabbitMQ, Redis, and Keycloak containers. Container-backed AppHost verification is available only through the manual `workflow_dispatch` path with `run-apphost-tests=true`, where the runner must have Docker available.

The default ops validation checks that committed ops JSON/YAML assets parse successfully and that the committed Dockerfiles build locally on the GitHub runner. Release and deployment workflows are available separately for GHCR image publishing and Helm-based Kubernetes rollout. They require environment-owned secrets and do not provision cloud infrastructure.

## Production boundary

These assets are an operational baseline for local and early environment hardening, not a production platform. Before production use, teams still need environment-specific image publishing, secret management, dependency provisioning, ingress/TLS, Kubernetes rollout strategy, readiness semantics, alert tuning, retention, backup/restore expectations, and incident runbooks.
