# 15_prompt_for_codegen.md

## Status

- Working draft
- Not ready for final use yet

## Draft Prompt for AI Code Generation

Build a responsive web-first AI-native food-focused social media platform.

The platform must support:

- user accounts and profiles
- posting food content, restaurant/location content, and recipe content
- discovery feed
- likes, comments, saves, and shares
- search by dish, restaurant, and location
- following users
- AI-based food recommendation
- moderation and reporting

Use the project documentation files in this folder as the source of truth.

## Stack

- Frontend: Angular
- Backend: .NET 10 microservices
- Local orchestration/composition: .NET Aspire
- Datastores: PostgreSQL with ownership per service
- Search: PostgreSQL-first search using full-text search, trigram similarity, and structured filters
- AI integration: multiple AI providers behind an internal provider-agnostic abstraction layer
- Authentication: self-hosted Keycloak as external identity provider
- Frontend API style: API Gateway / BFF
- Internal integration style: Hybrid REST + async events
- Deployment: Kubernetes-based deployment target
- Tenant model: multi-tenant
- Async messaging: RabbitMQ

## Coding Conventions

- Keep service boundaries explicit and contract-first
- Do not use shared cross-service database access
- Prefer deterministic business logic with AI assisting specific workflows rather than replacing core control flow
- Design graceful fallback behavior for AI-assisted features
- Keep Angular frontend feature-oriented and API contracts stable

## Files to Generate First

- Aspire AppHost and shared service defaults
- Angular shell and route structure
- identity/auth service skeleton
- posts/media service skeleton
- dish service skeleton
- place service skeleton
- search/recommendation service skeleton
- moderation/trust service skeleton

## Suggested Build Order

1. Define shared contracts, service templates, and Aspire composition
2. Implement identity/auth and Angular app shell
3. Implement posts/media plus canonical dish and place services
4. Implement feed and profile flows
5. Implement PostgreSQL-first search and AI-assisted query handling
6. Implement moderation/reporting and basic trust rules
7. Add multi-provider AI routing and fallback hardening

## Need Confirmation

- recommendation implementation scope details beyond the currently approved direction

Monetization direction:

- MVP should prioritize product validation over immediate monetization
- implementation should preserve a path toward future monetization models

Current approved direction:

- recommendation should ship at medium personalization depth in MVP
- implementation should preserve an upgrade path toward stronger personalization later

Agent direction:

- keep APIs and authorization design extensible enough for broader external agent actions later
- MVP agent-facing exposure should remain private or controlled-access rather than public open developer access
