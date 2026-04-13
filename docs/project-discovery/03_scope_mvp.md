# 03_scope_mvp.md

## Status

- Working draft

## In Scope for MVP

- user registration and authentication through self-hosted Keycloak
- user profile
- create posts about food, restaurants, and recipes
- discovery feed
- like, comment, save, and share
- search by dish, restaurant, and location
- follow other users
- AI-based food recommendation
- moderation and reporting
- responsive web experience
- AI-assisted user discovery and decision support
- practical nearby and area-aware food/place discovery

Detailed product behavior for in-scope capabilities lives in `04_functional_requirements.md`.

Technical implementation direction for AI, search, media lifecycle, and rollout safety lives in `10_architecture.md` and `05_non_functional_requirements.md`.

## Out of Scope for MVP

- native mobile apps
- direct food ordering with delivery partners
- deep restaurant partner portal (Assumption)
- advanced creator monetization (Assumption)
- complex ad platform (Assumption)
- deep internal multi-agent product behavior
- active monetization features in MVP
- heavyweight dedicated search infrastructure beyond the approved PostgreSQL-first approach
- vector-search infrastructure as a default MVP dependency
- platform-wide cache dependency introduced before measured bottlenecks justify it
- heavyweight experimentation platform

## MVP Boundary

The MVP is already broad and should be treated as a focused but usable first product release rather than a minimal prototype.

Given the current delivery context, the MVP should remain strict enough for a solo, AI-assisted build with a limited real-user beta target.

Delivery framing lives in `00_project_overview.md`.

Risk detail lives in `14_risks_open_questions.md` and `10_architecture.md`.

## Possible Phase 2 / Future Ideas

- native mobile apps
- food delivery / ordering integration
- richer AI personalization
- internal AI agents for moderation, curation, or concierge workflows
- partner ecosystem for restaurants or delivery platforms
- advanced trust and anti-seeding systems

## Need Confirmation

- Whether all listed MVP features are equally mandatory for first launch or whether some can be staged within MVP

## Recipe Scope Direction

- Recipe is included in MVP as a structured post type with basic ingredients, steps, and media
- Recipe is not yet treated as a deep standalone module

## MVP Delivery Constraints to Preserve

- Search remains PostgreSQL-first in MVP.
- Caching remains selective and evidence-driven.
- Moderation remains manual-review-first.
- Feature rollout safety remains lightweight and practical.

Canonical technical detail for these constraints lives in `05_non_functional_requirements.md` and `10_architecture.md`.
