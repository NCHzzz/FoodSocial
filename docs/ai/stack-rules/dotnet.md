# dotnet.md

## Purpose

This file is the single backend engineering rule source for FoodSocial .NET services ( FoodSocial/Backend )

Use this file when creating, moving, reviewing, or refactoring backend code. Product behavior and business truth still live in `docs/project-discovery/`.

## Rule Levels

For rule-level definitions (MUST / SHOULD / MAY) and deviation documentation requirements, see `docs/ai/engineering-rules.md`.

## Backend Architecture Direction

FoodSocial backend services use Clean Architecture with explicit microservice boundaries and pragmatic service extraction.

- Backend services **MUST** target `.NET 10`.
- Services **MUST** treat PostgreSQL as their owned transactional store unless a documented exception exists.
- Aspire **SHOULD** remain the default local orchestration and composition model.
- Keycloak **MUST** remain the default authentication provider unless a documented exception exists.
- RabbitMQ **MUST** be treated as the default async backbone.
- Frontend-facing composition **MUST** stay at the API Gateway / BFF edge.
- Internal integration **MUST** assume hybrid REST plus async events.
- Runtime configuration **MUST** remain environment-driven.
- Secrets **MUST NOT** live in code or source control.
- Each deployable service **MUST** remain independently buildable, deployable, scalable, versionable, and rollbackable.

## Service Boundary Rules

- A service **MUST** own one primary business capability and one data ownership boundary.
- A service **MUST NOT** read or write another service's database directly.
- Service boundaries **MUST** align to business capabilities, not UI pages or generic technical layers.
- HTTP, messaging, persistence, and provider integration code **MUST** stay outside Domain and business-core logic.
- Infrastructure **MUST NOT** leak into Domain.
- Services **SHOULD** prefer clear vertical slices over abstraction-heavy internal layering.
- Shared packages **MUST NOT** become a backdoor for cross-service business logic.
- Dish and Place **MUST** remain explicit first-class owned boundaries as defined in `docs/project-discovery/`.
- Tenant-aware services **MUST** keep tenant isolation explicit in authorization and data access boundaries.

## DPUnity-Style Service Tree

Each bounded service should use this shape. Use only folders that are needed; do not create empty folders for aesthetics.

```text
<Service>/
├── Api/
│   ├── Authorization/
│   ├── Controllers/
│   ├── Contracts/                 # API-only request/response types when not shared
│   ├── DependencyInjection/
│   ├── Health/
│   ├── Media/                     # only when API owns media boundary workflows
│   ├── Observability/             # metrics/logging helpers for API boundary
│   ├── Startup/
│   ├── GlobalUsings.cs            # optional compatibility bridge
│   └── Program.cs
├── Application/
│   ├── DTOs/
│   ├── DependencyInjection/
│   ├── Exceptions/
│   ├── Interfaces/
│   │   └── Repositories/
│   ├── Mapping/
│   ├── Policies/
│   ├── UseCases/
│   │   └── <Capability>/
│   ├── Validation/
│   └── GlobalUsings.cs            # optional compatibility bridge
├── Domain/
│   ├── Entities or records
│   ├── Value objects
│   ├── Enums
│   ├── Exceptions
│   ├── Constants
│   └── Domain rules
└── Infrastructure/
    ├── Clients/
    ├── DependencyInjection/
    ├── Messaging/
    ├── Persistence/
    │   ├── Configurations/
    │   ├── Migrations/
    │   └── Repositories or stores
    └── GlobalUsings.cs            # optional compatibility bridge
```

### DPUnity Alignment Notes

- DPUnity modules split by `Core`, `Application`, `Infrastructure`, and `API`. FoodSocial keeps `Domain` as the equivalent of DPUnity `Core`.
- DPUnity application code groups orchestration under `UseCases/<Capability>/`; FoodSocial should do the same for application workflows.
- DPUnity application interfaces group repositories under `Interfaces/Repositories`; FoodSocial repository/store interfaces should follow that location.
- DPUnity dependency registration lives under `DependencyInjection/ServiceCollectionExtensions.cs`; FoodSocial should follow that naming and placement.
- DPUnity infrastructure groups concrete adapters under persistence, messaging, clients, and provider folders; FoodSocial should avoid infrastructure files in the project root.

## BFF and Gateway Layering Exception

The four-layer service tree described in "DPUnity-Style Service Tree" applies
to **domain services** (Identity, UserProfile, Dish, Place, Post) and any
future service that owns business invariants.

`FoodSocial.Bff` and `FoodSocial.ApiGateway` are **not** domain services and
**MUST NOT** use the four-layer shape:

- `FoodSocial.Bff` **MUST** be organized by bounded context (vertical slice)
  because it owns no domain invariants. See
  `docs/project-discovery/12_backend_structure.md` for the approved BFF
  folder shape.
- `FoodSocial.ApiGateway` **MUST** stay a flat collection of middleware
  and configuration helpers. It owns no business logic and has no layering
  need.

Both the BFF and the API Gateway still **MUST** follow every other rule in
this file: `ProblemDetails` errors, async end-to-end I/O, environment-driven
configuration, no secrets in code, explicit input validation, rate limiting,
observability, security headers, and the testing rules.
## Layer Responsibilities

### API Layer

API owns the transport boundary.

- API **MUST** own controllers, HTTP/auth filters, API-only workflows, health checks, OpenAPI setup, middleware pipeline, and startup composition.
- API **MUST NOT** contain business rules that belong in Application.
- API **MUST NOT** directly encode persistence decisions.
- API **MUST** use explicit request and response models for public APIs.
- API **MUST NOT** return EF entities or raw persistence models.
- External input **MUST** be validated at the boundary.
- API errors **MUST** return standardized `ProblemDetails`.
- Authentication **MUST** remain distinct from application authorization.
- Authorization **SHOULD** be explicit and policy-based where possible.
- Public APIs **SHOULD** be stable and resource-oriented.
- Breaking API changes **MUST** be intentional and versioned.
- API versioning **SHOULD** use URL path segments (for example `/api/v1/dishes`) as the default strategy unless a documented exception exists.
- List and feed endpoints **MUST** use an intentional result-bounding strategy. Prefer cursor-based pagination for feed-like surfaces and offset-based pagination for admin-like lists. Default page size **SHOULD** be explicit in code and bounded by a maximum.
- `Program.cs` **MUST** stay thin: build app, call service registration, apply startup tasks, configure pipeline, run.

### Application Layer

Application owns business orchestration.

- Application **MUST** own use cases, application DTOs/results, policies, validation, repository interfaces, and service orchestration.
- Application **MUST NOT** depend on Infrastructure.
- Application **SHOULD** expose explicit result types rather than leaking persistence entities.
- Use cases **SHOULD** live under `Application/UseCases/<Capability>/`.
- Repository/store interfaces **SHOULD** live under `Application/Interfaces/Repositories/`.
- Expected business outcomes **SHOULD** use explicit result paths instead of exception-driven branching.
- Cancellation tokens **SHOULD** pass through meaningful async boundaries.
- Application services **MUST** keep authorization and ownership decisions explicit.

### Domain Layer

Domain owns business invariants.

- Domain **MUST** own invariants, state transitions, normalized values, lifecycle rules, and domain records/entities.
- Domain **MUST NOT** reference HTTP, EF Core, RabbitMQ, configuration, logging, or external providers.
- Domain **MUST NOT** publish external integration events directly; map domain meaning to integration contracts outside Domain.
- Infrastructure **MUST NOT** leak into Domain.

### Infrastructure Layer

Infrastructure owns implementation details.

- Infrastructure **MUST** implement Application interfaces.
- Infrastructure **MUST** own EF Core contexts, migrations, repositories/stores, HTTP clients, messaging publishers/consumers, outbox workers, and provider adapters.
- Infrastructure **MUST NOT** leak provider-specific types into Domain.
- Infrastructure **SHOULD** keep persistence under `Infrastructure/Persistence`, messaging under `Infrastructure/Messaging`, and external HTTP/provider clients under `Infrastructure/Clients`.

## Dependency Rules

Allowed dependency direction:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application -> Domain
Infrastructure -> Application -> Domain
```

- API **MAY** reference Application and Infrastructure for composition.
- Infrastructure **MAY** reference Application and Domain.
- Application **MAY** reference Domain and shared contracts.
- Domain **MUST NOT** reference any outer layer.
- Shared contracts **MUST NOT** contain service-owned business logic.

## Naming Rules

- C# files **MUST** use PascalCase and match the primary type they contain.
- Public types **MUST** use PascalCase.
- Interfaces **MUST** use the `I` prefix only for true interfaces.
- Names **MUST** communicate purpose, not implementation accident.
- Request/response and DTO types **MUST** be explicit about their transport or contract role.
- Command, query, handler, validator, mapper, client, repository, policy, and use-case names **SHOULD** reflect their actual role.
- Async methods **SHOULD** use the `Async` suffix unless framework-required exceptions apply.
- Dependency registration files **SHOULD** be named `ServiceCollectionExtensions.cs` and live under `DependencyInjection/`.
- Use cases **SHOULD** be named by capability and action, for example `PostPublishingUseCase`, `UserSavedItemsService`, or `EnsureIdentityCommandHandler`.
- Avoid vague folders or type names such as `Helpers`, `Common`, `Utils`, `Manager`, or `Misc` unless no narrower name fits.

## Data and Persistence Rules

- Writes **MUST** stay inside the owning service boundary.
- Each service **MUST** own its EF Core migrations.
- Read APIs **MUST** use explicit projections rather than persistence shapes.
- `AsNoTracking` **SHOULD** be used for read-only queries unless tracking is required.
- Lazy loading **MUST NOT** be used.
- Hidden N+1 query behavior **MUST NOT** be introduced.
- Efficient projections **SHOULD** be preferred over loading full graphs by default.
- Indexes **SHOULD** match real query patterns.
- Transaction boundaries **MUST** be explicit and local to owning service.
- Distributed transactions across services **MUST NOT** be used.
- Deletion and retention behavior **MUST** be intentional.
- Binary media lifecycle **SHOULD** remain explicit: metadata ownership, derivative assets, cleanup signaling, and storage deletion behavior.
- A canonical `DefaultTenantId` literal **MUST** live in `FoodSocial.Contracts.Common.TenantDefaults.Id` and be consumed by service-owned infrastructure stores. Per-entity `DefaultTenantId` constants are forbidden as they cause drift; a Domain layer **MUST NOT** reference the Contracts layer.

- Binary media lifecycle **SHOULD** remain explicit: metadata ownership, derivative assets, cleanup signaling, and storage deletion behavior.

## Async, Messaging, and Reliability Rules

- All I/O-bound paths **MUST** be async end-to-end.
- `.Result`, `.Wait()`, or sync-over-async patterns **MUST NOT** be used.
- Cross-service workflows **SHOULD** prefer async integration when eventual consistency is acceptable.
- Event consumers **MUST** be safe under duplicate delivery.
- Event contract evolution **SHOULD** be additive.
- Important integration events **SHOULD** carry stable metadata: event id, type, version, occurred-at timestamp, and correlation id.
- Timeout, retry, and fallback behavior **MUST** be explicit at integration boundaries.
- Non-idempotent operations **MUST NOT** be retried blindly.
- User-visible or trust-sensitive publish flows **SHOULD** use a reliability pattern that avoids silent write-then-publish loss.
- Important consumers **SHOULD** define replay, dead-letter, or poison-message handling.
- Integration contracts **MUST NOT** expose private persistence assumptions.

## Error Handling Rules

- Exceptions **SHOULD** represent unexpected failures, not normal control flow.
- Unhandled exceptions **MUST** be translated once into standardized API failure responses such as `ProblemDetails`.
- Cancellation **SHOULD NOT** be treated as an application failure state.
- Exceptions **MUST NOT** be swallowed silently; log, translate, or rethrow intentionally.
- Error handling **MUST NOT** leak secrets, tokens, stack traces, or internal topology.
- Failure paths for authorization, tenant isolation, missing resources, and downstream dependency failure **SHOULD** remain explicit and testable.

## Observability and Operations Rules

- Services **MUST** emit structured logs, traces, and metrics.
- Correlation IDs **MUST** flow across HTTP and messaging boundaries.
- Health endpoints **MUST** be meaningful.
- Liveness and readiness **SHOULD** be separated where meaningful.
- Readiness **SHOULD** reflect whether service can safely serve traffic.
- Optional enrichment and AI-assisted behavior **SHOULD** stay off critical paths where possible.
- Downstream and AI-provider failure paths **MUST** degrade safely and predictably.
- Moderation-sensitive or security-relevant actions **SHOULD** produce useful audit logs.

## Security and Trust Rules

- Keycloak **MUST** be the default authentication provider unless a documented exception exists.
- Security-sensitive configuration **MUST** remain environment-driven.
- Secrets, tokens, internal topology, and stack traces **MUST NOT** leak in API responses.
- External input **MUST** be validated and sanitized at the boundary.
- Database, broker, and service credentials **SHOULD** follow least-privilege principles.
- Service-to-service and admin operations **MUST** have explicit trust and authorization boundaries.
- Public-facing APIs **SHOULD** define abuse-aware protections where relevant.
- Moderation-sensitive workflows **SHOULD** preserve auditability and evidence retention.
- Launch paths **MUST NOT** silently introduce future-phase moderation automation such as auto-hide behavior.

## Testing and Delivery Rules

- Domain rules **MUST** receive direct unit tests.
- Persistence, messaging, and provider adapters **SHOULD** have integration tests where behavior matters.
- Public APIs and important event contracts **SHOULD** have contract tests where drift risk is meaningful.
- Production bugs **MUST** receive regression tests.
- Authorization, bounded list behavior, and duplicate-delivery/idempotency behavior **SHOULD** be tested where applicable.
- Each deployable service **MUST** stay independently releasable.
- Database migrations **MUST** remain responsibility of owning service.
- When generating, applying, reviewing, or troubleshooting EF Core migrations, also load `Backend/MIGRATIONS.md`.

## Refactor Rules

- Preserve routes, DTO contracts, status codes, schema, configuration keys, and service boundaries unless task explicitly approves behavior change.
- Prefer moving code into right folder before adding abstractions.
- Do not add MediatR, generic repositories, unit-of-work, or framework-heavy patterns without concrete local need.
- Do not introduce abstractions before a second real use case exists.
- Global usings **MAY** be used as a compatibility bridge during large folder moves, but explicit imports are preferred for long-term clarity.
- Namespace changes **MUST** be deliberate and build-verified.
- Backend refactors **MUST** run diagnostics, build, and relevant tests before completion.

## Canonical Backend Patterns

The following patterns are the established implementation conventions. Deviate only with a documented exception.

### Typed wire constants for cross-service enums

When a service serializes a domain enum as a string in a wire DTO (e.g. `IdentityLifecycleState`, `IdentityAppRoles`, `DishLifecycleStatus`, `PlaceLifecycleStatus`), the corresponding string literals **MUST** be exposed as `public static class <Service>WireConstants` in `FoodSocial.Contracts` (e.g. `FoodSocial.Contracts.Identity.IdentityWireConstants`, `FoodSocial.Contracts.Dishes.DishWireConstants`, `FoodSocial.Contracts.Places.PlaceWireConstants`). Cross-service consumers (BFF, downstream HTTP clients) **MUST** compare against these constants, never against hard-coded string literals. Renames in the producer stay in sync with all consumers.

### BFF session facade and ProblemDetails catalog

The BFF **MUST** route all authenticated browser endpoints through `FoodSocial.Bff.Session.IBffSessionContext` rather than re-implementing identity-orchestration, lifecycle allow/deny, and moderator-role checks per endpoint file. The implementation **MUST** return a `ResolvedBffSession` envelope whose `Result` (an `IResult`) is non-null exactly when the request must short-circuit. The canonical ProblemDetails type URIs **MUST** live in `FoodSocial.Bff.Errors.BffProblemTypes` and the `IResult` builders in `FoodSocial.Bff.Errors.BffProblemResults`. Inline `Results.Problem(...)` or `Problem(...)` constructions with literal `https://foodsocial/...` URIs are forbidden.

### UseCases/<Capability>/ subfolders

Use case files **MUST** live under `Application/UseCases/<Capability>/`, where `<Capability>` is a noun that names the bounded slice the use case belongs to (e.g. `Identities/`, `Identities/Bootstrap/`, `Posts/Publishing/`, `Posts/Queries/`, `Posts/Engagement/`, `Posts/Comments/`, `Posts/Reposts/`, `Posts/Reports/`, `Posts/Notifications/`, `Posts/Media/`). The file namespace **MUST** match the folder (`FoodSocial.<Service>.Application.UseCases.<Capability>`). One class per file; the class name describes the use case verb, not the folder. Service facades that aggregate use cases may live in a sibling `<Resource>/` folder (see `PostService` in Post) but use cases themselves do not.

### BFF endpoint files split by resource group

BFF endpoint files larger than ~250 lines **MUST** be split by resource group (read / write / moderation / search) into separate `BffXxx<Group>Endpoints` files, each exposing its own `MapBffXxx<Group>Endpoints(IEndpointRouteBuilder)` extension. Each endpoint parameter that is a service (e.g. `IBffSessionContext`) **MUST** be marked `[FromServices]`; otherwise ASP.NET Core minimal APIs will infer it as a Body parameter and the host will fail to start.

### Background service cancellation and jitter

Long-running `BackgroundService` poll loops (e.g. outbox drainers) **MUST** catch `OperationCanceledException` only when `stoppingToken.IsCancellationRequested` is true. Polling delays **SHOULD** apply ±25% jitter to avoid thundering-herd when many service instances boot together.



## Verification Checklist

Before backend work is complete, verify:

- service boundaries remain correct
- no cross-service database access was introduced
- contracts/DTOs are explicit and inputs are validated
- Domain has no Infrastructure/API references
- `Program.cs` files are thin
- API root has no business logic files outside folders, except `Program.cs` and optional `GlobalUsings.cs`
- Application root has no service/model dumping ground, except optional `GlobalUsings.cs`
- repository interfaces live under `Application/Interfaces/Repositories/`
- infrastructure adapters live under `Infrastructure/Persistence`, `Infrastructure/Messaging`, or `Infrastructure/Clients`
- async and failure behavior are sensible for changed integrations
- observability exists for new critical paths
- diagnostics, build, and tests were actually run
