# FoodSocial Production Readiness TODO

## Purpose

This checklist records the remaining operator-owned tasks required to turn the repository CI/CD, Kubernetes, and observability starters into a real staging/production deployment.

The repo now contains starter workflows, Dockerfiles, Helm chart, Kubernetes templates, and observability configuration. The items below require environment-specific accounts, clusters, domains, secrets, receivers, and managed infrastructure decisions.

## 1. GitHub Environments

- [ ] Create GitHub Environment `staging`.
- [ ] Create GitHub Environment `production`.
- [ ] Add required reviewers for `production`.
- [ ] Restrict `production` deployments to trusted branches or SemVer tags such as `v*.*.*`.
- [ ] Add environment secret `KUBE_CONFIG` to `staging`.
- [ ] Add environment secret `KUBE_CONFIG` to `production`.

## 2. Kubernetes Deploy Identity

- [ ] Create a least-privilege Kubernetes service account for deployment.
- [ ] Bind deploy permissions only to the target namespace.
- [ ] Generate a kubeconfig for that deployer identity.
- [ ] Store the kubeconfig as GitHub Environment secret `KUBE_CONFIG`.
- [ ] Verify the deployer can run `helm upgrade --install` in the target namespace.
- [ ] Verify the deployer can read rollout status for FoodSocial deployments.

## 3. GHCR Image Publishing and Pulling

- [ ] Confirm GitHub Actions workflow permissions allow `packages: write`.
- [ ] Run `Release Images` once and confirm all six images publish to GHCR:
  - [ ] `bff`
  - [ ] `identity-api`
  - [ ] `userprofile-api`
  - [ ] `dish-api`
  - [ ] `post-api`
  - [ ] `web`
- [ ] Decide whether GHCR packages are public or private.
- [ ] If private, create a GitHub token with `read:packages` for the cluster.
- [ ] Create Kubernetes `imagePullSecret` for GHCR in each deployment namespace.
- [ ] Add `imagePullSecrets` support to Helm values/templates if private pulling is required.

## 4. Image Repository Owner

- [ ] Confirm the GitHub owner/org used for image paths.
- [ ] Update `ops/helm/foodsocial/values.staging.yaml` with the real image repository if not using workflow overrides.
- [ ] Update `ops/helm/foodsocial/values.production.yaml` with the real image repository if not using workflow overrides.
- [ ] Confirm deployed image references resolve to `ghcr.io/<owner>/foodsocial/<service>:<tag>`.

## 5. Ingress and TLS

- [ ] Provision or select a Kubernetes ingress controller.
- [ ] Install NGINX Ingress or equivalent.
- [ ] Install cert-manager or equivalent certificate automation.
- [ ] Create a production ACME issuer or equivalent TLS issuer.
- [ ] Choose staging domain name.
- [ ] Choose production domain name.
- [ ] Configure DNS records to the ingress load balancer.
- [ ] Enable Helm `ingress.enabled` for staging.
- [ ] Enable Helm `ingress.enabled` for production.
- [ ] Add TLS configuration and certificate issuer annotations.
- [ ] Verify HTTPS works for the web frontend.
- [ ] Verify `/api` or BFF route forwarding works through ingress.

## 6. Secret Management

- [ ] Choose initial secret management mode:
  - [ ] Kubernetes Secrets for early beta.
  - [ ] External Secrets Operator for production-grade cloud secret sync.
  - [ ] Vault or cloud-native secret manager for stronger controls.
- [ ] Store BFF Keycloak client secret.
- [ ] Store Identity internal trust shared secret.
- [ ] Store PostgreSQL connection strings.
- [ ] Store RabbitMQ connection string.
- [ ] Store Redis connection string if Redis becomes active.
- [ ] Store Alertmanager receiver secrets.
- [ ] Define secret rotation owner and cadence.
- [ ] Ensure no real secrets are committed to the repo.

## 7. PostgreSQL Provisioning and Backups

- [ ] Choose managed PostgreSQL or self-hosted PostgreSQL.
- [ ] Create service-owned databases:
  - [ ] `identitydb`
  - [ ] `userprofiledb`
  - [ ] `dishdb`
  - [ ] `postdb`
- [ ] Create least-privilege users per service if possible.
- [ ] Store connection strings in the selected secret manager.
- [ ] Enable automated backups.
- [ ] Enable point-in-time recovery if supported.
- [ ] Define retention duration.
- [ ] Run and document a restore test.
- [ ] Add operator contact/owner for database incidents.

## 8. RabbitMQ Provisioning and Backups

- [ ] Choose managed RabbitMQ, RabbitMQ Cluster Operator, or Helm-based self-hosting.
- [ ] Provision RabbitMQ for staging.
- [ ] Provision RabbitMQ for production.
- [ ] Create application user and vhost if applicable.
- [ ] Store RabbitMQ connection string in the selected secret manager.
- [ ] Confirm Identity and UserProfile RabbitMQ readiness checks pass.
- [ ] Configure durable queues and persistent messages where required.
- [ ] Define retry/dead-letter monitoring expectations.
- [ ] Configure RabbitMQ definitions export or equivalent backup path.
- [ ] Monitor queue depth and consumer health.

## 9. Redis Provisioning

- [ ] Decide whether Redis is needed before production launch.
- [ ] If needed, choose managed Redis or self-hosted Redis.
- [ ] Provision Redis for staging.
- [ ] Provision Redis for production.
- [ ] Configure Redis auth and network access.
- [ ] Store Redis connection string in the selected secret manager.
- [ ] Define eviction policy and memory limits.
- [ ] Decide whether Redis needs backups based on actual usage.

## 10. Keycloak Provisioning and Backups

- [ ] Choose Keycloak deployment mode:
  - [ ] Keycloak Operator.
  - [ ] Helm chart.
  - [ ] Managed identity provider alternative.
- [ ] Provision Keycloak for staging.
- [ ] Provision Keycloak for production.
- [ ] Back Keycloak with production PostgreSQL, not ephemeral dev storage.
- [ ] Create/import the `foodsocial` realm.
- [ ] Configure BFF client and redirect URIs.
- [ ] Configure API audience and roles.
- [ ] Store client secrets in the selected secret manager.
- [ ] Define realm export cadence.
- [ ] Back up Keycloak PostgreSQL database.
- [ ] Document admin recovery path.

## 11. Observability Stack

- [ ] Deploy OpenTelemetry Collector.
- [ ] Deploy Prometheus with persistent storage.
- [ ] Deploy Alertmanager.
- [ ] Deploy Tempo for traces.
- [ ] Deploy Loki for logs.
- [ ] Deploy Grafana.
- [ ] Provision Grafana datasources from `ops/observability/grafana/provisioning/`.
- [ ] Provision FoodSocial dashboard JSON.
- [ ] Confirm `OTEL_EXPORTER_OTLP_ENDPOINT` is set for every service.
- [ ] Confirm Prometheus target `otel-collector:9464` is up.
- [ ] Confirm Grafana can query Prometheus.
- [ ] Confirm Grafana can query Tempo.
- [ ] Confirm Grafana can query Loki.
- [ ] Confirm one request can be traced from BFF into backend services.
- [ ] Confirm application logs appear in Loki.

## 12. Alertmanager Receivers

- [ ] Choose alerting channels:
  - [ ] Slack for warnings.
  - [ ] PagerDuty for critical alerts.
  - [ ] Email or another fallback if needed.
- [ ] Create Slack Incoming Webhook.
- [ ] Create PagerDuty Events API v2 integration key if PagerDuty is used.
- [ ] Store receiver secrets in the selected secret manager.
- [ ] Wire `warning` alerts to Slack or equivalent.
- [ ] Wire `critical` alerts to PagerDuty or equivalent.
- [ ] Test alert routing with a synthetic alert.
- [ ] Test alert resolution notification.
- [ ] Confirm inhibit rules prevent duplicate alert spam.

## 13. Deployment Verification

- [ ] Run `Release Images` workflow with a known tag.
- [ ] Run `Deploy Kubernetes` workflow to staging.
- [ ] Confirm all staging pods become ready.
- [ ] Confirm staging `/alive` endpoints return success.
- [ ] Confirm staging `/health` endpoints return success.
- [ ] Confirm BFF browser auth flow works through staging ingress.
- [ ] Confirm Identity/UserProfile PostgreSQL readiness checks work.
- [ ] Confirm Identity/UserProfile RabbitMQ readiness checks work.
- [ ] Run a rollback test with `helm rollback`.
- [ ] Promote the same tested image tag to production only after staging verification.

## 14. Incident and Recovery Readiness

- [ ] Document who owns production deploys.
- [ ] Document who owns database recovery.
- [ ] Document who owns Keycloak recovery.
- [ ] Document who receives critical alerts.
- [ ] Document rollback command and expected recovery time.
- [ ] Document database restore command or provider restore process.
- [ ] Document RabbitMQ recovery process.
- [ ] Document Keycloak realm restore process.
- [ ] Schedule periodic restore tests.

## Definition of Done

This production readiness checklist is complete when:

- [ ] Images can be published from GitHub Actions.
- [ ] Staging can deploy from GitHub Actions using Helm.
- [ ] Production deploy requires approval and uses the same Helm path.
- [ ] All application services pass Kubernetes readiness checks.
- [ ] Metrics, logs, and traces are visible in Grafana.
- [ ] Alerts route to real human-owned channels.
- [ ] PostgreSQL, RabbitMQ, Redis if used, and Keycloak have documented backup/restore paths.
- [ ] A rollback has been tested successfully.
