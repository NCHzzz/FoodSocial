---
title: MVP Evolution Path
status: planned-state
---

# MVP Evolution Path

This document describes how FoodSocial can move from the current scaffold-only repository state toward a runnable MVP, and later toward a service-oriented architecture when real pressure appears.

## Starting point today

Current repository evidence shows:

- `Backend/README.md` defines a non-runnable microservices-oriented scaffold.
- `Frontend/README.md` defines a non-runnable Angular app-shell scaffold.

That means the immediate architecture problem is not service scaling. It is establishing the first runnable, product-validating vertical slice.

## Target MVP shape

The target MVP should run as:

- one Angular web frontend
- one .NET 10 backend deployable
- one PostgreSQL database with PostGIS
- one object storage dependency for images

Internally, the backend should still preserve domain seams that match likely future services.

## Phase 1 — Establish the first runnable vertical slice

### Target outcome

Make FoodSocial usable for the first core loop: sign in, create food post, browse nearby posts.

### Recommended sequence

1. Bootstrap Angular app shell and backend API host.
2. Implement Identity basics.
3. Implement Posts and Media upload flow.
4. Implement Discovery nearby feed using PostgreSQL/PostGIS.

### Why this phase matters

It proves whether users can create the supply of content and whether other users can consume it through discovery.

## Phase 2 — Add lightweight social behavior

### Target outcome

Strengthen engagement without expanding into a full social network platform.

### Recommended additions

- likes
- comments
- simple post detail social summary
- initial moderation or reporting hooks if needed

### Architectural posture

- remain within the modular monolith
- keep social data ownership isolated in the Social module

## Phase 3 — Add AI Assist as a grounded experience

### Target outcome

Let users ask for food suggestions naturally while keeping discovery authoritative.

### Recommended additions

- AI Assist endpoint
- prompt construction based on FoodSocial data
- cited result cards instead of unsupported free-form suggestions
- telemetry for AI request cost and quality review

### Architectural posture

- keep AI Assist in-process with backend API initially
- avoid separate vector or orchestration platforms until product usage proves the need

## Phase 4 — Harden operations and extraction seams

### Target outcome

Prepare for growth without extracting services prematurely.

### Recommended additions

- stronger logging and tracing
- background processing for media or AI if latency requires it
- read projections if discovery queries become expensive
- clearer module contracts and outbox-ready event boundaries

## Signals that justify splitting or expanding modules

The MVP should stay as one backend deployable until specific pressure appears.

### Consider extracting a Media worker or service when

- uploads become large or frequent enough to impact API latency
- image transformation pipelines become non-trivial
- storage lifecycle and media processing need separate scaling

### Consider extracting Discovery when

- nearby queries dominate system load
- ranking logic becomes significantly more complex
- discovery indexing or read models need independent scaling

### Consider extracting AI Assist when

- LLM traffic, latency, or cost control becomes operationally distinct
- prompt orchestration evolves independently of the main backend cadence

### Consider broader service decomposition when

- team boundaries demand independent deployment
- release cadence differs substantially by domain
- operational incidents are repeatedly localized to a single domain area

## Decisions intentionally postponed

The following are valid future directions, but should remain deferred until the MVP proves enough value:

- Elasticsearch or hybrid search
- vector search and embeddings
- event bus as a mandatory core dependency
- separate public gateway and multiple backend services
- personalized recommendation pipelines
- advanced trust and moderation systems

## Evolution principle

FoodSocial should not split into microservices because the architecture diagram looks cleaner. It should split only when the product, runtime load, or team structure creates a real operational advantage.

## Sources

- `Backend/README.md`
- `Frontend/README.md`
