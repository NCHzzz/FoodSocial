# workflows.md

## Purpose

This file defines the default execution flow for agents in this repo.

## Global Load Order

1. `AGENTS.md`
2. `docs/ai/repo-map.md`
3. relevant `docs/project-discovery/` files
4. `docs/context/current_build_status.md` when current state matters
5. `docs/context/progress_log.md` when recent history matters
6. active OpenSpec change in `openspec/changes/` when the task is driven through `/opsx-*`
7. `docs/delivery/` only when curated markdown records already exist and still matter

## Global Rules

- load truth before implementation
- make task scope explicit before editing
- keep current state in `docs/context/`
- keep product truth in `docs/project-discovery/`
- keep agent rules in `docs/ai/`
- keep active `/opsx-*` change workflow in `openspec/`
- keep `docs/delivery/` for curated markdown records only
- do not duplicate the same live work across `openspec/` and `docs/delivery/`
- verify before claiming completion

## Task-Type Rule Loading

### Backend/API work

Always load:

- `docs/ai/engineering-rules.md`
- `docs/ai/stack-rules/dotnet.md`

These files must be explicitly read before backend implementation starts. Reading `AGENTS.md` or `repo-map.md` alone is not sufficient.

### Frontend work

Always load:

- `docs/ai/engineering-rules.md`
- `docs/ai/stack-rules/angular-tailwind.md`

These files must be explicitly read before frontend implementation starts. Reading `AGENTS.md` or `repo-map.md` alone is not sufficient.

### UI work

Always load:

- `docs/ai/engineering-rules.md`
- `docs/ai/stack-rules/angular-tailwind.md`
- `docs/ai/stack-rules/frontend-ui-unification.md`

These files must be explicitly read before UI implementation starts. Reading `AGENTS.md` or `repo-map.md` alone is not sufficient.

## Active Change Workflow Rule

For non-trivial work driven by `/opsx-*`, OpenSpec is the active workflow home.

- proposals, design, tasks, and change state stay in `openspec/`
- durable markdown records go in `docs/delivery/` only when they add value outside OpenSpec

## Repo/Docs Update Rule

When updating repo workflow/docs:

- prefer fewer canonical files
- do not restate the same rule in multiple places unless the shorter file points clearly to the canonical one
- keep the system understandable by task type: backend, frontend, UI, or OpenSpec-driven change
