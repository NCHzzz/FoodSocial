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
- agent-friendly platform contracts and APIs
- practical nearby and area-aware food/place discovery
- explicit media lifecycle support for upload, derivative handling, and deletion cleanup expectations
- lightweight runtime feature controls for beta rollout and safety fallback behavior

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

Given the current delivery context:

- solo builder
- AI-assisted coding workflow
- 2-month target timeline

the MVP feature list should later be validated against implementation feasibility and may require a launch-sequencing decision inside the MVP itself.

The likely risk areas are:

- AI recommendation depth
- moderation sophistication
- search quality
- content modeling across food posts, place posts, and recipe posts
- solo delivery capacity versus current MVP breadth
- media lifecycle and deletion complexity
- async messaging correctness and operator response clarity

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

- Search remains PostgreSQL-first in MVP even if later hybrid or vector retrieval becomes attractive
- Caching remains selective and evidence-driven rather than an assumed platform requirement
- Moderation remains manual-review-first even if later automation hooks are preserved
- Feature rollout safety should remain lightweight and practical for a limited beta
