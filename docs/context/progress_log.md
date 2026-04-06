# progress_log.md

## Purpose

This file records meaningful implementation progress over time.

Use it to append concise progress notes when the repository materially changes.

## Update Rule

Append an entry when any of the following happens:

- a new service, app, or major module is created
- a major architectural decision is implemented in code
- a vertical slice becomes runnable end to end
- local orchestration or infrastructure setup changes materially
- a major feature moves from planned to implemented

Do not append minor churn, repetitive edits, or every small refactor.

This file is for milestone-level progress only.

## Size Control Rule

- keep only recent and still-useful milestones in this file
- when the log becomes large, move older entries into `docs/context/archive/` using archive files such as `progress_log.2026-Q2.md`
- do not load full historical progress by default when current implementation state is enough

## Entry Format

```md
## YYYY-MM-DD

- What changed
- Why it matters
- What is unblocked next
```

## 2026-04-06

- Added a dedicated delivery-spec artifact for AI-agent workflow alignment.
- Clarified the repository direction toward separating current-state context from delivery artifacts.
- This unblocks migration of plan-like notes out of `docs/context/`.
