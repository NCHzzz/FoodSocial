# progress_log.md

## Purpose

This file records milestone-level implementation progress.

## Update Rule

Append an entry when any of the following happens:

- a new service, app, or major module is created
- a major architectural decision is implemented in code
- a vertical slice becomes runnable end to end
- local orchestration or infrastructure setup changes materially
- a major feature moves from planned to implemented
- documentation structure changes materially enough to affect agent workflow

Do not append minor churn, repetitive edits, or every small refactor.

## Size Control Rule

- keep only recent and still-useful milestones in this file
- archive older entries under `docs/context/archive/`
- do not load full historical progress by default when current implementation state is enough
- keep one entry per day unless multiple milestones are materially different and still useful to future work

## Entry Format

```md
## YYYY-MM-DD

- What changed
- Why it matters
- What is unblocked next
```

## 2026-05-07

- Promoted root `DESIGN.md` as the canonical FoodSocial UI/UX design source.
- Reworked the Angular UI surface toward the warm editorial design system and removed rule-breaking dynamic Tailwind usage.
- Verified the frontend with `npm test -- --watch=false` and `npm run build`.

## 2026-05-12

- Removed project-local `.agents/skills` entries that duplicated global skills under `C:\Users\Admin\.agents\skills`, while keeping FoodSocial-specific repo skills.
- Hardened backend architecture across BFF/Post trust, tenant isolation, PostgreSQL-first search, media storage deployment, Place.Api deployability, Post metrics, and starter ops runbooks.
- Added service-owned EF Core migrations for default tenant isolation and tenant-scoped search indexes.
- Reframed project-discovery docs from MVP-only wording toward a production-bound product model with staged maturity gates.
- Added `docs/project-discovery/16_maturity_roadmap.md` to define beta, public launch, social scale, and platform expansion stages.

## 2026-05-13

- Renamed old docs to product-oriented names: `03_product_scope.md`, `09_database_design.md`, `13_product_backlog.md`, and `product_launch_readiness.md`.
- Compressed `current_build_status.md` back into a current-state snapshot and archived older progress milestones.
- Removed the duplicate production readiness checklist because readiness gates now live canonically in `docs/context/product_launch_readiness.md`.
- Removed stale delivery archive plans and the old frontend UI compatibility shim after `README.md`, `Frontend/README.md`, and active agent rules pointed to current canonical docs.

## 2026-05-15

- Restructured the AI agent documentation system for consistency and completeness.
- Unified the canonical load order in `AGENTS.md` (single source of truth, no duplication in `workflows.md`).
- Added 5 new task types to `workflows.md`: Full-Stack, DevOps/Infra, Database Migration, Documentation, Architecture.
- Added ambiguous-task routing rules and "When Things Go Wrong" error recovery guidance.
- Centralized MUST/SHOULD/MAY rule-level definitions in `engineering-rules.md`.
- Added Frontend Security, Observability, Testing sections and 12-point Verification Checklist to `angular-tailwind.md`.
- Added API versioning strategy and pagination defaults to `dotnet.md`.
- Added Deferred Design Decisions section (dark mode, animation, breakpoints) to `DESIGN.md`.
| Created `docs/ai/git-rules.md` to standardize version control practices (commit formatting, pull strategy, push requirements).
| Removed all OpenSpec references and deleted `openspec/` directory.
| Deduplicated content across files: migration commands point to `Backend/MIGRATIONS.md`, doc pointers point to `repo-map.md`.
| Outbox jitter, DPUnity `UseCases/<Capability>/` subfolders, typed wire constants, and BFF session facade formalized (see entry below for the 2026-06-05 Phase A + D pass).


## 2026-06-05

- Completed Phase A code-health cleanup across the backend: Post use cases moved into `UseCases/<Capability>/` subfolders with matching namespaces; `Post/Domain/PostEntities.cs` (8 entities + 5 enums) split into one type per file; `Post/Api/Controllers/PostsController.cs` split into 7 per-resource controllers; `Bff/Endpoints/BffPostEndpoints.cs` split into `BffPostReadEndpoints`, `BffPostWriteEndpoints`, `BffPostModerationEndpoints`; `UserProfile/Api/Controllers/UsersController.cs` split into 4 per-resource controllers; magic-string status/role comparisons in BFF + `CatalogHttpClients` replaced with typed `FoodSocial.Contracts.*WireConstants`; `DefaultTenantId` literal centralized in `FoodSocial.Contracts.Common.TenantDefaults` and consumed by all 5 infrastructure stores; new `Bff/Session/IBffSessionContext` + `Bff/Session/BffSessionContext` + `Bff/Errors/BffProblemTypes` + `Bff/Errors/BffProblemResults` extracted from inline copies.
- Added `Dish.Domain.Tests` (11 tests), `Place.Domain.Tests` (15 tests), `Post.Domain.Tests` (17 tests) projects and registered them in `FoodSocial.slnx`. Coverage spans Dish/Place creation, validation, Vietnamese diacritic normalization, and lifecycle transitions; Post invariants including media-required, rating bounds, tag normalization, place-id-empty normalization, and `MarkAttached`/`MarkRead` lifecycle helpers.
- Outbox publisher in Identity gained ±25% jitter on the 2-second drain delay to avoid thundering-herd when many service instances boot together.
- Standardized `AddServiceDefaults()` call style in `UserProfile.Api.Program.cs` (was the long form).
- Verification: `dotnet build` 0 errors / 0 warnings; 275/275 tests pass across Dish, Place, Identity (Domain + Application + Api), UserProfile, Post, ApiGateway, and BFF test suites. AppHost tests require Docker and were not executed.

> Deferral note: BFF session + AI gateway + feature-control abstractions remain deferred per `dotnet.md` YAGNI; Phase C features (block/mute, visibility, account notifications, idempotency keys, typo-tolerance) remain blocked on product decisions.
