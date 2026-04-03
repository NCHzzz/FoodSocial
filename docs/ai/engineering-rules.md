# engineering-rules.md

## Purpose

This file defines repository-wide engineering expectations that go beyond generic clean code.

## Universal Engineering Rules

- Prefer explicit, readable, maintainable code over clever abstractions
- Keep functions, classes, and modules focused on one clear responsibility
- Preserve clear boundaries between UI, transport, application logic, persistence, and integration code
- Avoid duplicated business logic across services, frontend flows, and infrastructure layers
- Fail fast on invalid inputs, invalid configuration, and invalid state transitions
- Prefer deterministic behavior and graceful degradation over hidden fallback magic
- Make side effects obvious in naming and control flow
- Validate external input at the boundary before it reaches business logic
- Standardize logging, observability, and error handling rather than reinventing them per feature
- Design code so follow-up AI agents can understand intent from local context quickly

## Professional Standards

- Preserve contract-first boundaries for APIs, events, and service responsibilities
- Keep security, authorization, and tenant isolation explicit rather than implicit
- Prefer additive evolution of contracts over silent breaking changes
- Build for debuggability: correlation, logging, and traceability are part of the implementation quality bar
- Build for operability: health, readiness, timeouts, retries, and fallback behavior are engineering concerns, not later polish

## Performance Rules

- Keep non-critical work off critical user paths when possible
- Optimize measurable bottlenecks, not imagined ones
- Keep payloads, queries, and rendering work proportional to the actual user need
- Treat pagination, caching, projections, and deferred loading as standard tools, not special cases
- Preserve usability when AI-dependent or non-critical downstream features are degraded or unavailable

## Usage Rule

- Use this file for global engineering discipline only
- Put stack-specific implementation rules in `docs/ai/stack-rules/`
- Keep product and architecture truth in `docs/project-discovery/`
