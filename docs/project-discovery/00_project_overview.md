# 00_project_overview.md

## Status

- Reviewed working source
- Documentation language: English only
- Interview language may be Vietnamese

## Project Summary

FoodSocial is a food-focused social product for people who want to share food experiences, discover dishes and places, and make better everyday eating decisions.

The platform will support content such as:

- food photos and food posts
- restaurant and location recommendations
- personal reviews and experiences
- recipes and cooking-related posts

The long-term direction includes enabling users to order food directly through integrations with food delivery partners.

## Goals

- Create a social platform dedicated only to food and dining content
- Help users decide what to eat and where to eat
- Enable users to share memorable dishes, restaurants, and recipes with others
- Build AI-assisted food recommendations as a core product capability
- Position the product as an AI-native food social platform
- Grow into a dependable public product with repeated real-world usage, clear moderation operations, and staged scale maturity

## Problem Statement

Current platforms do not serve food discovery and food sharing well enough in a focused way.

- General social platforms are noisy and include too much unrelated content
- Google Maps is useful for places, but weak for social sharing and food-first discovery
- TikTok can create inspiration, but it may be distracting and vulnerable to excessive seeding or low-trust content

Users need a dedicated place to:

- ask what is good to eat in a given place
- discover what to eat right now
- share good restaurants and dishes with a relevant audience
- find authentic food-related content in a focused environment

## Stakeholders

### Primary

- Product owner / founder
- End users who share and discover food content

### Future / Secondary

- Admin / moderation team
- Restaurant or food business partners (Need confirmation)
- Food delivery partners for ordering integration in later phases (Assumption)

## Product Release Summary

FoodSocial should be documented and built as a real food-first social product, not as a disposable MVP prototype.

The product baseline must deliver a coherent loop with all of the following capabilities:

- create posts about food, restaurants, and recipes
- following and discovery feeds
- like, comment, save, and share interactions
- search by dish, restaurant, and location
- public profile and account-owned profile management
- follow, block, mute, and basic privacy controls
- AI-assisted food recommendation with deterministic fallback
- moderation and reporting
- notification center
- production operations for identity, media, backup, observability, and incident response

Product maturity is staged in `16_maturity_roadmap.md`.

## Content Strategy Direction

- Product direction: Hybrid content model
- Primary content strategy: Food-first
- Secondary content types: Restaurant / place content and recipe content

The product should optimize for the user question "what should I eat?" before deeper place-centric or recipe-centric experiences.

## AI-Native Product Direction

The product should be designed as an AI-native social experience rather than a conventional social product with AI added later.

For the product baseline, this means AI should meaningfully improve:

- discovery
- search
- recommendation
- food decision support

It also suggests that the platform should be compatible with AI-agent-driven interaction patterns.

## AI Agent Compatibility Direction

Current directional requirement:

- the platform should work well with AI agents

Initial architectural interpretation:

- content should be sufficiently structured for machine understanding
- platform APIs should be agent-friendly
- core entities and actions should be predictable and well-defined
- search and recommendation outputs should be explainable enough for agent consumption

Current clarified direction:

- Product priority: AI for end users plus controlled agent consumption
- Later direction: broader external agent actions with proper authorization, policy, and audit controls

## Approved AI Direction

The approved direction is:

- Priority A: AI for end users
- Priority B: AI agents can consume the platform effectively
- Later phase: C, internal AI agents inside the product

This means the product should focus first on:

- AI-assisted discovery for human users
- agent-friendly APIs and structured platform contracts
- postponing deeper internal agent orchestration until authorization, observability, and operator controls are ready

Long-term agent direction:

- external AI agents may eventually perform broader system actions if properly authorized

## Chosen Technology Stack

- Detailed stack and architecture truth live in `10_architecture.md`.

High-level summary:

- Backend: .NET 10
- Frontend: Angular
- Datastores: PostgreSQL per service
- Identity: self-hosted Keycloak
- Local orchestration: .NET Aspire
- Deployment direction: Kubernetes
- Product search: PostgreSQL-first until real query volume, relevance gaps, or operational needs justify a dedicated search engine

Tenant should be treated primarily as an architectural isolation concern until a real partner or multi-operator business model requires richer product-facing tenant workflows.

## Product Architecture Snapshot

- Canonical architecture truth lives in `10_architecture.md`.

Snapshot:

- Edge pattern: API Gateway / BFF
- Internal communication: Hybrid REST + async events
- Async backbone: RabbitMQ
- Search: PostgreSQL-first, with a staged path to dedicated search if evidence requires it
- AI strategy: provider-agnostic integration with deterministic fallback
- Rollout safety: lightweight runtime feature controls

## Platform Direction

- Product platform strategy: responsive web first, with native mobile staged after the web product proves retention and operational stability
- Native mobile apps: later product phase unless distribution evidence changes the priority

## Delivery Context

- Project type: Personal project evolving toward a real production product
- Delivery mode: Solo build with AI-assisted workflow
- Delivery implication: the product must use maturity gates so the architecture stays real-world capable without introducing every mature social-network subsystem at once
- Current delivery target: production-like beta first, then public launch, then scale maturity as described in `16_maturity_roadmap.md`

## Current Assumptions

- The initial target audience is consumer users, not enterprise users
- The first growth engine is content creation and sharing by food reviewers / food sharers
- Recipe content is included in the launch release, but it may not be as deep as the social/discovery layer
- AI recommendation will use staged implementation rather than advanced ML infrastructure before there are enough signals to justify it
- Search will remain PostgreSQL-centered until dedicated search has a clear relevance, scale, or operations case
- Multiple AI providers will be supported through a provider-agnostic internal integration layer
- Recommendation priority is Food-first: dishes first, then places
- Recommendation personalization starts at medium depth, with a deliberate upgrade path toward stronger personalization later
- Selective caching is sufficient until feed, catalog, or recommendation hotspots are measured
- Vector-search infrastructure is not a default dependency unless semantic retrieval needs become real enough to justify it
- Media lifecycle and deletion behavior must be treated as architecture concerns, not only implementation details
- Block, mute, privacy controls, and abuse operations are required for a real social product even if the first implementation remains simple

## Need Confirmation

- Production budget constraints
- Public-launch user cap and operating model
- Exact threshold for dedicated feed, search, notification, or moderation service extraction

## Product Scope

- Initial market: Vietnam
- Product class: food-first social platform
- Release strategy: production-like beta, public launch, then scale maturity
