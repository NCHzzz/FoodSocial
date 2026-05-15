# 03_product_scope.md

## Status

- Working draft

## Product Scope

- user registration and authentication through self-hosted Keycloak
- user profile
- create posts about food, restaurants, and recipes
- following feed and discovery feed
- like, comment, save, and share
- search by dish, restaurant, and location
- follow other users
- block, mute, and basic profile/content visibility controls
- AI-assisted food recommendation with deterministic fallback
- moderation and reporting
- notification center for follow and interaction activity
- responsive web experience
- AI-assisted user discovery and decision support
- practical nearby and area-aware food/place discovery
- production media storage, derivative, delivery, deletion, and cleanup expectations
- product analytics events needed to evaluate activation, retention, search, recommendation, and moderation volume
- operational readiness for identity, data, media, observability, alerts, backup, restore, rollback, and incident response

Detailed product behavior for in-scope capabilities lives in `04_functional_requirements.md`.

Technical implementation direction for AI, search, media lifecycle, and rollout safety lives in `10_architecture.md` and `05_non_functional_requirements.md`.

Maturity staging from beta to real product scale lives in `16_maturity_roadmap.md`.

## Not Required for First Public Release

- direct food ordering with delivery partners
- deep restaurant partner portal (Assumption)
- advanced creator monetization (Assumption)
- complex ad platform (Assumption)
- deep internal multi-agent product behavior
- heavyweight dedicated search infrastructure before measured PostgreSQL-first relevance or scale limits
- vector-search infrastructure before semantic retrieval has a proven product need
- platform-wide cache dependency before measured bottlenecks justify it
- heavyweight experimentation platform
- native mobile apps unless web retention or distribution needs justify earlier investment
- chat/direct messaging unless product learning proves it is part of the core food discovery loop
- advanced real-time presence, typing indicators, and read receipts
- video-first creation and transcoding-heavy media workflows unless usage proves they matter

## Product Release Boundary

FoodSocial should be treated as a real product with clear user value, social safety, operational ownership, and staged maturity.

Given the current delivery context, the product must avoid unnecessary mature-social infrastructure until evidence justifies it, but it must not omit the controls a real social product needs for trust, recovery, moderation, and public operation.

Delivery framing lives in `00_project_overview.md`.

Risk detail lives in `14_risks_open_questions.md` and `10_architecture.md`.

## Staged Product Expansion

- native mobile apps
- food delivery / ordering integration
- richer AI personalization
- internal AI agents for moderation, curation, or concierge workflows
- partner ecosystem for restaurants or delivery platforms
- advanced trust and anti-seeding systems
- dedicated feed, search, notification, and moderation services when load, complexity, or ownership justifies extraction
- dedicated realtime gateway if notifications, comments, or future chat require live delivery

## Need Confirmation

- public-launch user cap, support model, and incident owner
- exact threshold for introducing dedicated search, vector search, or heavier feed infrastructure
- exact threshold for mobile app investment

## Recipe Scope Direction

- Recipe is included as a structured post type with ingredients, steps, and media
- Recipe is not yet treated as a deep standalone module unless product usage justifies deeper recipe workflows

## Product Constraints to Preserve

- Search remains PostgreSQL-first until evidence justifies dedicated search.
- Caching remains selective and evidence-driven.
- Moderation remains manual-review-first for content actions until automation has review, appeal, audit, and rollback controls.
- Feature rollout safety remains practical and operator-owned.
- Service extraction follows maturity gates instead of speculative service count growth.

Canonical technical detail for these constraints lives in `05_non_functional_requirements.md` and `10_architecture.md`.
