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
- `DESIGN.md` is now the canonical FoodSocial UI/UX design source of truth, merging the previous `frontend-ui-unification.md` rules with the Claude-inspired warm editorial visual reference

### Backend skeleton

- backend solution exists at `Backend/FoodSocial.slnx`
- backend source-of-truth code lives under `Backend/src/`
- the duplicate backend solution folder was removed as a source-of-truth and reduced to leftover Visual Studio artifacts only

### Backend project structure

- `FoodSocial.AppHost`
- `FoodSocial.ServiceDefaults`
- `FoodSocial.Contracts`
- `FoodSocial.Messaging`
- `FoodSocial.Bff`
- `Identity/Api`, `Identity/Application`, `Identity/Domain`, `Identity/Infrastructure`
- `UserProfile/Api`, `UserProfile/Application`, `UserProfile/Domain`, `UserProfile/Infrastructure`
- `Dish/Api`, `Dish/Application`, `Dish/Domain`, `Dish/Infrastructure`
- `Place/Api`, `Place/Application`, `Place/Domain`, `Place/Infrastructure`
- `Post/Api`, `Post/Application`, `Post/Domain`, `Post/Infrastructure`

### Backend architecture state

- clean architecture layering exists for the first four services
- BFF exists as a separate API edge project
- BFF now exposes narrow browser-facing facades for UserProfile profile/graph/saved-item/follow-notification routes, Dish and Place read/search routes, and the Post MVP read/search/feed/create/media-upload/like/comment/repost/report/interaction-notification routes, plus moderator report-review pass-through routes, forwarding through Aspire service discovery while preserving upstream status and ProblemDetails responses and translating downstream network failures into safe BFF ProblemDetails responses
- BFF now uses Redis-backed distributed caching as its first concrete Redis consumer for short-lived anonymous Dish/Place read/search response caching, with memory-cache fallback when no Redis connection string is configured, safe uncached degradation on cache failures, and authenticated or bearer-authenticated requests bypassing the shared catalog cache
- BFF now includes config-driven rate limiting for default traffic, risky writes, media upload, and search-assist/recommendation-style paths, returning safe `429` ProblemDetails while bypassing health/OpenAPI routes
- BFF now exposes a backend-only recommendations facade with deterministic dish-primary/place-secondary fallback, a disabled-by-default AI provider abstraction, bounded limits, safe fallback reasons, and no committed provider credentials
- `ServiceDefaults` is referenced by the API projects
- default WeatherForecast sample files were removed
- Identity now has a production-oriented business slice with lifecycle state, claim-driven ensure flow, app-owned RBAC, trusted-service authorization rules, and durable outbox-backed bootstrap event publication
- UserProfile now includes the first bootstrap consumer/completion path for identity-driven shell creation and explicit completion signaling back to Identity
- UserProfile now also exposes the first application-owned profile graph slice, including profile retrieval/update, follow graph reads and mutations, owner-scoped follow notifications, and saved-item ownership APIs backed by additive EF Core persistence
- Dish now includes the backend canonical catalog foundation across domain/application/contracts/persistence/API: creation validation and normalization, active/deprecated lifecycle, database-bounded name/alias search, get-by-id, management-key-protected creation/deprecation with fixed-time key comparison, EF Core persistence, and API ProblemDetails behavior
- Place now includes the backend canonical catalog foundation across domain/application/contracts/persistence/API: creation validation and normalization, active/closed/suspended lifecycle, bounded restaurant/location search, get-by-id, management-key-protected creation/status updates with fixed-time key comparison, EF Core persistence, and API ProblemDetails behavior
- Post now includes the MVP content-core foundation across contracts/domain/application/persistence/API: trusted-caller author/dish/caption/primary-media food post creation, bounded JPEG/PNG/WebP media upload with stored original, placeholder thumbnail reference, upload ownership, cleanup-due metadata, attachment state tracking, active Dish and optional active Place catalog-reference validation at the publish boundary, read/search/discover/following-feed retrieval, idempotent likes, owner comment soft-delete, idempotent repost/reshare records, Post-owned interaction notifications for likes/comments/reposts, report intake without auto-hide, manual report resolution with hide/dismiss actions, hidden-content public read/interaction blocking, EF Core-owned Post persistence, and API ProblemDetails behavior
- Post now has EF Core migrations for the content-core schema covering `posts`, `post_media`, `post_media_uploads`, `post_likes`, `post_comments`, `post_reposts`, `post_notifications`, and `content_reports`, with Post-owned foreign keys only inside the Post boundary
- Post media cleanup lifecycle now has a trusted cleanup endpoint that marks stale pending uploads cleanup-due, deletes original/thumbnail placeholder files through the storage adapter, and preserves attached media even when cleanup timestamps are past
- FoodSocial.Contracts contains the active auth, identity, and bootstrap integration contracts used by the BFF, Identity, and UserProfile services
- FoodSocial.Contracts now also contains the current UserProfile API contracts for profile, follow, saved-item, and follow-notification flows
- FoodSocial.Contracts now also contains Dish API contracts for details, summaries, search, creation, and lifecycle responses
- FoodSocial.Contracts now also contains Place API contracts for details, summaries, search, creation, and lifecycle responses
- FoodSocial.Contracts now also contains Post API contracts for create/read/search/feed/like/comment/repost/report/notification/moderation flows

### Local orchestration state

- Aspire AppHost is present
- PostgreSQL resource is configured in AppHost
- logical PostgreSQL databases are configured for Identity, UserProfile, Dish, Place, and Post
- RabbitMQ resource is configured in AppHost and is exercised by the Identity to UserProfile bootstrap workflow
- Redis resource is configured in AppHost and referenced by BFF for catalog response caching
- Keycloak is configured as an Aspire-managed local resource with import-driven realm bootstrap for development; AppHost no longer pins Keycloak to host port 8080, so local UI access should use the Aspire Dashboard endpoint for the `keycloak` resource
- AppHost now supplies the Keycloak realm/audience configuration required by the hosted UserProfile service, keeping hosted UserProfile JWT validation aligned with Identity and BFF
- Keycloak realm bootstrap now uses environment-variable placeholders for local secrets instead of committed literal client credentials and demo passwords
- AppHost now supplies local BFF client secret, demo-user password, and internal trust secret through Aspire secret parameters at runtime
- AppHost local development now uses stable Docker container names for Aspire-managed PostgreSQL, RabbitMQ, Redis, and Keycloak to avoid accumulating new `resource-hash` containers on repeated backend runs; PostgreSQL, RabbitMQ, and Redis also use stable named data volumes, while Keycloak intentionally remains import-driven without a data volume

### Ops, CI, and observability assets

- repository-level GitHub Actions CI exists at `.github/workflows/ci.yml` for backend restore/build/non-AppHost tests, frontend `npm ci`/test/build verification, default syntax validation for committed ops JSON/YAML assets, and secret-free container build validation for the frontend plus deployable backend API/BFF projects
- repository-level .NET local tool manifest now includes `dotnet-ef` 10.0.5 so EF Core migrations can be generated reproducibly from the repo instead of relying on a machine-global tool
- release and deployment workflows now exist for GHCR image publishing and environment-gated Helm deployment through GitHub Environments with `KUBE_CONFIG`; the deploy workflow renders the Helm chart with ingress TLS values before applying changes
- AppHost container-backed tests are available through a manual GitHub Actions `workflow_dispatch` opt-in with `run-apphost-tests=true`; they require Docker on the runner and remain outside default CI
- lightweight ops assets now live under `ops/`, including OTLP collector, Prometheus scrape, Alertmanager, Tempo, Loki, Grafana provisioning, Grafana starter dashboard, Helm deployment chart, and Kubernetes service deployment/probe templates with rolling-update, startup, liveness, readiness, resource, read-only root filesystem, dropped capabilities, no privilege escalation, and baseline pod security-context settings
- observability examples are aligned with the existing `ServiceDefaults` OTLP exporter and use `OTEL_EXPORTER_OTLP_ENDPOINT` rather than replacing application telemetry setup
- Prometheus starter alerts now cover telemetry target availability, HTTP 5xx rate, and HTTP p95 latency as initial non-production thresholds
- observability dashboard and alert queries are starter examples and still require validation against actual collector-exported metric names and labels in the target environment
- Kubernetes guidance documents `/alive` as liveness and `/health` as readiness-style health behavior, with non-development endpoint exposure controlled explicitly through `ServiceDefaults__ExposeHealthEndpoints=true`; Identity and UserProfile now add PostgreSQL and RabbitMQ readiness checks, Dish/Place/Post add PostgreSQL health checks, and earlier-stage dependencies remain intentionally lightweight until deeper workflows need them

### Runtime migration policy

- Identity, UserProfile, Dish, Place, and Post APIs default EF Core startup migration application to development/AppHost-style runs; non-development startup now requires explicit `*:ApplyMigrationsOnStartup` configuration, and Helm defaults set these flags to `false` so production-style rollout uses an explicit migration job or operator-run migration step.

### Authentication and identity flow state

- BFF now also contains the browser-facing Keycloak OpenID Connect login/session/logout edge flow
- BFF session and refresh behavior are wired through the verified browser auth edge and identity orchestration path, and now reject blocked Identity lifecycle states such as Suspended and Deleted at the auth edge
- BFF browser session cookies now have explicit HttpOnly, Secure, SameSite, expiration, and sliding-expiration settings
- BFF cookie-authenticated state-changing browser endpoints now require an explicit CSRF token header; bearer-authenticated API calls are not treated as cookie-CSRF flows
- BFF Post mutations now require an authenticated browser/API identity at the edge, validate CSRF for cookie-backed writes, replace caller-supplied author/actor/reporter/moderator identifiers with the Identity orchestration identity before forwarding to Post.Api, fail fast when Post internal trust is not configured, and attach explicit internal trust headers plus role context for Post.Api mutation/notification/moderation routes
- BFF logout is now POST-only and verified through the AppHost browser flow
- Identity.Api uses authenticated claim-driven ensure behavior instead of caller-supplied subject input
- service-side auth relies on Aspire Keycloak resource wiring instead of a hardcoded localhost authority
- Identity.Api enforces app-owned RBAC policies and explicit trusted-service authorization for secured operations
- trusted internal ensure now requires an authenticated principal, a configured shared secret, and a matching authorized-party/client identifier for the trusted BFF caller

### Frontend skeleton

- Angular 21 application now lives directly at `Frontend/`
- TailwindCSS is integrated using the current Angular/Tailwind flow
- frontend structure uses `src/app/core/`, `src/app/shell/`, `src/app/features/`, and lazy route areas/foundations for discover, search, posts, profiles, following, post-create, notifications, profile, and Dish/Place search-select behavior
- frontend now includes the first auth-aware shell with guest-safe login entry, authenticated logout entry, and BFF session-state wiring
- frontend authenticated mutation calls now fetch the BFF CSRF token and send it with cookie-authenticated profile/follow/save/logout requests
- Angular local development now proxies auth-related browser routes to the BFF and depends on the local Keycloak realm allowing `http://localhost:4200` callback/origin entries for dev-server auth round-trips
- frontend now follows a clearer production Angular 21 structure with standalone bootstrap, shell-owned layout, lazy route areas, signal-driven local auth state, typed Dish/Place BFF clients, and reusable Dish/Place search-select foundations
- frontend Discover now composes real Dish/Place catalog selectors plus a live Post-backed Discover feed surface through the BFF; Search queries Dish, Place, Post, and deterministic assist BFF facades while preserving core results when assist is unavailable; Following derives a trusted feed from the follow graph plus bounded Post search; Create now publishes URL-backed or BFF-upload-backed food-first posts with thumbnail attachment, optional gallery media, and recipe notes stored through the current Post contract; dedicated Saved now gives authenticated users a direct revisit surface for saved posts plus dish/place search fallbacks; Profiles now supports opening known public profile identity links without inventing a directory endpoint; Profile now shows authored posts alongside editable taste/profile fields and saved identifiers; Notifications now show Post-owned like/comment/repost activity plus UserProfile-owned follow activity with partial-source degradation messages; Repost is BFF-backed from feed cards and post detail; report entry points are available from feed cards and post detail with explicit reason/note controls; Moderation now gives moderator/admin sessions a manual report-review queue backed by the BFF/Post.Api moderation facade

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
- focused BFF tests verify UserProfile facade forwarding, protected-route 401 behavior, cookie access-token propagation, follow-notification proxying, upstream ProblemDetails/status preservation, downstream network-failure ProblemDetails translation, and CSRF rejection for cookie-authenticated mutations without a token header
- Identity lifecycle, claim-driven ensure flow, duplicate-safe persistence, RBAC enforcement, trusted-service authorization, and durable outbox publication are covered by backend tests
- UserProfile bootstrap consumption and explicit completion signaling back to Identity are verified through the production-path AppHost suite after hosted Keycloak configuration, bounded-retry dead-letter handling, and AppHost test-isolation hardening for RabbitMQ topology/reset races
- direct UserProfile API and persistence regression tests now cover profile retrieval/update, ownership enforcement, self-follow rejection, follow graph behavior, newest-first follower/following ordering, owner-only saved items, and owner-only follow notifications
- direct Dish API and persistence regression tests now cover canonical dish creation, validation, normalization, default active search behavior, deprecation, persistence, and API ProblemDetails behavior
- direct Dish regression tests now also cover database-bounded alias search, alias-search filter correctness, empty-id ProblemDetails, unauthenticated management mutation rejection, and wrong-key management rejection
- focused BFF tests now verify Dish read/search facade forwarding, query preservation, upstream ProblemDetails preservation, downstream network-failure ProblemDetails translation, and cache hits for repeated successful catalog search responses
- direct Place API and persistence regression tests now cover canonical place creation, validation, normalization, duplicate safety, default active search behavior, city/area/cuisine/price filters, lifecycle status changes, empty-id ProblemDetails, management wrong-key rejection, persistence, and API ProblemDetails behavior
- focused BFF tests now verify Place read/search facade forwarding, query preservation, upstream ProblemDetails preservation, downstream network-failure ProblemDetails translation, and cache hits for repeated successful catalog search responses
- focused BFF tests now verify Post write-route auth rejection, CSRF rejection, edge-side author/reporter/moderator identity replacement, repost actor replacement, notification forwarding, moderator-role forwarding, following-feed composition, rate limiting, and deterministic recommendation fallback before forwarding to backend services
- direct Post API tests now cover media upload lifecycle persistence, uploaded-media attachment, unsafe upload rejection, stale pending-media cleanup with original/thumbnail deletion, attached-media retention, public hidden-content read/interaction blocking, repost/notification flows, and manual moderation hide behavior
- latest backend hardening verification passes: `dotnet build Backend/FoodSocial.slnx --configuration Release --no-restore --maxcpucount:1` succeeded with 0 warnings/errors; non-AppHost backend tests passed `188/188`; frontend `npm audit --package-lock-only` reports 0 vulnerabilities; non-template ops YAML parsing passed for 8 files; AppHost tests were excluded from this focused hardening run, and Helm rendering was not run because `helm` is not installed in the current environment
- frontend Angular app structure exists, but local build still depends on installed frontend packages in the working environment
- frontend dependency remediation is in place and `npm audit --package-lock-only` now reports zero vulnerabilities for the current checked-in lockfile
- frontend unit tests now cover the auth session service plus guest/authenticated shell rendering for the login/logout UI slice
- frontend auth/profile client tests now cover CSRF-token retrieval, malformed/failing CSRF retry behavior, logout pending-state cleanup, and header/form propagation for cookie-authenticated BFF mutations
- frontend production build now succeeds with the auth-aware shell changes
- frontend now includes typed UserProfile BFF facade models/client coverage plus a minimal public profile route foundation at `/profiles/:identityId` and a guarded own-profile entry scaffold
- frontend unit tests and production build pass for the UserProfile facade foundation slice
- frontend unit tests and production build pass for the completed Profile UX slice, including owner-only saved-state handling, delayed session hydration on public owner profiles, and public follow/follow-graph behavior
- focused frontend unit tests now cover the typed Dish BFF client and Dish search/select loading, empty, error, selected, and stale-response guard states
- focused frontend unit tests now cover the typed Place BFF client and Place search/select loading, empty, error, selected, and stale-response guard states
- focused frontend unit tests now cover typed Post BFF client behavior, Discover feed composition, deterministic Search aggregation with AI-assist degradation, Following feed assembly, upload-or-URL-backed post creation with thumbnail propagation, recipe-note and gallery-media post creation, dedicated Saved page states plus dish/place search fallbacks, feed-card and post-detail report entry behavior with explicit reason/note controls, profile identity-link lookup, profile authored-post rendering, repost/combined notification UI with partial-source degradation, moderator report-review UI, Dish/Place search-select behavior, and broader UserProfile mutation paths

### Platform maturity notes

- Redis now has a concrete BFF catalog response-cache use for short-lived Dish/Place read/search responses; it remains an optimization rather than a source of truth, and PostgreSQL-owned catalog services remain authoritative
- RabbitMQ-backed async delivery is implemented for the Identity bootstrap workflow through the shared `FoodSocial.Messaging` project, but broader cross-service messaging coverage is still limited to this first production path
- Kubernetes manifests and Helm chart are deployment starters; external dependency provisioning, cloud secret management, ingress/TLS, alert receiver wiring, retention backends, and environment-specific infrastructure remain operator-owned production work
- AppHost container-backed tests require a working Docker environment and are intentionally gated outside the default CI workflow

## Important Clarifications

- Redis is available as a selective cache or short-lived state layer, not a source-of-truth replacement
- PostgreSQL remains the source-of-truth datastore per service
- RabbitMQ remains the async messaging backbone
- Keycloak remains the chosen external identity provider and is now configured as a local Aspire-managed dependency for development

## Current Blockers

- Separate Feed, Search, Moderation, and Notification services remain deferred; Post and UserProfile own the current MVP content/feed/search/report/repost/interaction/follow-notification basics until extraction is justified
- Core browser MVP activity loops now have bounded backend implementations for following feed composition, BFF rate limits, deterministic recommendation fallback, media storage/cleanup lifecycle, feeds/search, moderation review, and notifications; Post media delivery is lifecycle-gated with pending uploads visible only to the trusted owner path and attached media publicly readable; beta exposure still requires approvals for provider/model/budget, cloud object-storage target, production derivative/cleanup workers, recommendation scope, dedicated read models/services where justified, account/system notifications, rule-based anti-spam/user warning/restriction/suspension automation, and final beta thresholds

## Current Priority

- keep internal-trust secrets environment-owned, apply generated Post and UserProfile migrations in the target PostgreSQL environment, confirm beta threshold approvals, then harden production media, recommendation, notification, and moderation automation paths without expanding MVP scope

## Launch Readiness Snapshot

- `docs/context/mvp_launch_readiness.md` now captures the remaining environment-owned launch gates and recommended beta defaults.
- Repo-side validation remains green, but production launch still waits on target-environment migrations, Keycloak config, object-storage selection, AI approval, beta thresholds, and infra wiring.

## Delivery Artifact Note

Do not treat this file as an implementation plan. Current-state snapshots belong here; active `/opsx-*` change workflow belongs in `openspec/`, and curated markdown delivery records belong in `docs/delivery/`.
