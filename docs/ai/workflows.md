# workflows.md

## Purpose

This file defines the default task workflows AI agents should follow in this repository.

## Global Workflow Shape

1. Load repo rules from `AGENTS.md`
2. Load navigation from `docs/ai/repo-map.md`
3. Load only the relevant product/architecture docs from `docs/project-discovery/`
4. Load `docs/context/current_build_status.md` when current implementation progress, active skeleton state, or current priority signals matter
5. Load `docs/context/progress_log.md` when recent implementation history matters
6. Load relevant active specs and plans from `docs/delivery/` when they already exist for the task
7. Follow the matching workflow below

## Global Workflow Gates

- Do not start implementation before the relevant discovery context is loaded
- Do not start implementation before the task interpretation and scope boundary are explicit
- For non-trivial work, do not skip the spec/plan flow when the task needs design or execution artifacts
- Do not skip verification and then describe work as complete
- Do not update code in a way that materially changes current state without updating `docs/context/` when required
- Do not treat support/bootstrap docs as product source of truth

## Interpretation and Scope Rule

- Before changing code or docs, state the task interpretation clearly enough to avoid ambiguous implementation
- For non-trivial work, identify the affected subsystem or file group before editing
- For non-trivial work, keep project-state context separate from specs and plans
- Do not carry unresolved ambiguity into implementation if it can be resolved from repo docs

## Spec and Plan Rule

- Use `docs/delivery/specs/` for approved or approval-seeking design artifacts on non-trivial work
- Use `docs/delivery/plans/` for execution-ready implementation plans derived from approved specs
- Use `docs/context/` only for current project state, blockers, and milestone progress
- Do not store implementation plans in `docs/context/`
- Archive completed, cancelled, or superseded delivery artifacts under `docs/delivery/archive/` when they still have reference value

## Workflow 1: Add or change a product feature

Use when implementing or refining a user-visible feature.

### Steps

1. Read `docs/project-discovery/00_project_overview.md`
2. Read `04_functional_requirements.md` and `06_user_flows.md`
3. Read the relevant architecture file:
   - frontend work -> `11_frontend_structure.md`
   - backend work -> `08_api_spec.md` and `12_backend_structure.md`
4. Read `docs/ai/engineering-rules.md`
5. Read the relevant stack rules file when needed
6. For non-trivial work, create or update a spec in `docs/delivery/specs/` before implementation
7. For approved non-trivial work, create or update a plan in `docs/delivery/plans/`
8. Check `13_backlog_mvp.md` for story direction
9. Implement only the approved MVP shape
10. Validate behavior and summarize what changed

Exit criteria:

- the feature matches approved MVP behavior
- relevant implementation validation has been performed

## Workflow 2: Backend/API change

Use when touching services, APIs, contracts, persistence, or messaging.

### Steps

1. Read `08_api_spec.md`
2. Read `10_architecture.md` and `12_backend_structure.md`
3. Read `07_domain_model.md` and `08_database_design.md`
4. Read `docs/ai/engineering-rules.md`
5. Read `docs/ai/stack-rules/dotnet.md`
6. Preserve these invariants:
   - API Gateway / BFF at the edge
   - Hybrid REST + async events internally
   - RabbitMQ for async messaging
   - PostgreSQL per service
7. For non-trivial work, create or update a spec in `docs/delivery/specs/` before implementation
8. For approved non-trivial work, create or update a plan in `docs/delivery/plans/`
9. Keep future extensibility for broader agent actions, stronger recommendation, and monetization

Exit criteria:

- service boundaries remain explicit
- contracts, persistence, and integration behavior remain aligned to architecture docs

### Debugging discipline for backend/API work

- begin with reproduction and error inspection
- trace the failing path before proposing a fix
- prefer one root-cause hypothesis at a time over broad speculative edits

## Workflow 3: Frontend/UI change

Use when touching Angular routes, UX flows, profile, notifications, or feed behavior.

### Steps

1. Read `11_frontend_structure.md`
2. Read `06_user_flows.md`
3. Read `04_functional_requirements.md`
4. Read `docs/ai/engineering-rules.md`
5. Read `docs/ai/stack-rules/angular-tailwind.md`
6. Preserve user-facing truths:
   - Food-first UI
   - Discover feed is the primary recommendation surface
   - Following feed is follower-prioritized but may include limited mixed content
   - Notification center exists in MVP
7. For non-trivial work, create or update a spec in `docs/delivery/specs/` before implementation
8. For approved non-trivial work, create or update a plan in `docs/delivery/plans/`
9. Keep flows understandable and not misleading

Exit criteria:

- UX remains aligned to the approved product behavior
- frontend changes do not silently redefine product rules

## Workflow 4: Recommendation or AI change

Use when touching search, recommendation, personalization, AI explanation, or agent exposure.

### Steps

1. Read `00_project_overview.md`
2. Read `04_functional_requirements.md`
3. Read `08_api_spec.md`
4. Read `10_architecture.md`
5. Preserve these constraints:
   - medium personalization in MVP
   - dishes first, places second
   - short recommendation explanations only
  - PostgreSQL-first search in MVP
  - multi-provider AI abstraction
6. For non-trivial work, create or update a spec in `docs/delivery/specs/` before implementation
7. For approved non-trivial work, create or update a plan in `docs/delivery/plans/`

Exit criteria:

- AI assistance remains optional in failure scenarios
- deterministic fallback behavior is still preserved

## Workflow 5: Moderation or trust change

Use when touching reporting, review workflows, restrictions, appeal-readiness, or moderation notifications.

### Steps

1. Read `04_functional_requirements.md`
2. Read `05_non_functional_requirements.md`
3. Read `06_user_flows.md`
4. Read `13_backlog_mvp.md`
5. Preserve these truths:
   - report targets: post, user, comment
  - manual-review-first in MVP
  - no user-facing appeal flow in MVP
  - future appeal and auto-hide readiness must remain possible
6. For non-trivial work, create or update a spec in `docs/delivery/specs/` before implementation
7. For approved non-trivial work, create or update a plan in `docs/delivery/plans/`

Exit criteria:

- moderation behavior matches current MVP policy exactly
- future extensibility is preserved without silently enabling future-phase behavior now

## Workflow 6: Repo or planning update

Use when improving docs, prompts, planning, or AI operating files.

### Steps

1. Do not duplicate discovery facts unnecessarily
2. Keep `AGENTS.md` minimal and operational
3. Put factual product/system truth in `docs/project-discovery/`
4. Put agent-operating guidance in `docs/ai/`
5. Prefer fewer canonical files over many thin wrappers
6. Put implementation progress and current-state notes in `docs/context/`
7. Put approved specs in `docs/delivery/specs/`
8. Put execution-ready implementation plans in `docs/delivery/plans/`
9. Archive completed, cancelled, or superseded delivery artifacts under `docs/delivery/archive/` when they still have reference value
10. Do not assume an automatic `docs/superpowers/` path in this repository unless such a folder and workflow are explicitly added

## Self-Review Before Completion

- check for contradictions against discovery docs
- check for accidental scope expansion
- check for stale placeholders or outdated references
- check that the verification performed actually matches the completion claim

## Context Maintenance Rule

- Update `docs/context/current_build_status.md` when implementation status, blockers, or current priority materially change
- Append to `docs/context/progress_log.md` when the repository materially advances
- Do not leave major setup or architecture changes undocumented in the context layer
- Keep `current_build_status.md` as an overwrite-only snapshot, not a running journal
- Keep `progress_log.md` milestone-only and archive older entries when size starts hurting context efficiency
- Keep specs and plans out of `docs/context/`
- Archive stale delivery artifacts instead of letting active folders accumulate outdated plans

## Evidence Before Completion

- describe work as complete only after fresh verification
- prefer concrete evidence over “should work” language
- if setup, build, or runtime state changed, ensure the context layer reflects that new reality

## Common Footguns to Avoid

- Expanding future-phase ideas into MVP by accident
- Treating tenant capability as a fully exercised MVP business model when it is currently future-oriented
- Ignoring the operational burden of Keycloak + Kubernetes + microservices
- Making Discover and Following feel indistinguishable
- Adding heavy recommendation or moderation automation too early
- Creating too many markdown files that restate the same guidance in different words

## Clean Code Expectations

- Prefer explicit, readable, domain-aligned code
- Keep functions, classes, and modules focused on one clear responsibility
- Preserve clear boundaries between UI, transport, application logic, and persistence
- Avoid duplication and accidental divergence across services
- Favor maintainability and local clarity over premature abstraction
