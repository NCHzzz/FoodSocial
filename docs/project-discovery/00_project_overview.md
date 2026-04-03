# 00_project_overview.md

## Status

- Working draft
- Documentation language: English only
- Interview language may be Vietnamese

## Project Summary

The product is a food-focused social media platform for users to share and discover food-related content.

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
- Build a foundation for future AI-based food recommendations
- Position the product as an AI-native food social platform
- Validate that a small group of real users uses the product repeatedly and finds it useful

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

## MVP Summary

The current MVP direction includes all of the following capabilities:

- create posts about food, restaurants, and recipes
- discovery feed
- like, comment, save, and share interactions
- search by dish, restaurant, and location
- user profile
- follow other users
- AI-based food recommendation
- moderation and reporting

## Content Strategy Direction

- Product direction: Hybrid content model
- Primary content strategy: Food-first
- Secondary content types: Restaurant / place content and recipe content

The MVP should optimize for the user question "what should I eat?" before deeper place-centric or recipe-centric experiences.

## AI-Native Product Direction

The product should be designed as an AI-native social experience rather than a conventional social product with AI added later.

In MVP terms, this means AI should meaningfully improve:

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

- MVP priority: AI for end users plus controlled agent consumption
- later direction: broader external agent actions with proper authorization and audit controls

## Approved MVP AI Direction

The approved MVP direction is:

- Priority A: AI for end users
- Priority B: AI agents can consume the platform effectively
- Later phase: C, internal AI agents inside the product

This means the MVP should focus on:

- AI-assisted discovery for human users
- agent-friendly APIs and structured platform contracts
- postponing deeper internal agent orchestration until later phases

Long-term agent direction:

- external AI agents may eventually perform broader system actions if properly authorized

## Chosen Technology Stack

- Backend: .NET 10
- AI-oriented services: Python is allowed where model-heavy capabilities justify it
- Service orchestration and local composition: .NET Aspire
- Frontend: Angular
- Data storage: PostgreSQL with ownership per service
- Search approach: PostgreSQL-first search for MVP
- AI provider strategy: support multiple AI providers through an internal abstraction layer
- External identity provider: self-hosted Keycloak
- Deployment direction: Kubernetes-based deployment
- Tenant model: multi-tenant

For MVP implementation, tenant should be treated primarily as an architectural isolation concern rather than a rich product-facing workflow driver.

## Architecture Snapshot for MVP

- Edge pattern: API Gateway / BFF
- Internal communication: Hybrid REST + async events
- Async backbone: RabbitMQ
- Search model: PostgreSQL-first lexical search with structured filtering and typo tolerance
- Geo-discovery model: practical nearby and area-aware retrieval without dedicated geo infrastructure in MVP
- Media model: object-style storage plus explicit derivative and cleanup lifecycle
- AI strategy: provider-agnostic integration with graceful deterministic fallback
- Rollout safety: lightweight runtime feature controls instead of heavyweight experimentation infrastructure
- Deferred by default: vector-first retrieval, heavyweight dedicated search, platform-wide cache dependency

## Platform Direction

- MVP platform strategy: Responsive web first
- Native mobile apps: Phase 2 or later (current direction)

## Delivery Context

- Project type: Personal project
- Delivery mode: Solo build with AI-assisted / vibe coding workflow
- Target timeline: 2 months
- Delivery implication: MVP scope must be carefully constrained even if the feature vision remains broad
- End-of-timeline target: limited real-user beta first, with a path toward production-ready public launch later

## Current Assumptions

- The initial target audience is consumer users, not enterprise users
- The first growth engine is content creation and sharing by food reviewers / food sharers
- Recipe content is included in MVP, but it may not be as deep as the social/discovery layer
- AI recommendation in MVP will use staged implementation rather than advanced ML infrastructure from day one
- Search in MVP will remain PostgreSQL-centered instead of using a heavyweight dedicated search engine
- Multiple AI providers will be supported through a provider-agnostic internal integration layer
- Recommendation priority in MVP is Food-first: dishes first, then places
- Recommendation personalization in MVP will be medium-depth, with a deliberate upgrade path toward stronger personalization later
- Selective caching is sufficient for MVP if explicit ownership and invalidation remain visible per use case
- Vector-search infrastructure is not a default MVP dependency unless semantic retrieval needs become real enough to justify it
- Media lifecycle and deletion behavior must be treated as architecture concerns, not only implementation details

## Need Confirmation

- Exact MVP depth for recommendation heuristics and signal weighting
- Budget constraints

## Launch Scope

- Initial launch market: Vietnam
