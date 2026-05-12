# Observability Examples

FoodSocial services already use `FoodSocial.ServiceDefaults` for OpenTelemetry logs, metrics, and traces. Identity and UserProfile also register dependency-aware readiness checks for PostgreSQL and RabbitMQ. The OTLP exporter is enabled by setting `OTEL_EXPORTER_OTLP_ENDPOINT`; these examples show how to collect that output without replacing the application telemetry setup.

## Local collector example

Run an OpenTelemetry Collector with `ops/observability/otel-collector.yaml`, then start services with:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

The collector accepts OTLP over gRPC (`4317`) and HTTP (`4318`) and exposes Prometheus-scrapable metrics at `9464`. It does not enable the OpenTelemetry debug exporter by default, so telemetry is not echoed to collector stdout.

## Prometheus example

`ops/observability/prometheus.yml` scrapes:

- the collector metrics endpoint at `otel-collector:9464`
- Prometheus itself at `localhost:9090`

`ops/observability/alerts/foodsocial-alerts.yml` provides starter alerts for scrape availability, HTTP 5xx rate, HTTP p95 latency, Post media cleanup failures, and moderation backlog growth. Tune thresholds per environment before treating them as an SLO contract.

In Kubernetes, prefer service discovery or `ServiceMonitor`/equivalent objects for real environments. Keep scrape targets environment-driven rather than hardcoding cluster topology into application code.

## Grafana example

`ops/observability/grafana/foodsocial-overview-dashboard.json` is a starter dashboard for service health and HTTP server latency/request rate metrics commonly produced by OpenTelemetry .NET instrumentation. Treat it as a seed dashboard, not a production SLO contract.

Validate metric names and labels against the real collector output before production use. The starter queries assume Prometheus-normalized OpenTelemetry HTTP metric names and labels such as `http_server_request_duration_seconds_*`, `http_response_status_code`, and `service_name`.

## Production extensions

Additional production-oriented starter assets are provided alongside the local examples:

- `alertmanager.yml` for alert routing placeholders.
- `tempo.yaml` for trace storage.
- `loki.yaml` for log storage.
- `grafana/provisioning/` for Prometheus, Tempo, Loki, and dashboard provisioning.
- `README-production.md` for environment-specific production decisions and validation.

## Important limits

- CI validates these files for JSON/YAML syntax only; it does not prove metric names, labels, alert thresholds, retention, paging routes, or dashboard panels are production-ready.
- These assets add starter alert thresholds, not production SLOs or retention claims.
- They do not require a specific cloud provider.
- They do not store secrets.
- They assume service names remain stable enough to filter by OpenTelemetry `service.name` labels.

Before using these examples outside local or early test environments, validate them against real exported telemetry, choose retention and scrape settings intentionally, connect alerts to the target incident process, and document ownership for dashboard and alert maintenance.
