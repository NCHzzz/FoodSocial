# RULES.md

## Purpose

This file contains durable repo-wide rules for AI agents and human collaborators.

## Must Always

- Keep all Markdown documentation in English
- Treat `docs/project-discovery/` as the product and architecture source of truth
- Treat `docs/ai/` as the agent operating layer
- Read existing discovery docs before implementing or changing behavior
- Keep changes aligned to the approved MVP direction
- Preserve first-class treatment of `Dish` and `Place`
- Keep recommendation medium-depth in MVP and future-extensible
- Keep moderation manual-review-first in MVP

## Must Never

- Do not silently expand MVP scope
- Do not rewrite project truth into parallel docs without a clear need
- Do not convert future-phase ideas into current requirements without approval
- Do not bypass architecture invariants such as BFF, Hybrid REST + async events, service-owned PostgreSQL, and Keycloak-based auth

## Documentation Rule

- Interviews may be in Vietnamese
- Final repository Markdown deliverables must remain in English

## AI-Workflow Rule

- Root policy belongs in `AGENTS.md` and `RULES.md`
- Operational workflows belong in `docs/ai/`
- Factual product/system truth belongs in `docs/project-discovery/`
