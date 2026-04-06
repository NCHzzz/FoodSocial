# AGENTS.md

## Purpose

This repository is an AI-native product workspace for building a Food-first social media platform.

This file is the canonical operating contract for AI agents working in this repo.

## Source of Truth

Agents must use the following context order:

1. `AGENTS.md`
2. `docs/ai/repo-map.md`
3. relevant files in `docs/project-discovery/`
4. `docs/ai/workflows.md`
5. relevant files in `docs/delivery/specs/` and `docs/delivery/plans/` when the task already has approved delivery artifacts
6. relevant files in `docs/ai/stack-rules/` when the task is stack-specific
7. `docs/context/current_build_status.md` when current implementation status or project-state context matters

## Instruction Priority

- User instructions override repo workflow guidance
- Repo workflow guidance overrides default assistant habits
- Project-discovery docs are the factual product and architecture source of truth

## Project Profile

- Product: AI-native Food-first social platform
- Frontend: Angular
- Backend: .NET 10 microservices
- Local orchestration: .NET Aspire
- Datastores: PostgreSQL per service
- Auth: self-hosted Keycloak
- Frontend API style: API Gateway / BFF
- Internal integration style: Hybrid REST + async events
- Messaging: RabbitMQ
- Deployment target: Kubernetes
- Launch market: Vietnam

## Working Rules

- All Markdown documentation must remain in English
- Interviews or working discussions may happen in Vietnamese, but repository Markdown deliverables must remain in English
- Prefer reading existing docs and code patterns before proposing changes
- Do not invent missing business rules when discovery docs still mark them as future or TBD
- Keep implementation aligned with the approved MVP direction in `docs/project-discovery/`
- Treat `Dish` and `Place` as first-class entities
- Keep recommendation medium-depth in MVP, but preserve a path to stronger personalization later
- Keep moderation manual-review-first in MVP
- Preserve future extensibility for monetization, appeals, stronger recommendation, and broader agent actions

## Clean Code Rules

- Prefer small, composable units over large multi-purpose files or classes
- Keep naming explicit and domain-driven
- Avoid hidden behavior and surprising side effects
- Favor straightforward control flow over clever abstractions
- Keep business rules out of UI glue and transport glue where possible
- Keep service boundaries clear and contract-first
- Avoid duplication of business logic across services or layers
- Write code so a follow-up AI agent can understand intent quickly from local context

## Engineering Rules

- Use `docs/ai/engineering-rules.md` for repo-wide engineering expectations beyond generic clean code
- Use `docs/ai/stack-rules/dotnet.md` for backend .NET 10 implementation rules
- Use `docs/ai/stack-rules/angular-tailwind.md` for Angular and TailwindCSS implementation rules

## Definition of Done

A task is complete only when all applicable items are satisfied:

- requested behavior is implemented exactly
- modified paths stay consistent with repo architecture
- diagnostics or validation checks are clean for affected areas
- the feature is manually exercised when behavior changed
- summary clearly states what changed, where, and any follow-up required

## Hard Workflow Gates

- Do not implement before loading the relevant discovery docs for the task
- Do not treat assumptions as facts when the repo already contains a more authoritative source
- Do not claim a change is complete before verification is actually performed
- Do not leave material implementation or setup changes undocumented in `docs/context/` when current state or next steps change

## Repo Working Model

Use this repository as a living AI workspace:

- `docs/project-discovery/` = product and architecture truth
- `docs/ai/` = agent operating layer
- `docs/context/` = current project state and milestone context only
- `docs/delivery/` = approved specs, active plans, and archived delivery artifacts
- `AGENTS.md` = canonical entrypoint for agents working in this repo

## Guardrails

- Do not create alternative architecture docs outside the approved structure
- Do not duplicate product truth across multiple files unless the duplication is intentional and minimal
- Do not expand MVP scope silently
- Do not convert future-phase ideas into MVP requirements unless explicitly approved

## Recommended Task Loop

1. Read `docs/ai/repo-map.md`
2. Read only the relevant `docs/project-discovery/` files for the task
3. Read `docs/ai/workflows.md` for the execution pattern
4. Read `docs/context/current_build_status.md` if the task depends on current setup status, active skeleton state, or current priority signals
5. Read `docs/context/progress_log.md` when recent implementation history matters
6. Read relevant active specs or plans in `docs/delivery/` when they exist for the task
7. Read relevant stack rules when the task is stack-specific
8. Implement or update code/docs without breaking source-of-truth boundaries

## AI Agent Workflow

### Phase 1: Load context

- load repo rules from `AGENTS.md`
- load navigation from `docs/ai/repo-map.md`
- load only the relevant discovery docs
- load current build context when implementation status matters

### Phase 2: Decide task type

- product or feature work
- backend/API work
- frontend/UI work
- AI/recommendation work
- moderation/trust work
- repo/docs/workflow work

Exit gate for this phase:

- the task has a clear interpretation, clear scope boundary, and clear task type

### Phase 3: Follow the matching workflow

- use `docs/ai/workflows.md`
- use stack-specific rules only when needed
- keep product truth in `docs/project-discovery/`
- keep project-state context in `docs/context/`
- keep specs and implementation plans in `docs/delivery/`

Exit gate for this phase:

- the task has a clear implementation path and the relevant constraints are loaded

### Phase 4: Preserve repo truth boundaries

- product and architecture facts belong in `docs/project-discovery/`
- AI operating guidance belongs in `docs/ai/`
- current implementation status belongs in `docs/context/`
- approved specs and plans belong in `docs/delivery/`
- do not duplicate the same truth in multiple layers without a strong reason

### Phase 5: Maintain current context

- update `docs/context/current_build_status.md` when setup state, blockers, or current priority materially change
- append to `docs/context/progress_log.md` when implementation progress materially changes
- keep `current_build_status.md` as a short overwrite-only snapshot
- keep `progress_log.md` milestone-only and archive older entries when it grows too large
- archive completed or superseded delivery artifacts under `docs/delivery/archive/`

Exit gate for this phase:

- repo context still reflects current reality after the change

### Phase 6: Self-review before completion

- check for contradictions against discovery docs
- check for scope creep beyond the approved MVP direction
- check for placeholders, stale assumptions, or incomplete follow-through

Exit gate for this phase:

- the change is internally consistent and supported by verification evidence

## Future Expansion Rule

If a workflow repeatedly causes confusion, add or update a supporting file under `docs/ai/` instead of bloating this file.
