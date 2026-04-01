# workflows.md

## Purpose

This file defines the default task workflows AI agents should follow in this repository.

## Workflow 1: Add or change a product feature

Use when implementing or refining a user-visible feature.

### Steps

1. Read `docs/project-discovery/00_project_overview.md`
2. Read `04_functional_requirements.md` and `06_user_flows.md`
3. Read the relevant architecture file:
   - frontend work -> `11_frontend_structure.md`
   - backend work -> `09_api_and_integration_design.md` and `12_backend_structure.md`
4. Check `13_backlog_mvp.md` for story direction
5. Implement only the approved MVP shape
6. Validate behavior and summarize what changed

## Workflow 2: Backend/API change

Use when touching services, APIs, contracts, persistence, or messaging.

### Steps

1. Read `09_api_and_integration_design.md`
2. Read `10_architecture.md` and `12_backend_structure.md`
3. Read `07_domain_model.md` and `08_database_design.md`
4. Preserve these invariants:
   - API Gateway / BFF at the edge
   - Hybrid REST + async events internally
   - RabbitMQ for async messaging
   - PostgreSQL per service
5. Keep future extensibility for broader agent actions, stronger recommendation, and monetization

## Workflow 3: Frontend/UI change

Use when touching Angular routes, UX flows, profile, notifications, or feed behavior.

### Steps

1. Read `11_frontend_structure.md`
2. Read `06_user_flows.md`
3. Read `04_functional_requirements.md`
4. Preserve user-facing truths:
   - Food-first UI
   - Discover feed is the primary recommendation surface
   - Following feed is follower-prioritized but may include limited mixed content
   - Notification center exists in MVP
5. Keep flows understandable and not misleading

## Workflow 4: Recommendation or AI change

Use when touching search, recommendation, personalization, AI explanation, or agent exposure.

### Steps

1. Read `00_project_overview.md`
2. Read `04_functional_requirements.md`
3. Read `09_api_and_integration_design.md`
4. Read `10_architecture.md`
5. Preserve these constraints:
   - medium personalization in MVP
   - dishes first, places second
   - short recommendation explanations only
   - PostgreSQL-first search in MVP
   - multi-provider AI abstraction

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

## Workflow 6: Repo or planning update

Use when improving docs, prompts, planning, or AI operating files.

### Steps

1. Do not duplicate discovery facts unnecessarily
2. Keep `AGENTS.md` minimal and operational
3. Put factual product/system truth in `docs/project-discovery/`
4. Put agent-operating guidance in `docs/ai/`
5. Keep tool-specific adapters thin

## Common Footguns to Avoid

- Expanding future-phase ideas into MVP by accident
- Treating tenant capability as a fully exercised MVP business model when it is currently future-oriented
- Ignoring the operational burden of Keycloak + Kubernetes + microservices
- Making Discover and Following feel indistinguishable
- Adding heavy recommendation or moderation automation too early
