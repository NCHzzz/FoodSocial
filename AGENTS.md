# AGENTS.md

## Purpose

This is the root operating contract for AI agents working in this repo.

## Source Order

Read in this order:

1. `AGENTS.md`
2. `docs/ai/repo-map.md`
3. relevant `docs/project-discovery/` files
4. `docs/ai/workflows.md`
5. relevant stack rules in `docs/ai/stack-rules/` and `docs/ai/engineering-rules.md`
6. `docs/context/current_build_status.md` when current implementation state matters
7. relevant files in `docs/delivery/` only when curated markdown records already exist for the task

This is the single canonical load order. Other files reference it but do not restate it.

## Priority

- user instructions override repo workflow guidance
- repo workflow guidance overrides default assistant habits
- `docs/project-discovery/` is the factual product and architecture truth

## Project Profile

- Product: AI-native Food-first social platform
- Frontend: Angular
- Backend: .NET 10 microservices
- Local orchestration: .NET Aspire
- Datastores: PostgreSQL per service
- Auth: self-hosted Keycloak
- Edge API style: API Gateway / BFF
- Internal integration: Hybrid REST + async events
- Messaging: RabbitMQ
- Deployment: Kubernetes
- Launch market: Vietnam

## Working Rules

- Markdown in the repo must remain English
- do not invent business rules that discovery docs have not approved
- keep implementation aligned with the MVP direction in `docs/project-discovery/`
- keep moderation manual-review-first in MVP
- product-level entity decisions (such as first-class status of Dish and Place) are owned by `docs/project-discovery/`, not by agent rule files

## Rule Loading by Task Type

Task-type rule files are a required load step before implementation, not a soft reference.

After reading `docs/ai/workflows.md`, agents MUST explicitly load the rule files for the current task type before editing.

For the full task-type rule table and ambiguity routing, see `docs/ai/workflows.md`.

## Hard Gates

- do not implement before loading relevant discovery truth
- do not treat assumptions as facts when the repo already contains the answer
- do not claim completion before verification
- do not leave material state changes undocumented in `docs/context/`

## Definition of Done

A task is complete only when:

- requested behavior is implemented
- modified paths fit repo architecture
- diagnostics or validation are clean where applicable
- behavior was exercised when behavior changed
- the summary states what changed, where, and any follow-up

## Expansion Rule

If the workflow becomes confusing, simplify it in `docs/ai/` rather than adding more competing doc layers.
