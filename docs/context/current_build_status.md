# current_build_status.md

## Purpose

This file records the current implementation setup status of the repository.

## Maintenance Rule

Update this file whenever implementation status or current blockers materially change.

This file is a current-state snapshot, not a history log.

- overwrite outdated status instead of appending history
- keep only the current setup state, current blockers, and current priority signals
- remove stale completed details when they no longer help near-term implementation
- keep this file short enough to load as working context without unnecessary token cost

## Current Setup Completed

### Repository and documentation

- project-discovery documentation has been heavily reviewed and cleaned up
- AI operating docs have been simplified and aligned to the repository structure
- architecture, backend, API, and domain docs now reflect the current direction more accurately

### Backend skeleton

- backend solution exists at `Backend/FoodSocial.slnx`
- backend source-of-truth code lives under `Backend/src/`
- the duplicate backend solution folder was removed as a source-of-truth and reduced to leftover Visual Studio artifacts only

### Backend project structure

- `FoodSocial.AppHost`
- `FoodSocial.ServiceDefaults`
- `FoodSocial.Contracts`
- `FoodSocial.Bff`
- `Identity/Api`, `Identity/Application`, `Identity/Domain`, `Identity/Infrastructure`
- `UserProfile/Api`, `UserProfile/Application`, `UserProfile/Domain`, `UserProfile/Infrastructure`
- `Dish/Api`, `Dish/Application`, `Dish/Domain`, `Dish/Infrastructure`
- `Post/Api`, `Post/Application`, `Post/Domain`, `Post/Infrastructure`

### Backend architecture state

- clean architecture layering exists for the first four services
- BFF exists as a separate API edge project
- BFF now exposes a narrow browser-facing UserProfile facade for profile, follow graph, and saved-item routes, forwarding to the UserProfile service through Aspire service discovery with cookie-or-bearer token propagation for protected operations
- `ServiceDefaults` is referenced by the API projects
- default WeatherForecast sample files were removed
- Identity now has a production-oriented business slice with lifecycle state, claim-driven ensure flow, app-owned RBAC, trusted-service authorization rules, and durable outbox-backed bootstrap event publication
- UserProfile now includes the first bootstrap consumer/completion path for identity-driven shell creation and explicit completion signaling back to Identity
- UserProfile now also exposes the first application-owned profile graph slice, including profile retrieval/update, follow graph reads and mutations, and saved-item ownership APIs backed by additive EF Core persistence
- FoodSocial.Contracts contains the active auth, identity, and bootstrap integration contracts used by the BFF, Identity, and UserProfile services
- FoodSocial.Contracts now also contains the current UserProfile API contracts for profile, follow, and saved-item flows

### Local orchestration state

- Aspire AppHost is present
- PostgreSQL resource is configured in AppHost
- logical PostgreSQL databases are configured for Identity, UserProfile, Dish, and Post
- RabbitMQ resource is configured in AppHost and is exercised by the Identity to UserProfile bootstrap workflow
- Redis resource is configured in AppHost
- Keycloak is configured as an Aspire-managed local resource with import-driven realm bootstrap for development
- AppHost now supplies the Keycloak realm/audience configuration required by the hosted UserProfile service, keeping hosted UserProfile JWT validation aligned with Identity and BFF
- Keycloak realm bootstrap now uses environment-variable placeholders for local secrets instead of committed literal client credentials and demo passwords
- AppHost now supplies local BFF client secret, demo-user password, and internal trust secret through Aspire secret parameters at runtime
- AppHost-backed verification now uses session-scoped local infrastructure resources and service-owned database creation before migrations, which keeps fresh local verification runs reproducible

### Ops, CI, and observability assets

- repository-level GitHub Actions CI exists at `.github/workflows/ci.yml` for backend restore/build/non-AppHost tests, frontend `npm ci`/test/build verification, default syntax validation for committed ops JSON/YAML assets, and secret-free container build validation for the frontend plus deployable backend API/BFF projects
- release and deployment workflows now exist for GHCR image publishing and environment-gated Helm deployment through GitHub Environments with `KUBE_CONFIG`
- AppHost container-backed tests are available through a manual GitHub Actions `workflow_dispatch` opt-in with `run-apphost-tests=true`; they require Docker on the runner and remain outside default CI
- lightweight ops assets now live under `ops/`, including OTLP collector, Prometheus scrape, Alertmanager, Tempo, Loki, Grafana provisioning, Grafana starter dashboard, Helm deployment chart, and Kubernetes service deployment/probe templates with rolling-update, startup, liveness, readiness, resource, and baseline security-context settings
- observability examples are aligned with the existing `ServiceDefaults` OTLP exporter and use `OTEL_EXPORTER_OTLP_ENDPOINT` rather than replacing application telemetry setup
- Prometheus starter alerts now cover telemetry target availability, HTTP 5xx rate, and HTTP p95 latency as initial non-production thresholds
- observability dashboard and alert queries are starter examples and still require validation against actual collector-exported metric names and labels in the target environment
- Kubernetes guidance documents `/alive` as liveness and `/health` as readiness-style health behavior, with non-development endpoint exposure controlled explicitly through `ServiceDefaults__ExposeHealthEndpoints=true`; Identity and UserProfile now add PostgreSQL and RabbitMQ readiness checks while earlier-stage services remain self-check only until their dependencies are implemented

### Authentication and identity flow state

- BFF now also contains the browser-facing Keycloak OpenID Connect login/session/logout edge flow
- BFF session and refresh behavior are wired through the verified browser auth edge and identity orchestration path, and now reject blocked Identity lifecycle states such as Suspended and Deleted at the auth edge
- BFF browser session cookies now have explicit HttpOnly, Secure, SameSite, expiration, and sliding-expiration settings
- BFF logout is now POST-only and verified through the AppHost browser flow
- Identity.Api uses authenticated claim-driven ensure behavior instead of caller-supplied subject input
- service-side auth relies on Aspire Keycloak resource wiring instead of a hardcoded localhost authority
- Identity.Api enforces app-owned RBAC policies and explicit trusted-service authorization for secured operations
- trusted internal ensure now requires an authenticated principal, a configured shared secret, and a matching authorized-party/client identifier for the trusted BFF caller

### Frontend skeleton

- Angular 21 application now lives directly at `Frontend/`
- TailwindCSS is integrated using the current Angular/Tailwind flow
- frontend structure uses `src/app/core/`, `src/app/shell/`, `src/app/features/`, and lazy route areas for discover, posts, profiles, following, post-create, notifications, and profile
- frontend now includes the first auth-aware shell with guest-safe login entry, authenticated logout entry, and BFF session-state wiring
- Angular local development now proxies auth-related browser routes to the BFF and depends on the local Keycloak realm allowing `http://localhost:4200` callback/origin entries for dev-server auth round-trips
- frontend now follows a clearer production Angular 21 structure with standalone bootstrap, shell-owned layout, lazy route areas, and signal-driven local auth state

### Verification already completed

- backend solution builds successfully
- Identity domain, application, and API tests pass successfully
- AppHost builds successfully
- BFF and Identity.Api build successfully after Aspire Keycloak client integration
- backend solution restore/build now passes on WSL with .NET SDK 10.0.203 installed user-locally under `~/.dotnet`
- backend CI-scope tests now pass for BFF, Identity Domain/Application/API, and UserProfile API after the Profile UX and Ops/CI changes
- backend vulnerable-package scan reports no vulnerable packages after the OpenTelemetry package patch update in central package management
- initial Identity and UserProfile EF Core migrations were generated and applied successfully for the current production path
- AppHost brings up PostgreSQL, Redis, RabbitMQ, and Keycloak successfully for local development
- Docker Desktop is reachable from the Ubuntu-24.04 WSL environment through the `desktop-linux` context, and container execution is verified with `docker run --rm hello-world`
- Keycloak issues access tokens successfully through the imported `foodsocial` realm
- BFF login now redirects correctly to the Keycloak login page through the local auth edge
- the BFF browser auth edge, authenticated session path, refresh behavior, and POST-only logout behavior are verified through AppHost-backed tests
- focused BFF tests verify blocked lifecycle-state handling for session/refresh and explicit browser-cookie settings
- focused BFF tests verify UserProfile facade forwarding, protected-route 401 behavior, cookie access-token propagation, and upstream ProblemDetails/status preservation
- Identity lifecycle, claim-driven ensure flow, duplicate-safe persistence, RBAC enforcement, trusted-service authorization, and durable outbox publication are covered by backend tests
- UserProfile bootstrap consumption and explicit completion signaling back to Identity are verified through the production-path AppHost suite after hosted Keycloak configuration, bounded-retry dead-letter handling, and AppHost test-isolation hardening for RabbitMQ topology/reset races
- direct UserProfile API and persistence regression tests now cover profile retrieval/update, ownership enforcement, self-follow rejection, follow graph behavior, and owner-only saved items
- the full AppHost-backed verification suite passes end to end against the live local Docker-backed platform: `17/17` tests pass from WSL
- frontend Angular app structure exists, but local build still depends on installed frontend packages in the working environment
- frontend dependency remediation is in place and `npm audit --package-lock-only` now reports zero vulnerabilities for the current checked-in lockfile
- frontend unit tests now cover the auth session service plus guest/authenticated shell rendering for the login/logout UI slice
- frontend production build now succeeds with the auth-aware shell changes
- frontend now includes typed UserProfile BFF facade models/client coverage plus a minimal public profile route foundation at `/profiles/:identityId` and a guarded own-profile entry scaffold
- frontend unit tests and production build pass for the UserProfile facade foundation slice
- frontend unit tests and production build pass for the completed Profile UX slice, including owner-only saved-state handling, delayed session hydration on public owner profiles, and public follow/follow-graph behavior

### Platform maturity notes

- Redis is provisioned and available, but it is not yet used by a concrete cache or ephemeral-state implementation in the current codebase
- RabbitMQ-backed async delivery is implemented for the Identity bootstrap workflow, but broader cross-service messaging coverage is still limited to this first production path
- Kubernetes manifests and Helm chart are deployment starters; external dependency provisioning, cloud secret management, ingress/TLS, alert receiver wiring, retention backends, and environment-specific infrastructure remain operator-owned production work
- AppHost container-backed tests require a working Docker environment and are intentionally gated outside the default CI workflow

## Important Clarifications

- Redis is available as a selective cache or short-lived state layer, not a source-of-truth replacement
- PostgreSQL remains the source-of-truth datastore per service
- RabbitMQ remains the async messaging backbone
- Keycloak remains the chosen external identity provider and is now configured as a local Aspire-managed dependency for development

## Current Blockers

- Dish and Post still remain earlier-stage service slices compared with the completed Identity path
- Redis usage is still intentionally deferred until a measured cache or short-lived-state need exists
- Dish and Post still expose only self health checks until their concrete infrastructure dependencies are implemented

## Current Priority

- continue from the verified Identity/UserProfile/BFF foundation into the next MVP vertical slice, with Dish, Place, and Post still earlier-stage compared with the completed Identity/UserProfile path

## Delivery Artifact Note

Do not treat this file as an implementation plan. Current-state snapshots belong here; active `/opsx-*` change workflow belongs in `openspec/`, and curated markdown delivery records belong in `docs/delivery/`.
