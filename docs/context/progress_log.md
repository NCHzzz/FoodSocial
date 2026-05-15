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
