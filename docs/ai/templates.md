# templates.md

## Purpose

This file provides short prompt templates for common vibecoding tasks.

## Template 1: Feature implementation

Use when asking an AI agent to implement a new feature.

```md
Implement <feature>.

Goal:
- <user outcome>

Relevant docs:
- <docs/project-discovery/...>

Constraints:
- follow approved MVP direction only
- do not add future-phase features

Validation:
- <how success will be checked>
```

## Template 2: Bug fix

```md
Fix <bug>.

Observed behavior:
- <what is broken>

Expected behavior:
- <what should happen>

Relevant docs:
- <docs/project-discovery/...>

Constraints:
- minimal fix only
- preserve current architecture and workflow decisions
```

## Template 3: API or backend change

```md
Implement or adjust <API/service change>.

Goal:
- <business/system outcome>

Relevant docs:
- 09_api_and_integration_design.md
- 10_architecture.md
- 12_backend_structure.md
- 08_database_design.md

Constraints:
- preserve BFF model
- preserve Hybrid REST + async events
- preserve service-owned PostgreSQL boundaries
```

## Template 4: Frontend/UI change

```md
Implement or refine <UI feature>.

Goal:
- <user outcome>

Relevant docs:
- 11_frontend_structure.md
- 06_user_flows.md
- 04_functional_requirements.md

Constraints:
- keep UX aligned with approved MVP behavior
- do not invent extra product scope
```
