# Identity Service Complete

## TL;DR
> **Summary**: Deliver a production-oriented Identity Service slice on top of the current Keycloak + BFF + Identity API foundation by adding synchronous local identity ensuring, hybrid downstream bootstrap via reliable async events, account-level RBAC, refresh/session lifecycle support, service-to-service trust rules, and TDD-backed verification.
> **Deliverables**:
> - Complete Identity API business slice with account lifecycle, ensure/provision flow, RBAC, and bootstrap status
> - BFF orchestration updates for auth/session/refresh and identity bootstrap handoff
> - Reliable hybrid provisioning with outbox + RabbitMQ + idempotent UserProfile bootstrap consumer
> - First real backend test projects with unit, API integration, and AppHost-backed flow coverage
> - Operator-grade verification evidence and runbook-ready failure checks
> **Effort**: XL
> **Parallel**: YES - 3 waves
> **Critical Path**: 1 → 2 → 4 → 6 → 9

## Audit Update — 2026-04-10

- **Status**: Completed after audit remediation.
- **What was finished during this audit**:
  - updated stale Identity application/API tests to match the logger-aware `EnsureIdentityCommandHandler` constructor
  - aligned the UserProfile → Identity bootstrap-completion routing key to `identity.bootstrap.completed.v2` in both runtime and AppHost test helpers
  - switched AppHost-backed verification to session-scoped infrastructure resources and hardened fresh-environment startup by ensuring service-owned databases exist before migrations run
  - made the `20260410100000_AddIdentityUserState` migration idempotent so fresh AppHost environments do not fail on duplicate `State` column creation
  - aligned `Identity.Api` to prefer the HTTPS-discovered Keycloak authority in local Aspire environments so bearer-token validation matches the actual token issuer path
  - tightened the AppHost test fixture so schema-dependent tests wait for the Identity/UserProfile tables they actually require before running
- **Verified commands**:
  - `dotnet build Backend/FoodSocial.slnx`
  - `dotnet test Backend/tests/Identity/Domain.Tests/FoodSocial.Identity.Domain.Tests.csproj`
  - `dotnet test Backend/tests/Identity/Application.Tests/FoodSocial.Identity.Application.Tests.csproj`
  - `dotnet test Backend/tests/Identity/Api.Tests/FoodSocial.Identity.Api.Tests.csproj`
  - `dotnet test Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj`
- **Plan maintenance note**: some lower task references in this file are historical planning breadcrumbs from before the repo evolved. The audit status and verified commands above are the current source of truth for completion.

## Context
### Original Request
- Build a complete, polished Identity Service.

### Interview Summary
- Provisioning model: **Hybrid**.
- RBAC: **included**.
- Test strategy: **TDD**.
- Identity owns authentication and account-level authorization primitives.
- UserProfile owns profile data.

### Metis Review (gaps addressed)
- Locked the ownership boundary so Identity does not absorb profile-domain ownership.
- Chose hybrid provisioning to keep login deterministic while allowing downstream bootstrap async.
- Elevated outbox, idempotency, retry, dead-letter, and replay from “nice to have” to mandatory work items.
- Added explicit failure behavior, bootstrap state handling, and executable acceptance criteria requirements.

## Work Objectives
### Core Objective
Implement the first production-suitable Identity business slice for FoodSocial that preserves the approved architecture: Keycloak authenticates users, BFF stays the browser auth edge, Identity owns local identity mapping plus account-level RBAC primitives, and downstream profile bootstrap is triggered asynchronously with durable delivery.

### Deliverables
- Identity domain/application/infrastructure/API implementation for local identity ensuring and lifecycle state.
- BFF-to-Identity orchestration for session bootstrap and refresh-aware auth flow.
- Canonical RBAC model for account/global roles, role claim propagation rules, and service authorization usage.
- Durable outbox-backed integration event publication from Identity.
- Idempotent UserProfile bootstrap consumer and minimal profile-shell creation.
- Backend test projects and automated verification coverage for happy path, duplicate path, invalid auth, and downstream failure cases.

### Definition of Done (verifiable conditions with commands)
- `dotnet build Backend/FoodSocial.slnx` succeeds.
- `dotnet test Backend/tests/FoodSocial.Identity.Domain.Tests/FoodSocial.Identity.Domain.Tests.csproj` succeeds.
- `dotnet test Backend/tests/FoodSocial.Identity.Application.Tests/FoodSocial.Identity.Application.Tests.csproj` succeeds.
- `dotnet test Backend/tests/FoodSocial.Identity.Api.Tests/FoodSocial.Identity.Api.Tests.csproj` succeeds.
- `dotnet test Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj` succeeds.
- Running the AppHost and scripted auth flow proves: first ensure creates exactly one local identity, repeat ensure is idempotent, refresh path works as designed, RBAC-protected endpoints enforce policy, outbox rows are written then drained, and UserProfile bootstrap occurs once.

### Must Have
- BFF remains the frontend auth edge.
- Identity remains the owner of local identity mapping and account-level authorization primitives.
- UserProfile remains owner of profile data.
- Hybrid REST + async events remains the integration model.
- RabbitMQ-based async flow uses reliability patterns, not best-effort publishing.
- Event handlers are idempotent and replay-safe.
- TDD drives implementation order.

### Must NOT Have (guardrails, AI slop patterns, scope boundaries)
- Do not turn login into multi-service synchronous orchestration.
- Do not make Identity the owner of display profile, follow graph, or taste preferences.
- Do not rely on Keycloak realm roles alone as the business authorization source.
- Do not add broad admin console UX or full platform-wide policy engine design.
- Do not ship async provisioning without outbox, correlation IDs, retry, and dead-letter expectations.

## Verification Strategy
> ZERO HUMAN INTERVENTION - all verification is agent-executed.
- Test decision: **TDD** with xUnit-based backend test projects and AppHost-backed integration coverage.
- QA policy: Every task includes agent-executed scenarios.
- Evidence: `.sisyphus/evidence/task-{N}-{slug}.{ext}`

## Execution Strategy
### Parallel Execution Waves
> Target: 5-8 tasks per wave. <3 per wave (except final) = under-splitting.
> Extract shared dependencies as Wave-1 tasks for max parallelism.

Wave 1: domain/contracts/test foundation (Tasks 1-3)
Wave 2: auth orchestration + RBAC + reliability implementation (Tasks 4-7)
Wave 3: downstream bootstrap + operational verification + repo context alignment (Tasks 8-10)

### Dependency Matrix (full, all tasks)
- 1 blocks 2, 4, 5, 6
- 2 blocks 4, 6, 8
- 3 blocks 4, 5, 6, 8, 9
- 4 blocks 5, 6, 9
- 5 blocks 9
- 6 blocks 8, 9
- 7 blocks 9
- 8 blocks 9
- 9 blocks 10
- 10 blocks Final Verification Wave

### Agent Dispatch Summary (wave → task count → categories)
- Wave 1 → 3 tasks → quick / unspecified-high
- Wave 2 → 4 tasks → unspecified-high / deep
- Wave 3 → 3 tasks → unspecified-high / writing

## TODOs
> Implementation + Test = ONE task. Never separate.
> EVERY task MUST have: Agent Profile + Parallelization + QA Scenarios.

- [x] 1. Establish Identity ownership model, lifecycle state machine, and test harness baseline

  **What to do**: Create the first backend test projects for Identity and AppHost flow coverage using repo-aligned .NET test patterns. Define the canonical identity lifecycle model inside the Identity boundary: at minimum `PendingBootstrap`, `Active`, `Suspended`, and `Deleted` (or equivalent final names chosen by implementer strictly from this plan), with explicit rules for which states permit login, refresh, role resolution, and downstream bootstrap publication. Add the domain/application scaffolding needed so later tasks do not invent lifecycle semantics ad hoc. Keep UserProfile ownership out of this task except as a referenced downstream dependency.
  **Must NOT do**: Do not implement cross-service provisioning yet. Do not add profile-domain fields to `IdentityUser`. Do not leave lifecycle names or behavior to later judgment.

  **Recommended Agent Profile**:
  - Category: `unspecified-high` - Reason: multi-project backend foundation with domain and test setup.
  - Skills: [`dotnet-best-practices`, `test-driven-development`] - enforce .NET patterns and TDD-first sequencing.
  - Omitted: [`ef-core`] - not yet persistence-heavy; domain/test harness foundation comes first.

  **Parallelization**: Can Parallel: NO | Wave 1 | Blocks: [2, 4, 5, 6] | Blocked By: []

  **References** (executor has NO interview context - be exhaustive):
  - Pattern: `Backend/src/Identity/Domain/IdentityUser.cs:3-34` - current identity aggregate starting point.
  - Pattern: `Backend/src/Identity/Infrastructure/Persistence/IdentityDbContext.cs:14-35` - existing persistence boundary and uniqueness rule.
  - Pattern: `Backend/src/FoodSocial.AppHost/AppHost.cs:35-49` - current orchestration wiring between identity and userprofile services.
  - Pattern: `docs/project-discovery/10_architecture.md:74-83` - service responsibility matrix.
  - Pattern: `docs/project-discovery/12_backend_structure.md:63-65` - identity vs user/profile ownership boundary.
  - External: `https://xunit.net/` - xUnit reference for project structure and assertions.

  **Acceptance Criteria** (agent-executable only):
  - [ ] `dotnet build Backend/FoodSocial.slnx` succeeds after adding the new test projects.
  - [ ] `dotnet test Backend/tests/FoodSocial.Identity.Domain.Tests/FoodSocial.Identity.Domain.Tests.csproj` succeeds.
  - [ ] `dotnet test Backend/tests/FoodSocial.Identity.Application.Tests/FoodSocial.Identity.Application.Tests.csproj` succeeds.
  - [ ] Domain tests prove allowed/disallowed transitions for all lifecycle states and fail if an undefined state is used.

  **QA Scenarios** (MANDATORY - task incomplete without these):
  ```
  Scenario: Lifecycle transitions are enforced
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.Identity.Domain.Tests/FoodSocial.Identity.Domain.Tests.csproj" --filter "FullyQualifiedName~Lifecycle" --logger "trx;LogFileName=task-1-lifecycle.trx"`
    Expected: Test run passes; evidence shows transitions such as PendingBootstrap->Active allowed and Deleted->Active rejected per plan.
    Evidence: .sisyphus/evidence/task-1-identity-lifecycle.trx

  Scenario: Invalid lifecycle usage is rejected
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.Identity.Domain.Tests/FoodSocial.Identity.Domain.Tests.csproj" --filter "FullyQualifiedName~Reject" --logger "trx;LogFileName=task-1-lifecycle-reject.trx"`
    Expected: Test run passes only because invalid state transition attempts are rejected by the domain model.
    Evidence: .sisyphus/evidence/task-1-identity-lifecycle-error.trx
  ```

  **Commit**: YES | Message: `test(identity): add lifecycle foundation coverage` | Files: [`Backend/tests/**`, `Backend/src/Identity/Domain/**`, `Backend/src/Identity/Application/**`]

- [x] 2. Replace caller-supplied ensure contracts with claim-driven Identity ensure flow

  **What to do**: Redesign the local ensure/provision contract so the Identity API infers subject and allowed claim fields from authenticated bearer claims rather than trusting arbitrary caller-supplied subject values. Update or replace `EnsureIdentityRequest/Response` and related application command/handler/controller design so `sub` is authoritative from JWT claims, `preferred_username` and `email` are treated as Keycloak-sourced fields under explicit reconciliation rules, and the response includes enough information for BFF orchestration and downstream bootstrap status. Preserve backwards-safe contract naming only if it still reflects the secured semantics.
  **Must NOT do**: Do not keep a public/internal API that accepts arbitrary subject input from callers. Do not defer reconciliation rules. Do not encode profile ownership into the response.

  **Recommended Agent Profile**:
  - Category: `unspecified-high` - Reason: contract refactor touching API, application, and shared DTOs.
  - Skills: [`dotnet-best-practices`, `test-driven-development`] - contract safety and failing tests first.
  - Omitted: [`clean-code`] - correctness and security semantics matter more than stylistic cleanup here.

  **Parallelization**: Can Parallel: NO | Wave 1 | Blocks: [4, 6, 8] | Blocked By: [1]

  **References** (executor has NO interview context - be exhaustive):
  - Pattern: `Backend/src/FoodSocial.Contracts/Identity/EnsureIdentityRequest.cs:3-6` - insecure starting point because subject is caller-supplied.
  - Pattern: `Backend/src/FoodSocial.Contracts/Identity/EnsureIdentityResponse.cs:3-6` - current minimal response shape.
  - Pattern: `Backend/src/FoodSocial.Contracts/Identity/IdentityClaimReader.cs:8-33` - approved claim extraction helper.
  - Pattern: `Backend/src/Identity/Api/Controllers/CurrentUserController.cs:13-35` - current authenticated-claims boundary and missing-subject handling.
  - Pattern: `docs/project-discovery/08_api_spec.md:253-257` - external identity provider with internal roles/permissions.
  - Pattern: `docs/project-discovery/05_non_functional_requirements.md:35-43` - validate and authorize cross-service operations.

  **Acceptance Criteria** (agent-executable only):
  - [ ] `dotnet test Backend/tests/FoodSocial.Identity.Api.Tests/FoodSocial.Identity.Api.Tests.csproj --filter "FullyQualifiedName~EnsureIdentity"` succeeds.
  - [ ] Contract tests prove the ensure endpoint rejects requests with missing `sub` claim by returning HTTP 401 with problem details.
  - [ ] Contract tests prove caller-provided subject/body tampering is impossible or ignored by design.

  **QA Scenarios** (MANDATORY - task incomplete without these):
  ```
  Scenario: Claim-driven ensure succeeds with valid token
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.Identity.Api.Tests/FoodSocial.Identity.Api.Tests.csproj" --filter "FullyQualifiedName~EnsureIdentity_ValidToken" --logger "trx;LogFileName=task-2-valid.trx"`
    Expected: Test passes and verifies the endpoint builds its ensure command from bearer claims rather than caller input.
    Evidence: .sisyphus/evidence/task-2-ensure-valid.trx

  Scenario: Missing subject claim is rejected
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.Identity.Api.Tests/FoodSocial.Identity.Api.Tests.csproj" --filter "FullyQualifiedName~EnsureIdentity_MissingSubject" --logger "trx;LogFileName=task-2-missing-sub.trx"`
    Expected: Test passes and asserts HTTP 401 with a concrete problem title/detail for missing subject claim.
    Evidence: .sisyphus/evidence/task-2-ensure-missing-sub.trx
  ```

  **Commit**: YES | Message: `feat(identity): secure ensure contract around claims` | Files: [`Backend/src/FoodSocial.Contracts/Identity/**`, `Backend/src/Identity/Api/**`, `Backend/src/Identity/Application/**`, `Backend/tests/**`]

- [x] 3. Add AppHost-backed integration test infrastructure and reusable auth test utilities

  **What to do**: Stand up FoodSocial-native integration test infrastructure inspired by the Aspire reference repo under `FoodSocialaspire-temp`, but only for the active FoodSocial backend. Create reusable test helpers for: AppHost startup, discovery of mapped ports/endpoints, token acquisition from Keycloak test client, authenticated HTTP clients for BFF and Identity API, and database verification helpers for the identity and userprofile PostgreSQL databases. This task must make later end-to-end tasks executable without bespoke shell scripting inside each test.
  **Must NOT do**: Do not treat `FoodSocialaspire-temp` as production code. Do not couple tests to volatile localhost ports without helper abstraction. Do not skip DB verification helpers.

  **Recommended Agent Profile**:
  - Category: `unspecified-high` - Reason: integration harness creation across Aspire, HTTP, and DB verification.
  - Skills: [`dotnet-best-practices`, `test-driven-development`] - infrastructure should be robust and reusable.
  - Omitted: [`playwright-best-practices`] - browser automation is not the primary verification path for this service slice.

  **Parallelization**: Can Parallel: YES | Wave 1 | Blocks: [4, 5, 6, 8, 9] | Blocked By: []

  **References** (executor has NO interview context - be exhaustive):
  - Pattern: `FoodSocialaspire-temp/tests/testproject/TestProject.AppHost/TestProject.AppHost.csproj` - reference structure for AppHost-based tests.
  - Pattern: `FoodSocialaspire-temp/tests/Aspire.Hosting.Azure.Tests/RoleAssignmentTests.cs` - representative xUnit test style from reference repo.
  - Pattern: `Backend/src/FoodSocial.AppHost/AppHost.cs:1-70` - live resource graph to boot in tests.
  - Pattern: `docs/delivery/plans/2026-04-07-local-auth-and-identity-bootstrap-runbook.md:32-205` - current scripted validation flow to convert into automation.
  - Pattern: `docs/context/current_build_status.md:85-96` - already-verified manual bootstrap behavior.
  - External: `https://learn.microsoft.com/dotnet/aspire/testing/overview` - Aspire testing patterns.

  **Acceptance Criteria** (agent-executable only):
  - [ ] `dotnet test Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj` succeeds.
  - [ ] Integration helpers can acquire a Keycloak access token for `demo-user` using the imported local realm.
  - [ ] Integration helpers can query DB state for `identity_users` and userprofile bootstrap records.

  **QA Scenarios** (MANDATORY - task incomplete without these):
  ```
  Scenario: AppHost test harness boots local dependencies
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj" --filter "FullyQualifiedName~AppHostBoot" --logger "trx;LogFileName=task-3-apphost.trx"`
    Expected: Test passes and proves AppHost test harness can start services and resolve endpoints/resources.
    Evidence: .sisyphus/evidence/task-3-apphost.trx

  Scenario: Token helper obtains authenticated bearer token
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj" --filter "FullyQualifiedName~TokenAcquisition" --logger "trx;LogFileName=task-3-token.trx"`
    Expected: Test passes and captures a valid access token for the imported `demo-user` without manual port hunting.
    Evidence: .sisyphus/evidence/task-3-token.trx
  ```

  **Commit**: YES | Message: `test(apphost): add integration harness for auth flows` | Files: [`Backend/tests/FoodSocial.AppHost.Tests/**`, `Backend/tests/Shared/**`]

- [x] 4. Implement synchronous local identity ensure endpoint and reconciliation rules

  **What to do**: Implement the Identity API/application/domain path that ensures a local identity record exists for the authenticated Keycloak subject during the synchronous auth path. The canonical behavior is: if subject does not exist, create a new identity in `PendingBootstrap`; if subject exists, return the existing identity; reconcile `preferred_username` and `email` from Keycloak claims according to explicit rules chosen here: update cached values on successful ensure when the incoming claim is non-empty and differs, but never null out existing stored values from missing claims unless the plan defines a stronger rule inside code comments/tests. Return a response indicating `IdentityId`, `Subject`, `Created`, current lifecycle state, and whether downstream bootstrap remains pending.
  **Must NOT do**: Do not call UserProfile synchronously. Do not block the request on RabbitMQ. Do not create duplicate identity rows under concurrency. Do not silently swallow unique constraint races.

  **Recommended Agent Profile**:
  - Category: `unspecified-high` - Reason: central business slice with concurrency, persistence, and API semantics.
  - Skills: [`dotnet-best-practices`, `ef-core`, `test-driven-development`] - persistence correctness and TDD are core here.
  - Omitted: [`security-review`] - security-specific review belongs after implementation, not during core build.

  **Parallelization**: Can Parallel: NO | Wave 2 | Blocks: [5, 6, 9] | Blocked By: [1, 2, 3]

  **References** (executor has NO interview context - be exhaustive):
  - Pattern: `Backend/src/Identity/Infrastructure/Persistence/IdentityDbContext.cs:16-35` - unique subject mapping constraint.
  - Pattern: `Backend/src/Identity/Domain/IdentityUser.cs:20-33` - existing create/update behavior to extend safely.
  - Pattern: `Backend/src/Identity/Api/Controllers/CurrentUserController.cs:17-35` - subject extraction and missing-claim semantics.
  - Pattern: `docs/project-discovery/12_backend_structure.md:133-142` - synchronous calls only when immediate consistency is required; async for background reactions.
  - Pattern: `docs/project-discovery/10_architecture.md:26-27` - idempotent consumers and retry/dead-letter expectations.
  - Pattern: `docs/project-discovery/05_non_functional_requirements.md:80-85` - explicit async failure handling and operator response.

  **Acceptance Criteria** (agent-executable only):
  - [ ] `dotnet test Backend/tests/FoodSocial.Identity.Application.Tests/FoodSocial.Identity.Application.Tests.csproj --filter "FullyQualifiedName~EnsureIdentity"` succeeds.
  - [ ] Integration tests prove first ensure creates exactly one row in `identity_users`.
  - [ ] Integration tests prove second ensure for the same `sub` returns `Created=false` and does not increase row count.
  - [ ] Integration tests prove concurrent duplicate ensure requests do not create duplicate rows and resolve to one identity record.

  **QA Scenarios** (MANDATORY - task incomplete without these):
  ```
  Scenario: First ensure creates one local identity
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.Identity.Api.Tests/FoodSocial.Identity.Api.Tests.csproj" --filter "FullyQualifiedName~EnsureIdentity_FirstLoginCreatesRow" --logger "trx;LogFileName=task-4-first-create.trx"`
    Expected: Test passes and asserts HTTP 200/201 per chosen contract, `Created=true`, and DB row count for the subject becomes exactly 1.
    Evidence: .sisyphus/evidence/task-4-first-create.trx

  Scenario: Concurrent duplicate ensure remains idempotent
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.Identity.Api.Tests/FoodSocial.Identity.Api.Tests.csproj" --filter "FullyQualifiedName~EnsureIdentity_ConcurrentSameSubject" --logger "trx;LogFileName=task-4-concurrent.trx"`
    Expected: Test passes and shows both requests resolve to the same `IdentityId` with DB row count still exactly 1.
    Evidence: .sisyphus/evidence/task-4-concurrent-error.trx
  ```

  **Commit**: YES | Message: `feat(identity): implement synchronous ensure flow` | Files: [`Backend/src/Identity/**`, `Backend/tests/**`]

- [x] 5. Implement account-level RBAC model, authorization policies, and service trust rules

  **What to do**: Add the app-owned RBAC model for global/account authorization primitives inside the Identity boundary. Define the initial role set explicitly in code and tests: at minimum `user`, `admin`, and `moderator` unless repo patterns force different canonical names; store role assignments in Identity-owned persistence; define how roles are surfaced to BFF and downstream services through claims and/or app lookups; and create explicit authorization policies for admin/moderation-capable endpoints. Also define the lightweight but explicit service-to-service trust model required by discovery docs: BFF and internal services may call secured Identity operations only through authenticated/authorized service or user context, never as implicit trusted callers.
  **Must NOT do**: Do not make Keycloak realm roles the only source of truth. Do not introduce resource-level authorization ownership into Identity. Do not leave service-to-service trust implicit.

  **Recommended Agent Profile**:
  - Category: `deep` - Reason: architecture-sensitive authz design with persistence and policy implications.
  - Skills: [`dotnet-best-practices`, `test-driven-development`] - policy clarity and tests first.
  - Omitted: [`ef-core`] - persistence is involved but this task is chiefly about authorization design and enforcement.

  **Parallelization**: Can Parallel: YES | Wave 2 | Blocks: [9] | Blocked By: [1, 3, 4]

  **References** (executor has NO interview context - be exhaustive):
  - Pattern: `Backend/src/FoodSocial.Contracts/Identity/IdentityClaimReader.cs:17-33` - current role extraction path.
  - Pattern: `Backend/src/FoodSocial.Bff/Program.cs:35-68` - OIDC and claim mapping at the BFF edge.
  - Pattern: `docs/project-discovery/12_backend_structure.md:63-64` - Identity owns account-level authorization primitives; UserProfile owns profile data.
  - Pattern: `docs/project-discovery/08_api_spec.md:253-257` - external authentication with internal roles and permissions.
  - Pattern: `docs/project-discovery/05_non_functional_requirements.md:35-43` - secure service-to-service communication and stricter admin authorization boundaries.
  - Pattern: `docs/project-discovery/05_non_functional_requirements.md:90-95` - audit and correlation expectations for sensitive actions.

  **Acceptance Criteria** (agent-executable only):
  - [ ] `dotnet test Backend/tests/FoodSocial.Identity.Api.Tests/FoodSocial.Identity.Api.Tests.csproj --filter "FullyQualifiedName~Authorization"` succeeds.
  - [ ] Tests prove a standard `user` cannot access admin/moderator-only endpoints.
  - [ ] Tests prove an `admin` or `moderator` role can access the correct protected routes per chosen policy.
  - [ ] Tests prove service/internal callers without explicit trust credentials are rejected.

  **QA Scenarios** (MANDATORY - task incomplete without these):
  ```
  Scenario: User role is denied admin capability
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.Identity.Api.Tests/FoodSocial.Identity.Api.Tests.csproj" --filter "FullyQualifiedName~Authorization_UserDeniedAdmin" --logger "trx;LogFileName=task-5-user-denied.trx"`
    Expected: Test passes and asserts HTTP 403 for a standard user token on an admin-only endpoint.
    Evidence: .sisyphus/evidence/task-5-user-denied.trx

  Scenario: Missing internal trust context is rejected
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.Identity.Api.Tests/FoodSocial.Identity.Api.Tests.csproj" --filter "FullyQualifiedName~Authorization_ServiceCallerRejected" --logger "trx;LogFileName=task-5-service-reject.trx"`
    Expected: Test passes and asserts unauthorized/forbidden behavior when an internal caller lacks the required explicit trust configuration.
    Evidence: .sisyphus/evidence/task-5-service-reject.trx
  ```

  **Commit**: YES | Message: `feat(identity): add account-level rbac policies` | Files: [`Backend/src/Identity/**`, `Backend/src/FoodSocial.Contracts/Identity/**`, `Backend/src/FoodSocial.Bff/**`, `Backend/tests/**`]

- [x] 6. Add durable outbox publication for identity bootstrap events

  **What to do**: Implement the reliable hybrid provisioning bridge inside Identity. When synchronous ensure creates a new local identity or detects bootstrap is still pending, write the canonical integration event into an Identity-owned outbox in the same transaction as the identity write. Add background publication logic that reads pending outbox messages, publishes to RabbitMQ, records correlation/causation identifiers, marks delivery success/failure state, and supports retry with dead-letter or poison-message handling rules. The canonical event contract should carry enough data to bootstrap UserProfile without further synchronous calls: `IdentityId`, `Subject`, authoritative current role set needed downstream, `PreferredUsername`, `Email`, lifecycle/bootstrap metadata, and versioning information.
  **Must NOT do**: Do not publish directly after DB write without outbox durability. Do not emit ambiguous event names. Do not make publication success a prerequisite for the synchronous login response.

  **Recommended Agent Profile**:
  - Category: `deep` - Reason: reliability-critical distributed workflow with persistence and messaging.
  - Skills: [`dotnet-best-practices`, `ef-core`, `test-driven-development`] - transactional correctness and failing tests matter.
  - Omitted: [`clean-code`] - correctness and durability outrank refactoring polish here.

  **Parallelization**: Can Parallel: NO | Wave 2 | Blocks: [8, 9] | Blocked By: [1, 2, 3, 4]

  **References** (executor has NO interview context - be exhaustive):
  - Pattern: `Backend/src/FoodSocial.Contracts/Identity/UserProvisionedIntegrationEvent.cs:3-8` - current starting event contract to evolve.
  - Pattern: `Backend/src/FoodSocial.AppHost/AppHost.cs:13-17` - RabbitMQ resource already wired.
  - Pattern: `docs/project-discovery/12_backend_structure.md:137-142` - mandatory event reliability expectations.
  - Pattern: `docs/project-discovery/10_architecture.md:18-27` - hybrid REST + async events plus retry/dead-letter expectations.
  - Pattern: `docs/project-discovery/05_non_functional_requirements.md:76-85` - critical async workflows require explicit retry and operator response.
  - Pattern: `docs/delivery/plans/2026-04-07-local-auth-and-identity-bootstrap-runbook.md:213-219` - outbox/messaging currently out of scope, now to be implemented.

  **Acceptance Criteria** (agent-executable only):
  - [ ] `dotnet test Backend/tests/FoodSocial.Identity.Application.Tests/FoodSocial.Identity.Application.Tests.csproj --filter "FullyQualifiedName~Outbox"` succeeds.
  - [ ] Integration tests prove ensure writes identity row and outbox row atomically.
  - [ ] Integration tests prove publisher drains pending outbox rows and records delivery status.
  - [ ] Integration tests prove a simulated publish failure leaves retryable outbox state rather than losing the event.

  **QA Scenarios** (MANDATORY - task incomplete without these):
  ```
  Scenario: Ensure writes durable outbox record
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.Identity.Application.Tests/FoodSocial.Identity.Application.Tests.csproj" --filter "FullyQualifiedName~Outbox_WritesWithIdentity" --logger "trx;LogFileName=task-6-outbox-write.trx"`
    Expected: Test passes and verifies identity persistence plus outbox insertion happen in the same transaction boundary.
    Evidence: .sisyphus/evidence/task-6-outbox-write.trx

  Scenario: Publish failure leaves retryable state
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.Identity.Application.Tests/FoodSocial.Identity.Application.Tests.csproj" --filter "FullyQualifiedName~Outbox_PublishFailureRetained" --logger "trx;LogFileName=task-6-publish-fail.trx"`
    Expected: Test passes and proves failed publication does not delete the outbox row and marks it for retry/dead-letter handling.
    Evidence: .sisyphus/evidence/task-6-publish-fail.trx
  ```

  **Commit**: YES | Message: `feat(identity): add outbox-backed provisioning events` | Files: [`Backend/src/Identity/**`, `Backend/src/FoodSocial.Contracts/Identity/**`, `Backend/tests/**`]

- [x] 7. Extend BFF auth edge with identity orchestration, refresh flow, and bootstrap-aware session semantics

  **What to do**: Update the BFF so its browser-facing session/auth endpoints orchestrate the Identity ensure flow without violating service boundaries. The synchronous path after successful login/session establishment should ensure local identity exists and return session payload enriched with identity/bootstrap state needed by the frontend or downstream BFF routing. Add the missing refresh/session lifecycle behavior referenced by discovery docs: define whether refresh is cookie/session renewal, token renewal via OIDC refresh token, or controlled re-auth fallback, then implement it consistently in BFF endpoints and tests. Expose bootstrap-pending state clearly so the app can authenticate even if downstream profile shell creation is not complete yet.
  **Must NOT do**: Do not let the frontend call Identity directly for composite auth flow. Do not block BFF responses on async profile/bootstrap completion. Do not invent a refresh model different from the implemented OIDC cookie/session setup.

  **Recommended Agent Profile**:
  - Category: `unspecified-high` - Reason: edge orchestration across auth/session semantics and downstream service call.
  - Skills: [`dotnet-best-practices`, `test-driven-development`] - auth edge behavior must be explicit and well-covered.
  - Omitted: [`playwright-best-practices`] - browser automation is optional; API/integration tests are primary.

  **Parallelization**: Can Parallel: YES | Wave 2 | Blocks: [8, 9] | Blocked By: [1, 2, 3, 4]

  **References** (executor has NO interview context - be exhaustive):
  - Pattern: `Backend/src/FoodSocial.Bff/Program.cs:24-135` - current BFF auth/session endpoints and claim mapping.
  - Pattern: `docs/project-discovery/08_api_spec.md:9-13` - auth endpoints include refresh.
  - Pattern: `docs/project-discovery/12_backend_structure.md:117-123` - BFF remains the frontend-facing auth-aware API layer.
  - Pattern: `docs/project-discovery/10_architecture.md:74-76` - BFF shapes frontend responses; Identity handles identity mapping.
  - Pattern: `docs/delivery/plans/2026-04-07-local-auth-and-identity-bootstrap-runbook.md:79-139` - current login/session/manual validation path.
  - Pattern: `docs/project-discovery/05_non_functional_requirements.md:80-84` - explicit fallback behavior when downstream services fail.

  **Acceptance Criteria** (agent-executable only):
  - [ ] `dotnet test Backend/tests/FoodSocial.Identity.Api.Tests/FoodSocial.Identity.Api.Tests.csproj --filter "FullyQualifiedName~Bff"` or equivalent BFF/API test suite succeeds.
  - [ ] Integration tests prove `/api/auth/session` returns authenticated user plus identity/bootstrap state after successful login orchestration.
  - [ ] Integration tests prove `/api/auth/refresh` behaves consistently with the chosen session model.
  - [ ] Integration tests prove BFF still returns a usable auth/session response when downstream UserProfile bootstrap is pending.

  **QA Scenarios** (MANDATORY - task incomplete without these):
  ```
  Scenario: BFF session ensures local identity and returns bootstrap state
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj" --filter "FullyQualifiedName~BffSession_EnsuresIdentity" --logger "trx;LogFileName=task-7-bff-session.trx"`
    Expected: Test passes and verifies authenticated BFF session response includes subject, roles, identity id, and bootstrap status fields per chosen contract.
    Evidence: .sisyphus/evidence/task-7-bff-session.trx

  Scenario: Refresh path behaves deterministically
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj" --filter "FullyQualifiedName~BffRefresh" --logger "trx;LogFileName=task-7-refresh.trx"`
    Expected: Test passes and proves refresh either renews session/tokens or returns a concrete re-auth requirement, exactly as implemented by the chosen model.
    Evidence: .sisyphus/evidence/task-7-refresh-error.trx
  ```

  **Commit**: YES | Message: `feat(bff): orchestrate identity ensure and refresh flow` | Files: [`Backend/src/FoodSocial.Bff/**`, `Backend/tests/**`]

- [x] 8. Implement idempotent UserProfile bootstrap consumer and bootstrap-status completion path

  **What to do**: Implement the downstream half of the hybrid provisioning model in UserProfile. Consume the canonical identity bootstrap event from RabbitMQ, create a minimal profile shell exactly once per `IdentityId`/`Subject`, and update Identity bootstrap completion state through the chosen bounded mechanism: either an explicit callback/API endpoint on Identity or an internal completion event consumed by Identity. The profile shell should contain only fields owned by UserProfile and sufficient for downstream profile existence; no display-enrichment or UX-polish scope creep. Consumer logic must be idempotent for duplicate delivery, safe under replay, and explicit about partial-failure recovery.
  **Must NOT do**: Do not move profile ownership into Identity. Do not create rich profile preferences here. Do not assume at-most-once delivery. Do not leave bootstrap completion semantics implicit.

  **Recommended Agent Profile**:
  - Category: `unspecified-high` - Reason: downstream consumer plus cross-service completion semantics.
  - Skills: [`dotnet-best-practices`, `ef-core`, `test-driven-development`] - consumer idempotency and persistence must be precise.
  - Omitted: [`clean-code`] - functional reliability and scope discipline are the priority.

  **Parallelization**: Can Parallel: YES | Wave 3 | Blocks: [9] | Blocked By: [2, 3, 6, 7]

  **References** (executor has NO interview context - be exhaustive):
  - Pattern: `Backend/src/UserProfile/Api/Program.cs:1-28` - scaffold-level starting point for UserProfile service.
  - Pattern: `Backend/src/FoodSocial.AppHost/AppHost.cs:44-49` - UserProfile already wired to DB and RabbitMQ.
  - Pattern: `Backend/src/FoodSocial.Contracts/Identity/UserProvisionedIntegrationEvent.cs:3-8` - starting contract to evolve for bootstrap.
  - Pattern: `docs/project-discovery/10_architecture.md:75-76` - Identity owns identity mapping; UserProfile owns profile data.
  - Pattern: `docs/project-discovery/12_backend_structure.md:64-65` - user/profile ownership boundary.
  - Pattern: `docs/project-discovery/12_backend_structure.md:137-142` - duplicate delivery and replay safety expectations.

  **Acceptance Criteria** (agent-executable only):
  - [ ] `dotnet test Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj --filter "FullyQualifiedName~UserProfileBootstrap"` succeeds.
  - [ ] Integration tests prove first consumed event creates exactly one profile shell.
  - [ ] Integration tests prove duplicate or replayed events do not create additional profile rows.
  - [ ] Integration tests prove Identity bootstrap state transitions from pending to active/completed only after downstream success.

  **QA Scenarios** (MANDATORY - task incomplete without these):
  ```
  Scenario: First bootstrap event creates profile shell once
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj" --filter "FullyQualifiedName~UserProfileBootstrap_FirstEventCreatesProfile" --logger "trx;LogFileName=task-8-bootstrap-create.trx"`
    Expected: Test passes and verifies one profile shell exists for the identity and Identity bootstrap status becomes completed/active per plan.
    Evidence: .sisyphus/evidence/task-8-bootstrap-create.trx

  Scenario: Replay or duplicate event is idempotent
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj" --filter "FullyQualifiedName~UserProfileBootstrap_DuplicateEventIgnored" --logger "trx;LogFileName=task-8-bootstrap-duplicate.trx"`
    Expected: Test passes and verifies no extra profile row is created and duplicate processing is safely acknowledged or skipped.
    Evidence: .sisyphus/evidence/task-8-bootstrap-duplicate-error.trx
  ```

  **Commit**: YES | Message: `feat(userprofile): consume identity bootstrap idempotently` | Files: [`Backend/src/UserProfile/**`, `Backend/src/FoodSocial.Contracts/**`, `Backend/tests/**`]

- [x] 9. Execute full production-path verification: auth, RBAC, outbox, duplicate delivery, and degraded downstream cases

  **What to do**: Build the complete AppHost-backed verification suite and scripted evidence collection for the final system behavior. Cover: Keycloak token acquisition, BFF session path, Identity current-user endpoint, ensure/create flow, repeat ensure idempotency, role-protected endpoint access, refresh/session lifecycle, outbox publication/drain, UserProfile bootstrap success, UserProfile outage/degraded pending-bootstrap behavior, publish failure retry, and duplicate message replay. This task is where the repo gains operator-grade confidence instead of just unit confidence.
  **Must NOT do**: Do not rely on browser-only manual checks. Do not treat `dotnet build` as sufficient evidence. Do not leave any critical branch of the hybrid flow unverified.

  **Recommended Agent Profile**:
  - Category: `deep` - Reason: end-to-end production-path verification across multiple services and failure modes.
  - Skills: [`dotnet-best-practices`, `test-driven-development`] - keep the suite maintainable and deterministic.
  - Omitted: [`playwright-best-practices`] - the core verification is service/API/integration centric.

  **Parallelization**: Can Parallel: NO | Wave 3 | Blocks: [10] | Blocked By: [3, 4, 5, 6, 7, 8]

  **References** (executor has NO interview context - be exhaustive):
  - Pattern: `docs/delivery/plans/2026-04-07-local-auth-and-identity-bootstrap-runbook.md:32-205` - existing manual verification script to convert into automated tests.
  - Pattern: `docs/project-discovery/05_non_functional_requirements.md:76-105` - reliability, logging, and monitoring expectations.
  - Pattern: `docs/project-discovery/10_architecture.md:17-27` - architecture anchors for reliability and degradation.
  - Pattern: `docs/context/current_build_status.md:117-125` - this is the first real Identity business slice and should update current-state reality when complete.
  - Pattern: `Backend/src/FoodSocial.Bff/Program.cs:97-135` - auth endpoints under verification.
  - Pattern: `Backend/src/Identity/Api/Controllers/CurrentUserController.cs:13-35` - current-user endpoint under verification.

  **Acceptance Criteria** (agent-executable only):
  - [ ] `dotnet test Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj --logger "trx;LogFileName=task-9-full-suite.trx"` succeeds.
  - [ ] End-to-end tests prove exactly one local identity row and exactly one profile shell row after repeated auth/bootstrap attempts for the same subject.
  - [ ] End-to-end tests prove downstream outage leaves login usable but marks bootstrap pending with recoverable retry path.
  - [ ] End-to-end tests prove publish failure does not lose the event and recovery eventually completes bootstrap.

  **QA Scenarios** (MANDATORY - task incomplete without these):
  ```
  Scenario: Full happy path from token to bootstrap completion
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj" --filter "FullyQualifiedName~IdentityFlow_HappyPath" --logger "trx;LogFileName=task-9-happy.trx"`
    Expected: Test passes and shows token acquisition, BFF session establishment, Identity ensure, outbox publication, UserProfile bootstrap, and completed bootstrap status.
    Evidence: .sisyphus/evidence/task-9-happy.trx

  Scenario: Downstream outage preserves auth and pending bootstrap state
    Tool: Bash
    Steps: Run `dotnet test "Backend/tests/FoodSocial.AppHost.Tests/FoodSocial.AppHost.Tests.csproj" --filter "FullyQualifiedName~IdentityFlow_UserProfileDown" --logger "trx;LogFileName=task-9-userprofile-down.trx"`
    Expected: Test passes and proves login/session remains usable, bootstrap status remains pending, and replay/retry can later complete the profile shell.
    Evidence: .sisyphus/evidence/task-9-userprofile-down-error.trx
  ```

  **Commit**: YES | Message: `test(identity): verify hybrid provisioning and failure paths` | Files: [`Backend/tests/**`]

- [x] 10. Update repository context and operational artifacts to match the new Identity reality

  **What to do**: After implementation and verification are green, update `docs/context/current_build_status.md` and `docs/context/progress_log.md` so the repo truth reflects the completed Identity slice, new test infrastructure, RBAC capability, hybrid provisioning reliability, and remaining known gaps. If any delivery artifacts are superseded by the completed plan, note that in the context/progress layer without duplicating implementation detail. Keep updates concise and factual.
  **Must NOT do**: Do not turn context docs into a plan. Do not leave the repo claiming Identity is still scaffold-only if this slice is complete. Do not duplicate discovery-level architecture truth.

  **Recommended Agent Profile**:
  - Category: `writing` - Reason: concise, accurate repository-context updates after verified work.
  - Skills: [] - no extra skill needed beyond disciplined doc updates.
  - Omitted: [`clean-code`] - documentation/context update only.

  **Parallelization**: Can Parallel: NO | Wave 3 | Blocks: [Final Verification Wave] | Blocked By: [9]

  **References** (executor has NO interview context - be exhaustive):
  - Pattern: `AGENTS.md:101-107` - update context files when implementation status materially changes.
  - Pattern: `docs/ai/workflows.md:192-206` - context maintenance and evidence-before-completion rules.
  - Pattern: `docs/context/current_build_status.md:1-127` - overwrite-only current state snapshot.
  - Pattern: `docs/context/progress_log.md` - append milestone entry format.
  - Pattern: `docs/delivery/plans/2026-04-07-local-auth-and-identity-bootstrap-runbook.md:213-220` - items that were previously out of scope and may now be complete.

  **Acceptance Criteria** (agent-executable only):
  - [ ] `dotnet build Backend/FoodSocial.slnx` still succeeds after doc/context updates.
  - [ ] `docs/context/current_build_status.md` no longer describes Identity as only a bootstrap slice if tasks 1-9 are complete.
  - [ ] `docs/context/progress_log.md` contains a milestone entry summarizing Identity completion and verification evidence.

  **QA Scenarios** (MANDATORY - task incomplete without these):
  ```
  Scenario: Current build status reflects new implementation truth
    Tool: Read
    Steps: Read `docs/context/current_build_status.md` after updates and verify it mentions completed Identity lifecycle/RBAC/hybrid provisioning work plus test infrastructure status.
    Expected: File content is concise, current-state focused, and consistent with completed verification results.
    Evidence: .sisyphus/evidence/task-10-current-build-status.md

  Scenario: Progress log records milestone cleanly
    Tool: Read
    Steps: Read `docs/context/progress_log.md` after update and verify one new milestone entry was appended for the Identity completion slice.
    Expected: File contains a factual milestone entry without duplicating the full plan.
    Evidence: .sisyphus/evidence/task-10-progress-log.md
  ```

  **Commit**: YES | Message: `docs(context): record identity service completion state` | Files: [`docs/context/current_build_status.md`, `docs/context/progress_log.md`]

## Final Verification Wave (MANDATORY — after ALL implementation tasks)
> 4 review agents run in PARALLEL. ALL must APPROVE. Present consolidated results to user and get explicit "okay" before completing.
> **Do NOT auto-proceed after verification. Wait for user's explicit approval before marking work complete.**
> **Never mark F1-F4 as checked before getting user's okay.** Rejection or user feedback -> fix -> re-run -> present again -> wait for okay.
- [ ] F1. Plan Compliance Audit — oracle
- [ ] F2. Code Quality Review — unspecified-high
- [ ] F3. Real Manual QA — unspecified-high (+ playwright if UI)
- [ ] F4. Scope Fidelity Check — deep

## Commit Strategy
- Commit 1: `test(identity): add failing coverage for ensure flow and RBAC`
- Commit 2: `feat(identity): implement local identity lifecycle and reliable provisioning`
- Commit 3: `feat(bff): add identity orchestration and refresh flow`
- Commit 4: `feat(userprofile): consume identity bootstrap event idempotently`
- Commit 5: `docs(context): update current build status and progress log`

## Success Criteria
- FoodSocial can authenticate through Keycloak and establish BFF session state without frontend-to-microservice sprawl.
- Identity can ensure exactly one local identity per Keycloak subject and reconcile allowed claim fields deterministically.
- Global/account roles are controlled by app code and enforced through explicit authorization policies.
- UserProfile bootstrap occurs asynchronously, durably, and idempotently.
- Operator evidence exists for duplicate delivery, downstream outage, and replay-safe recovery behavior.
