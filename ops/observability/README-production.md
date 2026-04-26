# Production Observability Setup

FoodSocial emits OpenTelemetry logs, metrics, and traces through `FoodSocial.ServiceDefaults` when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.

## Components

- OpenTelemetry Collector receives application telemetry.
- Prometheus scrapes collector metrics and evaluates starter alert rules.
- Alertmanager routes alerts to environment-specific receivers.
- Tempo stores traces.
- Loki stores structured logs.
- Grafana provisions Prometheus, Tempo, and Loki datasources plus the starter dashboard.

## Required production decisions

Before public production use, replace local filesystem retention with environment-owned storage:

- Tempo: object storage such as S3/GCS/Azure Blob.
- Loki: object storage and retention aligned to compliance needs.
- Prometheus: persistent volume, retention, and optional long-term storage.
- Alertmanager: Slack/PagerDuty/Opsgenie/email webhook receivers managed through secrets.

## Validation checklist

1. Deploy the collector and set `OTEL_EXPORTER_OTLP_ENDPOINT` on every service.
2. Confirm Prometheus target `otel-collector:9464` is up.
3. Confirm Grafana can query Prometheus, Tempo, and Loki.
4. Trigger a test 5xx and verify alert evaluation without paging real users.
5. Confirm traces and logs can be correlated from one failed request.
6. Document dashboard owner and alert response owner for each environment.
