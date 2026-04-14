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
- Make ambiguity explicit before implementation when multiple valid interpretations exist
- Prefer present needs over speculative flexibility; do not add abstractions or extension points without a real second use case
- Keep changes surgical and directly traceable to the requested outcome, except cleanup forced by the change itself
- Define verifiable success criteria before editing when behavior, diagnostics, or validation could drift

## Execution Principles

### Think Before Coding

- Do not guess silently when multiple interpretations are plausible.
- Surface assumptions, trade-offs, and uncertainty before locking into an implementation.
- Resolve uncertainty from repo truth first; ask only when the codebase and approved docs cannot answer.
- If a simpler or safer approach exists, state it instead of silently following a weaker plan.

### Simplicity First

- Solve the requested problem with the minimum code that clearly works.
- Do not add configurability, abstraction, or flexibility without a real second use case.
- If a solution feels broader than the problem, simplify it before shipping.

### Surgical Changes

- Touch only the code and docs required by the task.
- Do not opportunistically refactor adjacent code, comments, or formatting without a direct reason.
- Remove only the dead code or drift created by your own change, unless broader cleanup is explicitly requested.

### Goal-Driven Execution

- Translate vague implementation requests into verifiable goals before editing.
- Prefer changes that can be checked by tests, diagnostics, builds, or explicit observable behavior.
- For multi-step work, keep the path from request -> change -> verification obvious.

## Professional Standards

- Preserve contract-first boundaries for APIs, events, and service responsibilities
- Keep security, authorization, and tenant isolation explicit rather than implicit
- Prefer additive evolution of contracts over silent breaking changes
- Build for debuggability and operability; stack-specific mechanics stay in the relevant stack rules files

## Naming Rules

### Purpose

Naming must make production code easier to read, search, review, and maintain.

### Universal naming rules

- Names **MUST** communicate purpose, not implementation accident.
- Names **MUST** be explicit enough that a follow-up engineer or agent can infer intent from local context.
- Names **MUST NOT** be misleading, overloaded, or broader than the thing they represent.
- Names **SHOULD** use domain language where domain meaning matters.
- Names **MUST NOT** leak outdated implementation details after a refactor.

### Core rules

- File names **MUST** match the dominant responsibility of the file.
- Names **MUST NOT** hide responsibility behind vague buckets such as `helpers`, `utils`, `common`, or `misc` when a narrower name exists.
- Function and method names **MUST** describe the action clearly and **MUST NOT** hide meaningful side effects.
- Variable names **MUST** describe the current meaning of the value, not stale earlier context.
- Public APIs, contracts, persisted fields, events, and shared primitives **MUST** use stable, intention-revealing names.
- The broader the scope, the more explicit the name must become.
- Prefer boringly clear names over clever or compressed names.

Stack-specific casing, suffix, and framework-role naming rules belong in the relevant files under `docs/ai/stack-rules/`.

## Performance Rules

- Keep non-critical work off critical user paths when possible
- Optimize measurable bottlenecks, not imagined ones
- Preserve usability when AI-dependent or non-critical downstream features are degraded or unavailable

Stack-specific performance, pagination, caching, projection, deferred-loading, and rendering rules belong in the relevant files under `docs/ai/stack-rules/`.

## Usage Rule

- Use this file for global engineering discipline only
- Put stack-specific implementation rules in `docs/ai/stack-rules/`
- Keep product and architecture truth in `docs/project-discovery/`
