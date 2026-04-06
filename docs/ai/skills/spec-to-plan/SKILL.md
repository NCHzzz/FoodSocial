---
name: spec-to-plan
description: Use this skill when an approved product or architecture direction needs to be converted into an implementation-ready plan.
---

# Spec to Plan

## Purpose

Translate discovery docs into an actionable implementation plan.

Write the resulting plan into `docs/delivery/plans/` when the work is non-trivial and the repository uses delivery artifacts.

## Workflow

1. Read the relevant product-discovery files
2. Identify user outcome, system outcome, and constraints
3. Break the work into implementation slices
4. List dependencies and risks
5. Keep the plan aligned to current MVP boundaries
6. Store the plan as a delivery artifact rather than project-state context

## Output Shape

- short goal
- affected areas
- ordered implementation steps
- validation approach
- open risks or unresolved dependencies
- delivery plan path under `docs/delivery/plans/`
