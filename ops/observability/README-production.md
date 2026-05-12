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

## Starter runbooks

### Post media cleanup failure

Check `Post.Api` logs for `Post media cleanup failed for upload`. Confirm the media volume or object-style storage path is mounted and writable. Re-run the trusted cleanup endpoint after fixing storage permissions. Do not mark lifecycle rows deleted until original and thumbnail files are removed or intentionally quarantined.

### Moderation backlog growth

Open the moderator report queue through the BFF/Post moderation facade. Confirm moderators can resolve reports and that `foodsocial_post_content_reports_resolved_total` is increasing. If reports are abusive or spam-heavy, temporarily reduce beta exposure and increase manual review capacity; MVP must stay manual-review-first.

### Queue or outbox backlog

Check RabbitMQ queue depth, consumer logs, and Identity outbox delivery state before retrying. Prefer replaying idempotent messages after the consumer is healthy; do not delete poison messages until the payload and failure reason are captured for follow-up.
