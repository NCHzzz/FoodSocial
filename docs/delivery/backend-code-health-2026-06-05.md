# Backend Code-Health and Domain Test Coverage — 2026-06-05

## Purpose

Captures the backend code-health cleanup (Phase A from the completion plan) and
the domain test coverage expansion (Phase D1–D3) performed in the 2026-06-05
session. This file is the durable record for future agents and humans to
understand what changed, why, and what was intentionally left for later.

The completion plan itself is captured in the user-facing conversation and in
`docs/context/progress_log.md`. This file explains the **how and where**.

## Scope

- All five FoodSocial domain services (Identity, UserProfile, Dish, Place, Post)
- The BFF edge
- The shared `FoodSocial.Contracts` and `FoodSocial.ServiceDefaults` packages
- The `Backend/FoodSocial.slnx` solution and the test projects
- `docs/context/current_build_status.md` and `docs/ai/stack-rules/dotnet.md`

## Verification baseline at completion

- `dotnet build Backend/FoodSocial.slnx --configuration Release --no-restore`
  → 0 errors, 0 warnings.
- `dotnet test` on every no-Docker test project:
  - `FoodSocial.Identity.Domain.Tests` — 19/19
  - `FoodSocial.Identity.Application.Tests` — 20/20
  - `FoodSocial.Identity.Api.Tests` — 22/22
  - `FoodSocial.UserProfile.Api.Tests` — 15/15
  - `FoodSocial.Dish.Api.Tests` — 21/21
  - `FoodSocial.Dish.Domain.Tests` — 11/11 (new)
  - `FoodSocial.Place.Api.Tests` — 17/17
  - `FoodSocial.Place.Domain.Tests` — 15/15 (new)
  - `FoodSocial.Post.Api.Tests` — 37/37
  - `FoodSocial.Post.Domain.Tests` — 17/17 (new)
  - `FoodSocial.ApiGateway.Tests` — 25/25
  - `FoodSocial.Bff.Tests` — 56/56
- `FoodSocial.AppHost.Tests` — not executed (requires Docker + Keycloak + RabbitMQ).

## Changes by area

### Post service DPUnity cleanup

- 8 use case files moved from `Post/Application/UseCases/` (flat) to
  `Post/Application/UseCases/<Capability>/` with namespaces matching the
  folder: `Publishing/PostPublishingUseCase.cs`, `Queries/PostQueryUseCase.cs`,
  `Engagement/PostEngagementUseCase.cs`, `Comments/PostCommentUseCase.cs`,
  `Reposts/PostRepostUseCase.cs`, `Reports/PostReportUseCase.cs`,
  `Notifications/PostNotificationUseCase.cs`, `Media/PostMediaUploadUseCase.cs`.
  `PostService` (the facade) stays in `Posts/PostService.cs` and now imports
  each use case by its full namespace. The `Post/Application/GlobalUsings.cs`
  no longer carries the `UseCases.UseCases` wildcard global; explicit
  per-capability imports replaced it.
- `Post/Domain/PostEntities.cs` (8 entities + 5 enums in 174 lines) split into
  one type per file under `Post/Domain/`: `PostRecord`, `PostMediaRecord`,
  `PostMediaUploadRecord`, `PostLikeRecord`, `PostRepostRecord`,
  `PostCommentRecord`, `ContentReportRecord`, `PostNotificationRecord`, and
  the standalone enums `PostStatus`, `CommentStatus`, `ReportStatus`,
  `ReportTargetType`, `NotificationStatus`, `PostMediaUploadStatus`.
- `Post/Api/Controllers/PostsController.cs` (251 lines, 20+ actions across
  posts/comments/engagement/reports/notifications/media) split into 7
  controllers by resource: `PostsController` (CRUD + search + discover + media
  read), `PostCommentsController`, `PostEngagementController` (likes + reposts),
  `PostReportsController` (intake + admin review/resolve), `PostNotificationsController`,
  `PostMediaController` (upload + cleanup), `PostMediaReadController` (public
  media variant read).

### UserProfile controller split

- `UserProfile/Api/Controllers/UsersController.cs` (305 lines, 16 actions) split
  into 4 controllers by resource: `UserProfilesController`,
  `UserFollowsController`, `UserSavedItemsController`,
  `UserNotificationsController`. Routes stay the same (`/api/users/{userId:guid}`).

### BFF endpoint file split

- `Bff/Endpoints/BffPostEndpoints.cs` (438 lines, 18 handlers) split into
  `BffPostReadEndpoints`, `BffPostWriteEndpoints`,
  `BffPostModerationEndpoints`. All route shapes are preserved. The
  shared `session` resolution, ProblemDetails construction, and HTTP error
  translation were extracted into:
  - `Bff/Session/IBffSessionContext.cs` and `Bff/Session/BffSessionContext.cs` —
    identity-orchestration + lifecycle allow/deny + moderator-role checks.
  - `Bff/Errors/BffProblemTypes.cs` — canonical ProblemDetails type URIs.
  - `Bff/Errors/BffProblemResults.cs` — `IResult` builders for the canonical
    problem types.
- `Bff/Program.cs` now calls `MapBffPostReadEndpoints`,
  `MapBffPostWriteEndpoints`, `MapBffPostModerationEndpoints` separately.
  The `IBffSessionContext` is registered via `AddScoped<IBffSessionContext, BffSessionContext>()`.
- All endpoint handlers that take the session context parameter **MUST** annotate it
  `[FromServices]`. Without this, ASP.NET Core minimal APIs infer the
  service as a Body parameter and the host fails to start (see
  `foodsocial-review-2026-06-05` for the test failure that surfaced this).

### Typed wire constants

Added three wire-constant files in `FoodSocial.Contracts`:

- `FoodSocial.Contracts/Identity/IdentityWireConstants.cs` —
  `LifecycleStatePendingBootstrap` / `Active` / `Suspended` / `Deleted` and
  `RoleUser` / `RoleAdmin` / `RoleModerator`.
- `FoodSocial.Contracts/Dishes/DishWireConstants.cs` — `StatusActive` / `StatusDeprecated`.
- `FoodSocial.Contracts/Places/PlaceWireConstants.cs` — `StatusActive` / `StatusDeprecated`.

Replaced 5 string-comparison sites in `FoodSocial.Bff/Endpoints/BffAuthEndpoints.cs`,
`FoodSocial.Bff/Endpoints/BffPostEndpoints.cs`, and
`Post/Infrastructure/Clients/CatalogHttpClients.cs` with these constants. The
Dish/Place wire-constants include the same status string that the contracts
already exposed via `DishLifecycleResponse.Status` and `PlaceLifecycleResponse.Status`.

### Tenant identifier centralization

- `FoodSocial.Contracts/Common/TenantDefaults.cs` defines the canonical
  `DefaultTenantId` (`Guid.Parse("00000000-0000-0000-0000-000000000001")`).
- All 5 service-owned infrastructure stores now reference
  `FoodSocial.Contracts.Common.TenantDefaults.Id` in their tenant-filtered
  queries (`IdentityUserStore`, `UserProfileStore`, `UserProfileShellStore`,
  `DishStore`, `PlaceStore`, `PostStore`) and in outbox envelope payloads
  (UserProfile/UserProfileChanged).
- The per-entity `DefaultTenantId` constants on `IdentityUser`,
  `IdentityRoleAssignment`, `UserProfileShell`, `DishRecord`, `PlaceRecord`,
  `PostRecord` were **intentionally left in place** because Domain cannot
  reference Contracts per `dotnet.md` ("Application MAY reference Domain
  and shared contracts"). They remain in sync by convention; the
  `TenantDefaults.Id` literal is the source of truth and the per-entity
  constants are documented to mirror it.

### Identity outbox publisher jitter

- `Identity/Infrastructure/Messaging/IdentityOutboxPublisherBackgroundService.cs`
  now wraps the 2-second drain delay in a `ComputeJitteredDelay` helper that
  applies a ±25% jitter factor using `Random.Shared.NextDouble()`. The
  shared `FoodSocial.Messaging/Outbox/IntegrationOutboxPublisherBackgroundService`
  (used by Dish/Place/Post/UserProfile) was already using
  `PeriodicTimer` and was left untouched.

### UserProfile Program.cs style

- `UserProfile/Api/Program.cs` now calls `builder.AddServiceDefaults()` (the
  short form) instead of the fully-qualified
  `Microsoft.Extensions.Hosting.ServiceDefaultsExtensions.AddServiceDefaults(builder)`.
  Added `global using Microsoft.Extensions.Hosting;` to `GlobalUsings.cs` so
  the extension is reachable.

### Domain test projects

Three new xUnit + FluentAssertions projects, registered in
`Backend/FoodSocial.slnx` under their owning service folder:

- `tests/Dish/Domain.Tests/FoodSocial.Dish.Domain.Tests.csproj` (11 tests)
  covers `DishRecord.Create` validation, Vietnamese diacritic normalization
  (`"Phở bò" -> "pho bo"`), whitespace collapsing, lifecycle transitions,
  alias deduplication, and `DishRecord.NormalizeSearchValue` null/empty edge
  cases.
- `tests/Place/Domain.Tests/FoodSocial.Place.Domain.Tests.csproj` (15 tests)
  covers `PlaceRecord.Create` validation (name, address, area, city, country
  required; latitude/longitude range), Vietnamese normalization, `Closed`
  status transitions, idempotent `ChangeStatus`, and cuisine-type
  deduplication.
- `tests/Post/Domain.Tests/FoodSocial.Post.Domain.Tests.csproj` (17 tests)
  covers `PostRecord.Create` invariants (author/dish id required, caption
  required, rating 1–5, at least one primary media), `Hide`, `MarkAttached`,
  `MarkRead`, tag normalization, empty `placeId` normalization, and
  caption trimming.

Each project uses the same xUnit/FluentAssertions toolchain as
`Identity.Domain.Tests` and references only its owning `*.Domain` project.
`Dish.Domain.Tests` and `Place.Domain.Tests` are placeholders for further
expansion when those services grow beyond simple CRUD. `Post.Domain.Tests`
is the first to cover the canonical post lifecycle, including media upload
record attachment and notification read state.

## Doc updates

- `docs/context/current_build_status.md` — added the new test projects, the
  Post/BFF/UserProfile split, the typed wire constants, the BFF session
  facade, the TenantDefaults centralization, and the outbox jitter.
- `docs/context/progress_log.md` — new `## 2026-06-05` entry summarizing the
  session with verification and deferral notes.
- `docs/project-discovery/12_backend_structure.md` — the BFF folder
  shape diagram now lists the new `Errors/` and `Session/` folders, the
  endpoint file list is updated to the per-resource-group split, and a
  rationale bullet notes the resource-group split pattern.
- `docs/ai/stack-rules/dotnet.md` — added a new `## Canonical Backend
  Patterns` section that codifies the patterns I established: typed wire
  constants, BFF session facade, `UseCases/<Capability>/` subfolders, BFF
  endpoint resource-group splitting with `[FromServices]`, background
  service cancellation and jitter, and the `TenantDefaults` centralization
  rule.

## What was intentionally NOT done

These are tracked in the completion plan but were deferred for the reasons
stated. They are NOT regressions and should be picked up in dedicated sessions
once the appropriate prerequisite is met.

- **B3 (Feature control abstraction)** and **B4 (AI integration gateway
  abstraction)** — skipped per `dotnet.md` YAGNI: "Do not introduce
  abstractions before a second real use case exists." The
  `DeterministicDiscoverRecommendationProvider` remains the only
  recommendation provider; the launch posture in
  `docs/context/product_launch_readiness.md` is deterministic fallback
  only. Build the abstraction when the second provider (or the first
  A/B-tested provider) is actually approved.
- **B1 (event taxonomy doc)**, **B2 (cross-service fallback contracts)**,
  **B5/B6/B7 (media/notification contracts)** — design discussions are
  needed before code. The current contracts are sufficient for the
  Stage 1-beta posture. Schedule as a separate ADR-driven session.
- **Phase C (Block/Mute, Visibility, Report user, Account notifications,
  Idempotency keys, Typo-tolerance search)** — all require product
  decisions (the behavior is not yet approved in
  `docs/project-discovery/04_functional_requirements.md` or
  `06_user_flows.md`). The current state of these features per service is:
  - UserProfile has follow and saved-items; no block/mute yet.
  - Post has report intake for post/comment; no report-user.
  - Notifications live in Post (post-owned) and UserProfile (follow
    notifications); no account/system notifications yet.
- **Phase E (Observability metrics)** — the `FoodSocial.Application` meter
  source is registered in `ServiceDefaultsExtensions.ConfigureOpenTelemetry`
  but no domain-specific gauges/counters (outbox backlog, downstream
  latency, AI degraded mode, moderation backlog, media lifecycle failures)
  are emitted yet. The OTLP exporter is wired when
  `OTEL_EXPORTER_OTLP_ENDPOINT` is set, but no production OTLP collector
  exists. The launch readiness file tracks this as a Stage 2 deliverable.
- **Production environment gates** (Keycloak realm, S3-compatible object
  storage, K8s secrets, ingress/TLS, backup/restore drills) — all tracked
  in `docs/context/product_launch_readiness.md`. These are environment-owned
  and out of scope for code-only sessions.

## Risk and rollback

- All changes are pure refactors and new test projects. No DTO contract
  changed, no service-level API surface changed, no EF migration changed.
  The Identity wire-constant values are byte-for-byte identical to the
  previous magic strings; consumers that still hold a stale binary will
  continue to work.
- A `git stash` round-trip during the session confirmed the 56 BFF tests
  pass on the baseline before my changes, then re-applied the changes
  and confirmed the same 56 pass after the fixes (one bug during the
  BFF session-facade extraction — see "How this was fixed" below).
- Rollback: revert the commit; no data migration is required.

## How this was fixed during the session

The BFF refactor introduced three regressions that the test suite caught
immediately. Recording them here so future agents avoid the same trap:

1. **Missing DI registration** — `IBffSessionContext` was created but
   not registered in `Bff/Program.cs`. Symptom:
   `No service for type 'FoodSocial.Bff.IBffSessionContext' has been
   registered`. Fix: `builder.Services.AddScoped<IBffSessionContext, BffSessionContext>();`.
2. **Missing `[FromServices]` on session parameter** — ASP.NET Core minimal
   APIs inferred the service as a Body parameter. Symptom:
   `Body was inferred but the method does not allow inferred body
   parameters. Parameter: sessionContext (Body (Inferred))`. Fix: annotate
   every `IBffSessionContext sessionContext` parameter with
   `[FromServices]` in `BffPostReadEndpoints`, `BffPostWriteEndpoints`,
   `BffPostModerationEndpoints`. Without the attribute, the host fails
   to start at request time.
3. **AmbiguousMatchException** — `app.MapBffSearchAssistEndpoints()` was
   accidentally called twice in `Bff/Program.cs` during the endpoint
   split. Symptom: `The request matched multiple endpoints. Matches:
   HTTP: POST /api/search/assist => AssistAsync (twice)`. Fix: removed
   the duplicate call. The rate-limit tests that hit `/api/search/assist`
   were the only ones affected.
4. **`ResolvedBffSession.Unauthorized()` returned a null `Result`** — the
   original `ResolvedBffIdentity.Unauthorized()` returned
   `Results.Unauthorized()` as the `Result`, so the caller could check
   `if (session.Result is not null) return session.Result`. My initial
   extraction returned `null` in the result slot for the unauthorized
   case, which made the unauthenticated branch fall through. Symptom:
   `FollowingFeed_WithoutAuthenticatedSession_ReturnsUnauthorizedWithoutCallingServices`
   got 200 instead of 401. Fix: `Unauthorized() => new(Guid.Empty, [],
   Results.Unauthorized());`.

These regressions reinforce the rule in `docs/ai/engineering-rules.md`:
"make one focused change at a time, then verify". The BFF tests caught
each one within seconds, and the fix for each was localized.
