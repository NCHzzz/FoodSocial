# dotnet.md

## Purpose

This file defines backend implementation rules for .NET services in this repository.

Use it for code-generation and review decisions. Product and architecture truth still live in `docs/project-discovery/`.

## Rule Levels

- **MUST** = required default
- **SHOULD** = strong default; deviate intentionally

## Exception Handling Rule

- Deviations from a **MUST** or major **SHOULD** **SHOULD** be documented with:
  - the rule being deviated from
  - why the default does not fit
  - the risk introduced
  - how that risk is mitigated
  - who approved the exception when approval is required

## Repo Defaults

- Backend services **MUST** target `.NET 10`
- Shared libraries, tooling, benchmarks, and test projects **MAY** target another compatible framework when justified
- Services **MUST** treat PostgreSQL as the default owned transactional store unless a documented exception exists
- Frontend-facing composition **MUST** stay at the API Gateway / BFF edge
- Internal integration **MUST** assume Hybrid REST + async events
- RabbitMQ **MUST** be treated as the default async backbone
- Aspire **SHOULD** be the default local orchestration/composition model
- Services **SHOULD** use shared Service Defaults or an equivalent shared baseline for telemetry, health checks, resilience, service discovery, and environment-driven configuration
- Keycloak **MUST** be treated as the default authentication provider
- Internal service-to-service communication **MUST NOT** hardcode hostnames or ports when service discovery is available
- Runtime configuration **MUST** remain environment-driven
- Secrets **MUST NOT** live in code or source control
- Each deployable service **MUST** remain buildable, deployable, scalable, versionable, and rollbackable independently

## Service Boundaries and Architecture

- A service **MUST** own one primary business capability and one data ownership boundary
- A service **MUST NOT** read or write another service's database directly
- Service boundaries **MUST** align to business capabilities, not UI pages or technical layers
- HTTP, messaging, persistence, and provider integration code **MUST** stay outside Domain and outside business-core logic
- Infrastructure **MUST NOT** leak into Domain
- Services **SHOULD** prefer clear vertical slices over abstraction-heavy internal layering
- Shared packages **MUST NOT** become a backdoor for cross-service business logic
- Dish and Place **MUST** remain explicit first-class owned boundaries
- Tenant-aware services **MUST** keep tenant isolation explicit in authorization and data access boundaries

## Naming Rules

Follow universal naming semantics in `docs/ai/engineering-rules.md`.

This section adds only .NET-specific naming mechanics, type-role conventions, and boundary-sensitive backend naming.

### File and type naming

- C# file names **MUST** use PascalCase and match the primary type they contain.
- Public types **MUST** use PascalCase.
- Interface names **MUST** use the `I` prefix only for true interfaces.

### Type-role naming

- Request/response and DTO types **MUST** be explicit about their transport or contract role.
- Command, query, handler, validator, mapper, client, repository, and policy names **SHOULD** reflect their actual role directly.

### Member naming

- Async methods **SHOULD** use the `Async` suffix unless they are event handlers or framework-required exceptions.

### Boundary naming rules

- Names **MUST** distinguish clearly between domain concepts and infrastructure concepts.

Examples that belong in this layer:

- `GetProfileQuery`, `GetProfileQueryHandler`
- `UpdateDishCommand`, `UpdateDishCommandHandler`
- `UserProfileDto`, `CreatePostRequest`, `CreatePostResponse`
- `LoadSessionAsync`, `PublishIntegrationEventAsync`

## API and Contract Rules

- Public APIs **MUST** use explicit request and response models
- Public APIs **MUST NOT** return EF entities or raw persistence models
- External input **MUST** be validated at the boundary
- API errors **MUST** return standardized `ProblemDetails`
- `ProblemDetails` responses **SHOULD** include stable fields such as `type`, `title`, `status`, validation details, and `traceId` where relevant and safe
- Authentication **MUST** remain distinct from application authorization
- Authorization **SHOULD** be explicit and policy-based where possible
- Endpoints **SHOULD** stay organized by bounded capability
- Public APIs **SHOULD** be stable and resource-oriented
- Breaking API changes **MUST** be intentional and versioned
- List and feed endpoints **MUST** use an intentional result-bounding strategy
- Idempotency **SHOULD** be supported when the business operation allows it
- Idempotency keys **SHOULD** be considered for operations likely to be retried by clients or gateways
- Gateway/BFF composition **SHOULD** be preferred when it reduces client chatter or cross-service leakage

## Exception and Failure-Handling Rules

- Exceptions **SHOULD** represent unexpected failures, not normal control flow
- Expected business outcomes **SHOULD** use explicit result paths or application-level responses instead of exception-driven branching
- Unhandled exceptions **MUST** be translated once into standardized API failure responses such as `ProblemDetails`
- Cancellation **SHOULD NOT** be treated as an application failure state
- Exceptions **MUST NOT** be swallowed silently; log, translate, or rethrow intentionally
- Error handling **MUST NOT** leak secrets, tokens, stack traces, or internal topology
- Backend failure paths for authorization, tenant isolation, missing resources, and downstream dependency failure **SHOULD** remain explicit and testable

## Data and Persistence Rules

- Writes **MUST** stay inside the owning service boundary
- Read APIs **MUST** use explicit projections rather than persistence shapes
- `AsNoTracking` **SHOULD** be used for read-only queries unless tracking is required
- Lazy loading **MUST NOT** be used
- Hidden N+1 query behavior **MUST NOT** be introduced
- Efficient projections **SHOULD** be preferred over loading full graphs by default
- Indexes **SHOULD** match real query patterns
- Query performance **SHOULD** be checked with PostgreSQL plans when important paths justify it
- Read/search models **SHOULD** be treated as rebuildable when source-of-truth ownership remains explicit
- Read APIs **SHOULD NOT** be tightly coupled to persistence schema shape
- Transaction boundaries **MUST** be explicit and local to the owning service
- Distributed transactions across services **MUST NOT** be used
- When database writes and message publication must succeed together, an outbox-style pattern **SHOULD** be preferred
- Concurrency-sensitive write paths **SHOULD** use explicit concurrency controls where appropriate
- Deletion and retention behavior **MUST** be intentional, not accidental side effects
- Where the domain requires it, deletion semantics **SHOULD** distinguish soft-delete, hard-delete, and cleanup behavior explicitly
- Binary media lifecycle **SHOULD** remain explicit: metadata ownership, derivative assets, cleanup signaling, and storage deletion behavior must not be left implicit

## Async, Messaging, and Reliability Rules

- All I/O-bound paths **MUST** be async end-to-end
- `.Result`, `.Wait()`, or equivalent sync-over-async patterns **MUST NOT** be used
- Cancellation tokens **SHOULD** pass through meaningful async boundaries
- Synchronous calls **SHOULD** be used only when an immediate response or immediate consistency is truly required
- Cross-service workflows **SHOULD** prefer async integration when eventual consistency is acceptable
- Event consumers **MUST** be safe under duplicate delivery
- Event contract evolution **SHOULD** be additive
- Domain events **MUST NOT** be published externally without explicit mapping to integration events
- Important integration events **SHOULD** carry stable metadata such as event id, type, version, occurred-at timestamp, and correlation id
- Timeout, retry, and fallback behavior **MUST** be explicit at integration boundaries
- Non-idempotent operations **MUST NOT** be retried blindly
- User-visible or trust-sensitive publish flows **SHOULD** use a reliability pattern that avoids silent write-then-publish loss
- Important consumers **SHOULD** define replay, dead-letter, or poison-message handling
- Consumers **SHOULD** tolerate realistic out-of-order delivery where possible
- Integration contracts **MUST NOT** expose private persistence assumptions

## Time, Background Work, and Operational Defaults

- Persist timestamps in UTC unless the domain explicitly requires otherwise
- Convert time-zone-specific presentation at system boundaries
- Background jobs, media processing, enrichment, and projection refresh **SHOULD** stay off the critical request path when possible
- Consistency boundaries **SHOULD** be visible in code and contracts
- Compensating actions **SHOULD** be preferred over cross-service transactional coupling
- Public/business-facing contracts **SHOULD** stay explicit in .NET services even when model-heavy AI execution lives behind internal providers or Python services

## Observability and Failure Behavior

- Services **MUST** emit structured logs, traces, and metrics
- Correlation IDs **MUST** flow across HTTP and messaging boundaries
- Health endpoints **MUST** be meaningful
- Liveness and readiness **SHOULD** be separated where meaningful
- Readiness **SHOULD** reflect whether the service can safely serve traffic
- Reliability choices **SHOULD** be justified with traces, metrics, logs, or query diagnostics rather than guesswork
- Optional enrichment and AI-assisted behavior **SHOULD** stay off the critical path where possible
- Downstream and AI-provider failure paths **MUST** degrade safely and predictably
- Moderation-sensitive or security-relevant actions **SHOULD** produce useful audit logs

## Security and Trust Rules

- Keycloak **MUST** be the default authentication provider unless a documented exception exists
- Application services **MUST** keep authorization explicit
- Secrets, tokens, internal topology, and stack traces **MUST NOT** leak in API responses
- Security-sensitive configuration **MUST** remain environment-driven
- External input **MUST** be validated and sanitized at the boundary
- Database, broker, and service credentials **SHOULD** follow least-privilege principles
- Service-to-service and admin operations **MUST** have explicit trust/authorization boundaries
- Public-facing APIs **SHOULD** define abuse-aware protections such as rate limiting, bot friction, or equivalent controls where relevant
- Moderation-sensitive workflows **SHOULD** preserve auditability and evidence retention, and **MUST NOT** silently introduce future-phase automation such as auto-hide behavior into MVP paths

## Performance Rules

- Measure before optimizing
- Traces, metrics, and query plans **SHOULD** justify performance changes
- Whole result sets **MUST NOT** be loaded when a bounded page or projection is enough
- Expensive enrichment, optional integrations, and unnecessary mapping/network hops **SHOULD** stay off hot paths
- Services **SHOULD** degrade gracefully when downstream systems, brokers, or AI providers fail

## Testing and Delivery Rules

- Domain rules **MUST** receive direct unit tests
- Persistence, messaging, and provider adapters **SHOULD** have integration tests where behavior matters
- Public APIs and important event contracts **SHOULD** have contract tests where drift risk is meaningful
- Production bugs **MUST** receive regression tests
- Authorization, bounded list behavior, and duplicate-delivery/idempotency behavior **SHOULD** be tested where applicable
- Each deployable service **MUST** stay independently releasable
- Database migrations **MUST** remain the responsibility of the owning service
- Critical data paths **SHOULD** have documented recovery expectations, and restore confidence matters more than backup existence alone

## AI-Assisted Engineering Rules

- AI-generated backend code **MUST** follow the same service-boundary, contract, async, persistence, observability, and security rules as human-written code
- AI-generated backend code **MUST NOT** introduce cross-service database access, transport leakage into Domain, or abstraction-heavy framework code without clear local value
- AI-generated backend code **SHOULD** prefer the simplest correct pipeline, middleware, handler, or service abstraction that fits the current codebase
- AI-generated backend code **MUST NOT** invent reusable abstractions before a second real use case exists
- AI-assisted or provider-dependent backend behavior **MUST** degrade to deterministic fallback where the user flow or critical service behavior must continue
- AI-generated changes **SHOULD** be reviewed specifically for boundary violations, persistence leakage, missing ProblemDetails handling, missing cancellation propagation, missing idempotency handling, weak authorization, and secret exposure

## Agent Completion Checks

Before considering backend work complete, verify:

- service boundaries are still correct
- no cross-service DB access was introduced
- contracts/DTOs are explicit and inputs are validated
- async and failure behavior are sensible for changed integrations
- observability exists for new critical paths
- diagnostics/tests/verification were actually run
- `docs/context/` was updated if current state materially changed
