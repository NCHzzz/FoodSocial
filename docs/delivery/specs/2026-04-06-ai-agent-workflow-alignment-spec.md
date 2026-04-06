# AI Agent Workflow Alignment Specification

> Status: approved design direction pending implementation
> Date: 2026-04-06
> Scope: repository workflow and documentation structure

## Goal

Align this repository's AI-agent workflow with the core methodology of `obra/superpowers` while preserving FoodSocial's existing source-of-truth model and improving clarity, maintainability, and cleanup discipline.

## Non-Goals

This change does not:

- redefine product or architecture truth in `docs/project-discovery/`
- introduce a full copy of the `superpowers` repository structure
- add mandatory `docs/superpowers/` paths
- change business scope for MVP features
- require git commits as part of the workflow itself

## Design Principles

This repository should adopt the following `superpowers`-aligned principles:

1. spec before implementation for non-trivial work
2. plan before execution for approved work
3. verification before completion claims
4. small, explicit, reviewable work units
5. evidence over assumptions
6. repository state and delivery artifacts must be stored separately

This repository should not blindly adopt `superpowers` project-specific conventions such as:

- its exact folder layout
- its contributor voice and phrasing
- its plugin-specific assumptions
- any path conventions that conflict with FoodSocial's existing documentation layers

## Canonical Documentation Model

### 1. Product and architecture truth

Path:

- `docs/project-discovery/`

Responsibility:

- approved product direction
- domain truth
- architecture truth
- API and backlog truth

Rules:

- use for durable truth only
- do not store working plans or short-lived implementation notes here

### 2. AI operating guidance

Path:

- `docs/ai/`

Responsibility:

- agent workflow rules
- execution patterns
- stack rules
- reusable local skills

Rules:

- use for repository operating instructions
- do not store project-state snapshots here
- do not store one-off implementation plans here

### 3. Project context

Path:

- `docs/context/`

Responsibility:

- current build and setup state
- active blockers
- milestone-level progress
- current operational reality

Rules:

- context must describe what is true now
- context must not become a container for implementation plans
- context may mention the current recommended priority, but detailed execution artifacts belong elsewhere
- `current_build_status.md` remains an overwrite-style snapshot
- `progress_log.md` remains milestone-only and append-oriented

### 4. Delivery artifacts

Path:

- `docs/delivery/specs/`
- `docs/delivery/plans/`
- `docs/delivery/archive/`

Responsibility:

- specs = approved design artifacts for upcoming work
- plans = execution-ready implementation plans derived from approved specs
- archive = superseded, completed, cancelled, or historical delivery artifacts that should remain referenceable without cluttering active folders

Rules:

- delivery artifacts are not source-of-truth architecture docs
- delivery artifacts are not project-state context docs
- active work should be easy to identify without scanning old files
- old artifacts must be archived or removed instead of accumulating indefinitely

## Required Workflow Shape

For non-trivial work, the expected repository workflow becomes:

1. load context from `AGENTS.md`, `docs/ai/repo-map.md`, relevant discovery docs, relevant stack rules, and current context docs when needed
2. clarify the task and its scope
3. create or update a design spec in `docs/delivery/specs/` when the work is not trivial
4. obtain approval on the design direction before implementation
5. create an implementation plan in `docs/delivery/plans/`
6. execute the work with validation and review discipline
7. update `docs/context/` to reflect the new current state
8. archive or retire delivery artifacts when they are no longer active

For trivial work, the agent may skip spec and plan artifacts if the change is obvious, small, and safely verifiable.

## Delivery Artifact Lifecycle

### Spec lifecycle

Each spec should clearly indicate one of the following states:

- draft
- approved
- superseded
- cancelled
- implemented

Behavior:

- keep only active and relevant specs in `docs/delivery/specs/`
- when a spec is replaced, either mark it superseded and archive it or remove it if it has no historical value

### Plan lifecycle

Each plan should clearly indicate one of the following states:

- draft
- active
- blocked
- completed
- cancelled
- superseded

Behavior:

- plans belong in `docs/delivery/plans/` while active
- completed or superseded plans should be archived to reduce noise
- duplicate or abandoned plan drafts may be deleted if they no longer provide reference value

## Cleanup and Archival Policy

The repository should avoid uncontrolled markdown growth.

### Archive rules

Archive when:

- a spec or plan has historical value
- a completed plan may be useful for future auditing or follow-up
- a superseded design explains why a newer direction exists

Delete when:

- the file is a duplicate draft
- the file was created from workflow confusion and has no lasting value
- the file does not add truth, context, or useful execution history

### Visibility rules

- active specs and plans should be visible in their active folders only
- archived files should move under `docs/delivery/archive/`
- archive structure should remain easy to navigate by artifact type

## Naming Convention

### Specs

- `docs/delivery/specs/YYYY-MM-DD-<topic>-spec.md`

### Plans

- `docs/delivery/plans/YYYY-MM-DD-<topic>-plan.md`

### Archived artifacts

- preserve filenames where practical
- move them into type-specific archive folders such as:
  - `docs/delivery/archive/specs/`
  - `docs/delivery/archive/plans/`

## Repository Changes Required

Implementation of this specification should include:

1. create the new `docs/delivery/` structure
2. update `AGENTS.md` so the documentation model and workflow are explicit
3. update `docs/ai/repo-map.md` so the new layer is navigable
4. update `docs/ai/workflows.md` so specs and plans are routed to `docs/delivery/` and context stays limited to current state
5. update local skills or workflow docs that currently blur context and planning responsibilities
6. move or replace any recently created plan-like context files that do not belong in `docs/context/`
7. add archive guidance so old delivery artifacts do not pile up in active folders

## Migration Rules for Existing Files

During implementation:

- retain valid current-state information in `docs/context/`
- move plan-like files out of `docs/context/` if they are execution artifacts rather than project-state context
- avoid rewriting `docs/project-discovery/` unless durable truth is actually changing
- keep the migration minimal and explicit rather than using a broad doc rewrite

## Success Criteria

This specification is successfully implemented when:

1. the documentation model clearly separates truth, workflow, context, and delivery artifacts
2. `docs/context/` no longer acts as a storage area for plans
3. active specs and plans have clear homes and naming rules
4. workflow docs explicitly describe when to create specs, plans, and context updates
5. archive and cleanup rules prevent stale artifact accumulation
6. a new AI agent can quickly understand where to read current state, where to read truth, and where to store upcoming execution artifacts

## Risks and Mitigations

### Risk: too many markdown files

Mitigation:

- use the delivery layer only for non-trivial work
- archive completed artifacts
- delete duplicate or worthless drafts

### Risk: duplicated truth across layers

Mitigation:

- keep discovery docs as the only durable product and architecture source of truth
- keep delivery docs focused on upcoming or active work
- keep context docs focused on present state only

### Risk: workflow becomes heavier than the team needs

Mitigation:

- allow trivial work to skip spec and plan creation
- keep active folders small and visible
- use the archive instead of letting active directories become cluttered

## Implementation Boundary

The first implementation pass should focus on repository workflow and documentation structure only.

It should not expand into:

- product rewrites
- backend feature implementation
- frontend feature implementation
- unrelated refactors outside the documentation and workflow system
