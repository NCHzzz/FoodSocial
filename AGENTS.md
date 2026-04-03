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
5. relevant files in `docs/ai/stack-rules/` when the task is stack-specific

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

## Repo Working Model

Use this repository as a living AI workspace:

- `docs/project-discovery/` = product and architecture truth
- `docs/ai/` = agent operating layer
- `AGENTS.md` = canonical entrypoint for agents working in this repo

## Guardrails

- Do not create alternative architecture docs outside the approved structure
- Do not duplicate product truth across multiple files unless the duplication is intentional and minimal
- Do not expand MVP scope silently
- Do not convert future-phase ideas into MVP requirements unless explicitly approved

## Recommended Task Loop

1. Load `docs/ai/repo-map.md`
2. Load only the relevant `docs/project-discovery/` files
3. Use `docs/ai/workflows.md` for task execution patterns
4. Update or create code/docs without breaking existing source-of-truth structure

## Future Expansion Rule

If a workflow repeatedly causes confusion, add or update a supporting file under `docs/ai/` instead of bloating this file.
