# repo-map.md

## Purpose

This file tells agents where truth, rules, state, and code live.

This file is a directory map and canonical-file pointer only.

For the canonical loading order, see `AGENTS.md`.
For task-type loading rules, see `docs/ai/workflows.md`.

## Top-Level Map

- `Backend/` — .NET services and platform code
- `Frontend/` — Angular frontend code
- `docs/project-discovery/` — product and architecture truth
- `docs/ai/` — agent operating rules
- `docs/context/` — current repo state
- `docs/delivery/` — curated markdown delivery records
- `AGENTS.md` — root agent contract and canonical load order
- `CLAUDE.md` — thin Claude adapter, redirects to `AGENTS.md`
- `DESIGN.md` — canonical UI/UX design system (lives at root because it governs both frontend code and product-wide visual identity)
- `Backend/MIGRATIONS.md` — EF Core migration rules, service map, and commands

## Rule Files

- `docs/ai/engineering-rules.md` — repo-wide engineering rules and rule-level definitions
- `docs/ai/git-rules.md` — version control rules (commit, pull, push)
- `docs/ai/stack-rules/dotnet.md` — backend rules
- `docs/ai/stack-rules/angular-tailwind.md` — frontend engineering rules
- root `DESIGN.md` — frontend UI/UX design system

## Layer Summary

- `docs/project-discovery/` = truth
- `docs/ai/` = rules
- `docs/context/` = current state
- `docs/delivery/` = optional durable markdown records

## Canonical Current-State Files

- `docs/context/current_build_status.md` — current implementation snapshot
- `docs/context/product_launch_readiness.md` — environment-owned launch readiness gates
- `docs/context/progress_log.md` — recent milestones only

## Task-Type Detail

For task-type loading rules, see `docs/ai/workflows.md`.

Do not stop at this directory map. After locating the relevant rule files, explicitly load them before implementation.

Use this file only to understand **where** each class of document lives.
