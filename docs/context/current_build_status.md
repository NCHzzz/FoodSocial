# current_build_status.md

## Purpose

This file records the current implementation setup status of the repository.

## Maintenance Rule

Update this file whenever implementation status or current blockers materially change.

This file is a current-state snapshot, not a history log.

- overwrite outdated status instead of appending history
- keep only current setup state, current blockers, and current priority signals
- move useful older milestones to `docs/context/progress_log.md` or `docs/context/archive/`
- keep this file short enough to load as working context

## Snapshot
- Updated: 2026-06-05
- Scope of this update: backend code-health cleanup (Phase A) + domain test coverage (Phase D1-D3)
| Verification: backend build (Release) 0 errors / 0 warnings; 275/275 tests pass across Dish, Place, Identity (Domain + Application + Api), UserProfile, Post, ApiGateway, and BFF test suites; new Domain suites contribute Dish 11, Place 15, Post 17 (43 net new tests). AppHost tests require Docker and were not executed.


- Updated: 2026-05-15
- Scope of this update: backend DPUnity-style layering cleanup and Identity application refactor verification
- Verification: backend build and full no-build test suite passed after Identity current-user, bootstrap, and outbox idempotency updates

## Current Runtime Shape

- backend solution: `Backend/FoodSocial.slnx`
- backend source: `Backend/src/`
- frontend app: `Frontend/`
- local orchestration: `Backend/src/FoodSocial.AppHost/`
- operational starters: `ops/`

Active backend projects (after Phase A consolidation):
- `FoodSocial.AppHost`
- `FoodSocial.ServiceDefaults`
- `FoodSocial.Contracts`
- `FoodSocial.Messaging`
- `FoodSocial.ApiGateway`
- `FoodSocial.Bff`
- `Identity`
- `UserProfile`
- `Dish`
- `Place`
- `Post`

Active backend test projects (after Phase D additions):
- `FoodSocial.ApiGateway.Tests`
- `FoodSocial.Bff.Tests`
- `FoodSocial.AppHost.Tests`
- `Identity.Api.Tests`
- `Identity.Application.Tests`
- `Identity.Domain.Tests`
- `Dish.Api.Tests`
- `Dish.Domain.Tests` (new)
- `Place.Api.Tests`
- `Place.Domain.Tests` (new)
- `Post.Api.Tests`
- `Post.Domain.Tests` (new)
- `UserProfile.Api.Tests`

- `Place`
- `Post`

## Backend State

- API Gateway is the public HTTP edge for routing, gateway policy, sanitized correlation IDs, spoof-resistant rate limiting, fail-closed non-development CORS, enforced security headers, forwarded-header trust validation, request body limits, BFF readiness checks, YARP tracing, and safe gateway ProblemDetails responses.
- BFF remains downstream of the API Gateway and owns browser auth/session plus frontend-oriented service facades.
- Post service now follows the DPUnity-style `UseCases/<Capability>/` folder shape: Publishing, Queries, Engagement, Comments, Reposts, Reports, Notifications, Media. Each use case file lives in its capability folder with the matching `FoodSocial.Post.Application.UseCases.<Capability>` namespace.
- `Post/Domain/PostEntities.cs` (8 entities + 5 enums in one file) split into one type per file: `PostRecord.cs`, `PostMediaRecord.cs`, `PostMediaUploadRecord.cs`, `PostLikeRecord.cs`, `PostRepostRecord.cs`, `PostCommentRecord.cs`, `ContentReportRecord.cs`, `PostNotificationRecord.cs`, plus `PostStatus.cs`, `CommentStatus.cs`, `ReportStatus.cs`, `ReportTargetType.cs`, `NotificationStatus.cs`, `PostMediaUploadStatus.cs`.
- `Post/Api/Controllers/PostsController.cs` split into per-resource controllers: `PostsController`, `PostCommentsController`, `PostEngagementController`, `PostReportsController`, `PostNotificationsController`, `PostMediaController`, `PostMediaReadController`.
- `Bff/Endpoints/BffPostEndpoints.cs` split into `BffPostReadEndpoints`, `BffPostWriteEndpoints`, `BffPostModerationEndpoints`. New helpers: `Bff/Session/IBffSessionContext.cs`, `Bff/Session/BffSessionContext.cs`, `Bff/Errors/BffProblemTypes.cs`, `Bff/Errors/BffProblemResults.cs`.
- `UserProfile/Api/Controllers/UsersController.cs` split into `UserProfilesController`, `UserFollowsController`, `UserSavedItemsController`, `UserNotificationsController`.
- Magic-string status/role comparisons in BFF + `Post.Infrastructure.Clients.CatalogHttpClients` replaced with typed wire constants in `FoodSocial.Contracts`: `IdentityWireConstants`, `DishWireConstants`, `PlaceWireConstants`.
- `DefaultTenantId` literal centralized in `FoodSocial.Contracts.Common.TenantDefaults.Id`; all 5 service-owned infrastructure stores now reference it instead of per-entity constants.
- `UserProfile.Api.Program.cs` standardized to use `builder.AddServiceDefaults()` (was the long form `Microsoft.Extensions.Hosting.ServiceDefaultsExtensions.AddServiceDefaults(builder)`).
- `IdentityOutboxPublisherBackgroundService` adds ±25% jitter to the 2-second drain delay to avoid thundering-herd when many service instances boot together.
- BFF endpoints (auth + post + moderation + write + read) all route through the new `IBffSessionContext` for identity-orchestration, lifecycle allow/deny, and moderator-role checks; `BffProblemTypes` and `BffProblemResults` are the canonical ProblemDetails catalog.

- Backend service APIs use shared safe ProblemDetails helpers where implemented, and ServiceDefaults owns common telemetry, health, resilience, and service-discovery baseline behavior.
- Service-owned EF Core persistence exists for Identity, UserProfile, Dish, Place, and Post.
- Service-owned migrations exist; target environments must apply them through a controlled migration path.
- RabbitMQ is the async backbone with outbox-backed publishing on current service-owned event paths.
- Redis is a selective BFF catalog response cache, not a source of truth.
- PostgreSQL remains source of truth per service boundary.
- Keycloak remains the external identity provider.

## Local Orchestration State

- Aspire AppHost composes PostgreSQL, RabbitMQ, Redis, Keycloak, API Gateway, BFF, and backend APIs for local development.
- AppHost uses stable local container names and named data volumes for PostgreSQL, RabbitMQ, and Redis.
- Keycloak is Aspire-managed for local development and remains import-driven.
- AppHost is local-orchestration-only and must not be treated as the production runtime.

## Frontend State
- New xUnit domain test projects added: `Dish.Domain.Tests` (11 tests), `Place.Domain.Tests` (15 tests), `Post.Domain.Tests` (17 tests). Coverage now spans canonical Dish/Place creation, validation, normalization, and lifecycle; Post invariants including media-required, rating bounds, tag normalization, place-id-empty normalization, and `MarkAttached`/`MarkRead` lifecycle helpers.
- `BffProblemResults` ProblemDetails construction consolidated in a single helper file; earlier inline copies in `BffAuthEndpoints` and `BffPostEndpoints` removed.
- `ResolveBffIdentity` magic-string role comparison replaced with `IdentityWireConstants.RoleModerator` / `IdentityWireConstants.RoleAdmin` references.


## Ops State

- CI exists for backend restore/build/tests, frontend install/test/build, syntax validation for ops assets, Helm validation, and container build validation.
- Release image publishing and Helm deployment workflow starters exist.
- Helm and Kubernetes starters exist under `ops/`.
- Observability starters exist for OTLP Collector, Prometheus, Alertmanager, Tempo, Loki, Grafana provisioning, starter dashboard, and starter alerts.
- Production infrastructure remains operator-owned: cluster, DNS, TLS, secrets, managed datastores, media storage, alert receivers, backup/restore, and rollback drills.

## Current Verification Baseline

The latest local verification recorded these passing checks:

- `dotnet restore Backend\FoodSocial.slnx`
- `dotnet build Backend\FoodSocial.slnx --configuration Release --no-restore --maxcpucount:1`
- `dotnet test Backend\FoodSocial.slnx --configuration Release --no-build`
- `dotnet test Backend\tests\Identity\Application.Tests\FoodSocial.Identity.Application.Tests.csproj --configuration Release --no-restore`
- `dotnet test Backend\tests\Identity\Api.Tests\FoodSocial.Identity.Api.Tests.csproj --configuration Release --no-restore`
- `dotnet test Backend\tests\FoodSocial.ApiGateway.Tests\FoodSocial.ApiGateway.Tests.csproj --configuration Release --no-build`
- `dotnet test Backend\tests\FoodSocial.AppHost.Tests\FoodSocial.AppHost.Tests.csproj --configuration Release --no-build`
- `dotnet test Backend\tests\FoodSocial.AppHost.Tests\FoodSocial.AppHost.Tests.csproj --configuration Release --no-build --filter FullyQualifiedName~ServiceBoundaryArchitectureTests`
- `npm ci` from `Frontend`
- `npm audit --json` from `Frontend`
- `npm run lint` from `Frontend`
- `npm test -- --watch=false` from `Frontend`
- `npm run build` from `Frontend`
- focused backend tests across BFF, Identity, UserProfile, Dish, Place, and Post
- `.tools\helm\windows-amd64\helm.exe lint ops\helm\foodsocial`
- `.tools\helm\windows-amd64\helm.exe template foodsocial ops\helm\foodsocial --values ops\helm\foodsocial\values.staging.yaml`
- `.tools\helm\windows-amd64\helm.exe template foodsocial ops\helm\foodsocial --values ops\helm\foodsocial\values.production.yaml`
- ops YAML parsing for GitHub Actions workflows and Helm values files
- `docker build --file Backend\src\FoodSocial.ApiGateway\Dockerfile --tag foodsocial-api-gateway:ci Backend`

Helm was installed as a repo-local portable tool under `.tools\helm\windows-amd64\helm.exe` because it was not available in PATH.

## Current Blockers

- target-environment EF Core migrations must be applied and verified
- production Keycloak realm, clients, redirect URIs, audiences, roles, secrets, and backup path must be configured
- production media storage, derivative, cleanup, and recovery policy must be approved
- AI provider/model/budget/kill switch must be approved, or deterministic fallback must remain the launch posture
- block/mute and visibility policy must be verified across profile, feed, notification, and interaction paths
- production observability, alert routing, backup/restore, ingress/TLS, WAF/bot policy, secret management, and rollback drills remain environment-owned

## Current Priority

1. keep repo docs aligned to product truth and maturity stage
2. apply latest service-owned migrations in target PostgreSQL environments
3. finalize Keycloak and secret ownership for staging/production
4. approve media, AI, block/mute, visibility, and launch-threshold decisions
5. validate production-like observability, backup/restore, and rollback paths

## Delivery Artifact Note

Do not treat this file as an implementation plan. Current-state snapshots belong here; durable runbooks and checklists belong in `docs/delivery/` only when they add current value outside the launch readiness file.
