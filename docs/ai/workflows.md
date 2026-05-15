# workflows.md

## Purpose

This file defines the task-type execution rules and recovery guidance for agents in this repo.

For the canonical source-read order, see `AGENTS.md`.

## Task-Type Rule Loading

Before implementation, agents MUST load the rule files for the current task type. Reading `AGENTS.md` or `repo-map.md` alone is not sufficient.

### Backend / API work

Load:

- `docs/ai/engineering-rules.md`
- `docs/ai/stack-rules/dotnet.md`

### Frontend work

Load:

- `docs/ai/engineering-rules.md`
- `docs/ai/stack-rules/angular-tailwind.md`

### UI / visual work

Load:

- `docs/ai/engineering-rules.md`
- `docs/ai/stack-rules/angular-tailwind.md`
- root `DESIGN.md`

### Full-stack work

Load:

- `docs/ai/engineering-rules.md`
- `docs/ai/stack-rules/dotnet.md`
- `docs/ai/stack-rules/angular-tailwind.md`
- root `DESIGN.md` when the task includes user-facing UI changes

### DevOps / infrastructure work

Load:

- `docs/ai/engineering-rules.md`
- `docs/context/current_build_status.md` for current runtime shape
- `docs/delivery/ops-production-runbook.md` when modifying ops assets

### Database migration work

Load:

- `docs/ai/engineering-rules.md`
- `docs/ai/stack-rules/dotnet.md`
- `Backend/MIGRATIONS.md`

### Documentation work

Load:

- `docs/ai/engineering-rules.md`

Rules for documentation changes:

- do not invent product truth; cite or update `docs/project-discovery/`
- keep markdown in English
- keep current state in `docs/context/`, not in rule or discovery files
- do not duplicate content across files; point to the canonical source

### Version Control / Git work

Load:

- `docs/ai/engineering-rules.md`
- `docs/ai/git-rules.md`

### Architecture / design work

Load:

- `docs/ai/engineering-rules.md`
- relevant `docs/project-discovery/` files for current architecture truth
- `docs/ai/stack-rules/dotnet.md` when the design affects backend boundaries
- `docs/ai/stack-rules/angular-tailwind.md` when the design affects frontend structure

## Ambiguous Task Routing

When the task type is unclear:

- If the task touches both backend and frontend, treat it as full-stack
- If the task is about fixing a user-visible bug, load both backend and frontend rules plus `DESIGN.md`
- If the task involves committing, pulling, or pushing code, load `git-rules.md`
- If the task is about CI/CD, Helm, Docker, or Kubernetes, treat it as DevOps
- If the task involves EF Core migrations or schema changes, treat it as database migration work
- When genuinely uncertain, load `engineering-rules.md` and ask the user for clarification before loading stack-specific rules

## Global Rules

- load truth before implementation
- make task scope explicit before editing
- verify before claiming completion
- keep current state in `docs/context/`
- keep product truth in `docs/project-discovery/`
- keep agent rules in `docs/ai/`

## When Things Go Wrong

### A referenced file does not exist

- Skip it and note in your response that the file was missing
- Do not invent content to fill the gap
- If the missing file is critical to the task, ask the user before proceeding

### Build fails after implementation

- Read the error output carefully before attempting fixes
- Fix one error at a time, starting from the first reported error
- Do not introduce speculative fixes for errors you have not seen
- If the build failure is unrelated to your changes, note it and ask the user

### Tests fail after implementation

- Determine whether the failure is caused by your change or was pre-existing
- Fix failures caused by your change before claiming completion
- Do not silently delete or skip failing tests
- If a pre-existing test failure blocks your work, report it and ask the user

### Two rules conflict

- `AGENTS.md` priority chain applies: user instructions > repo workflow guidance > assistant defaults
- Between rule files at the same level, prefer the more specific file (stack rules over engineering rules)
- If the conflict is genuine and unresolvable, document both rules and ask the user

### Discovery docs appear outdated vs actual code

- Treat `docs/project-discovery/` as intended truth unless the user explicitly says otherwise
- Do not silently update discovery docs to match code drift
- Report the discrepancy to the user with specific file references
