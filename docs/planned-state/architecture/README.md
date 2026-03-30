---
title: FoodSocial MVP Solution Architecture
status: planned-state
---

# FoodSocial MVP Solution Architecture

This document set describes the **planned MVP architecture** for FoodSocial.

It is based on the current repository truth that:

- `Backend/README.md` defines a **non-runnable** .NET 10 microservices-oriented scaffold.
- `Frontend/README.md` defines a **non-runnable** Angular app-shell scaffold.

Nothing in this folder should be read as proof that the system is already implemented. These files describe the **target MVP shape** that the team can use to align on scope, structure, and delivery order.

## MVP goals

The MVP should prove one core product loop:

1. A user shares a food post with image and location.
2. Another user discovers nearby dishes that match their needs.
3. Users interact with posts through lightweight social actions.
4. AI helps users express what they want to eat, using FoodSocial post data as the grounding source.

## Non-goals for this phase

The MVP architecture intentionally avoids the following on day 1:

- production-grade distributed microservices
- Elasticsearch or vector search infrastructure
- advanced recommendation engines
- complex moderation pipelines
- messaging, follow graph, or deep social-network features
- autonomous AI workflows

## Planned solution at a glance

The recommended MVP architecture is:

- **Angular frontend** for the web experience
- **one .NET 10 backend deployable** organized as a modular monolith
- **PostgreSQL** as the system of record
- **PostGIS** for nearby discovery queries
- **object storage** for food images

This keeps implementation fast while preserving clean seams for future service extraction.

## Architecture principles

### 1. Build the product loop before the platform

The first version should solve food posting, nearby discovery, and lightweight interaction before introducing complex distributed infrastructure.

### 2. Stay microservice-ready without paying the cost too early

The backend should be organized around domain modules that mirror future service boundaries, but the MVP should still run as a single backend deployable.

### 3. Treat AI as an assistant, not the source of truth

AI should help translate natural-language intent into FoodSocial discovery filters. It should not replace the core discovery model or invent unsupported recommendations.

### 4. Keep planned-state and current-state separate

These documents describe intended architecture. Current implementation state should always be verified from repository evidence.

## Document map

- [`containers-and-modules.md`](./containers-and-modules.md) — planned system topology, module boundaries, and data ownership
- [`key-flows.md`](./key-flows.md) — expected MVP user and system flows
- [`evolution-path.md`](./evolution-path.md) — phased path from scaffold to MVP and later service extraction
- [`api-contracts.md`](./api-contracts.md) — planned MVP API surface, request and response shapes, and error conventions
- [`database-schema.md`](./database-schema.md) — planned relational schema, entity ownership, and indexing guidance

## Assumptions

- Stack target remains `.NET 10`, `Angular latest`, `TailwindCSS`, and `PostgreSQL`.
- FoodSocial is optimized first for the experience of finding something good to eat nearby.
- Social features exist to strengthen engagement, not to dominate the first release.
- AI suggestions are grounded in FoodSocial post data, not in a general external food knowledge graph.

## Open questions

- Should the first release prioritize a mobile-web experience only, or support desktop equally from day 1?
- Will posts represent only restaurants/vendors, or also personal recommendations for dishes at specific places?
- Is image upload synchronous in the create-post flow, or should processing be deferred after submit?
- Should the first AI experience be exposed as a dedicated page, a smart search box, or both?

## Sources

- `Backend/README.md`
- `Frontend/README.md`
