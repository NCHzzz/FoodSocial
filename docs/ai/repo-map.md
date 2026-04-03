# repo-map.md

## Purpose

This file helps AI agents navigate the repository quickly without guessing.

## Top-Level Structure

- `Backend/` — backend implementation area for .NET services
- `Frontend/` — Angular frontend implementation area
- `docs/project-discovery/` — product, domain, architecture, API, and backlog source-of-truth docs
- `docs/ai/` — AI operating layer for repository-native vibecoding workflows
- `AGENTS.md` — canonical agent contract

## AI Engineering Docs

- `docs/ai/engineering-rules.md` — repo-wide engineering discipline
- `docs/ai/stack-rules/dotnet.md` — .NET 10 backend standards
- `docs/ai/stack-rules/angular-tailwind.md` — Angular + TailwindCSS frontend standards

## Most Important Product Docs

- `docs/project-discovery/00_project_overview.md`
- `docs/project-discovery/04_functional_requirements.md`
- `docs/project-discovery/06_user_flows.md`
- `docs/project-discovery/08_api_spec.md`
- `docs/project-discovery/10_architecture.md`
- `docs/project-discovery/12_backend_structure.md`
- `docs/project-discovery/13_backlog_mvp.md`

## What Each Discovery File Is Best For

- `00_project_overview.md` — product identity, stack, launch context, major approved directions
- `01_business_context.md` — pain points, market direction, success metrics, monetization direction
- `02_personas_and_roles.md` — personas, roles, permissions, profile direction
- `03_scope_mvp.md` — in-scope vs out-of-scope boundaries
- `04_functional_requirements.md` — concrete functional expectations and major business rules
- `05_non_functional_requirements.md` — operational quality, security, monitoring, moderation posture
- `06_user_flows.md` — core user journeys and alternate flow direction
- `07_domain_model.md` — domain entities, relationships, aggregate directions
- `08_database_design.md` — database shape, keys, indexes, persistence direction
- `08_api_spec.md` — API surfaces, request/response shapes, auth model, and event direction
- `10_architecture.md` — macro architecture, trade-offs, deployment, tenant direction
- `11_frontend_structure.md` — Angular-facing page/module/UX structure
- `12_backend_structure.md` — service boundaries, auth, messaging, contracts
- `13_backlog_mvp.md` — epics, candidate stories, product outcome direction
- `14_risks_open_questions.md` — risk registry and residual uncertainty
- `15_prompt_for_codegen.md` — AI codegen-oriented implementation bootstrap support file, not primary product truth

## Current Approved System Shape

- Food-first AI-native social platform
- Dishes first, places second for recommendation
- Discover feed is the main recommendation surface
- Recommendation depth is medium in MVP
- Manual-review-first moderation in MVP
- Self-hosted Keycloak
- API Gateway / BFF
- Hybrid REST + async events
- RabbitMQ
- Kubernetes deployment target
- Vietnam-first launch

## Where Agents Should Look First By Task Type

### Product or feature understanding

- `00_project_overview.md`
- `04_functional_requirements.md`
- `06_user_flows.md`
- `13_backlog_mvp.md`

### Backend/API work

- `08_api_spec.md`
- `10_architecture.md`
- `12_backend_structure.md`
- `08_database_design.md`
- `07_domain_model.md`
- `docs/ai/engineering-rules.md`
- `docs/ai/stack-rules/dotnet.md`

### Frontend/UI work

- `11_frontend_structure.md`
- `06_user_flows.md`
- `04_functional_requirements.md`
- `docs/ai/engineering-rules.md`
- `docs/ai/stack-rules/angular-tailwind.md`

### Risk/trade-off checks

- `14_risks_open_questions.md`
- `10_architecture.md`

## Guidance for Future Nested Context

If future implementation grows large enough, add nested `AGENTS.md` files under `Backend/` or `Frontend/` only when repeated agent confusion proves they are needed.

## Minimal AI Layer Rule

This repository intentionally keeps the AI operating layer minimal to reduce model confusion.

- `AGENTS.md` is the canonical operating contract
- `docs/ai/repo-map.md` is the navigation layer
- `docs/ai/workflows.md` is the execution layer
- `docs/project-discovery/` is the factual product and architecture layer
