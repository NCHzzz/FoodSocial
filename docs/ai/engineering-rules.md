# engineering-rules.md

## Purpose

This file defines repository-wide engineering expectations that go beyond generic clean code.

## Universal Engineering Rules

- Prefer explicit, readable, maintainable code over clever abstractions
- Keep functions, classes, and modules focused on one clear responsibility
- Preserve clear boundaries between layers and responsibilities; stack-specific boundary rules live in the relevant files under `docs/ai/stack-rules/`
- Avoid duplicated business logic across services, frontend flows, and infrastructure layers
- Fail fast on invalid inputs, invalid configuration, and invalid state transitions
- Prefer deterministic behavior and graceful degradation over hidden fallback magic
- Make side effects obvious in naming and control flow
- Standardize logging, observability, and error handling rather than reinventing them per feature
- Design code so follow-up AI agents can understand intent from local context quickly

## Professional Standards

- Preserve contract-first boundaries for APIs, events, and service responsibilities
- Keep security, authorization, and tenant isolation explicit rather than implicit
- Prefer additive evolution of contracts over silent breaking changes
- Build for debuggability and operability; stack-specific mechanics stay in the relevant stack rules files

## Performance Rules

- Keep non-critical work off critical user paths when possible
- Optimize measurable bottlenecks, not imagined ones
- Preserve usability when AI-dependent or non-critical downstream features are degraded or unavailable

Stack-specific performance, pagination, caching, projection, deferred-loading, and rendering rules belong in the relevant files under `docs/ai/stack-rules/`.

## Usage Rule

- Use this file for global engineering discipline only
- Put stack-specific implementation rules in `docs/ai/stack-rules/`
- Keep product and architecture truth in `docs/project-discovery/`
