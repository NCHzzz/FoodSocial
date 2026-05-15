# repo-map.md

## Purpose

This file tells agents where truth, rules, state, and change workflow live.

This file is a directory map and canonical-file pointer only.

For canonical loading order and task-type loading rules, use:

- `AGENTS.md`
- `docs/ai/workflows.md`

## Top-Level Map

- `Backend/` — .NET services and platform code
- `Frontend/` — Angular frontend code
- `docs/project-discovery/` — product and architecture truth
- `docs/ai/` — agent operating rules
- `docs/context/` — current repo state
- `docs/delivery/` — curated markdown delivery records when they are worth keeping outside OpenSpec
- `openspec/` — active structured change workflow for `/opsx-*`
- `AGENTS.md` — root agent contract

## Rule Files

- `docs/ai/engineering-rules.md` — repo-wide engineering rules
- `docs/ai/stack-rules/dotnet.md` — backend rules
- `docs/ai/stack-rules/angular-tailwind.md` — frontend engineering rules
- root `DESIGN.md` — frontend UI/UX rules

## Task-Type Detail

For task-type loading rules, use `docs/ai/workflows.md`.

Do not stop at this directory map. After locating the relevant rule files, explicitly load them before implementation.

Use this file only to understand **where** each class of document lives.

## Layer Summary

- `docs/project-discovery/` = truth
- `docs/ai/` = rules
- `docs/context/` = current state
- `openspec/` = active change workflow
- `docs/delivery/` = optional durable markdown records

## Canonical Current-State Files

- `docs/context/current_build_status.md` — current implementation snapshot
- `docs/context/product_launch_readiness.md` — environment-owned launch readiness gates
- `docs/context/progress_log.md` — recent milestones only
