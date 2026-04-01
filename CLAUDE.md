# CLAUDE.md

Read `AGENTS.md` first.

This file is a thin Claude-specific adapter, not a second source of truth.

## Context Loading Order

1. `AGENTS.md`
2. `docs/ai/repo-map.md`
3. relevant `docs/project-discovery/*.md`
4. `docs/ai/workflows.md`
5. `docs/ai/templates.md` if needed

## Claude Usage Notes

- Follow the repo contract defined in `AGENTS.md`
- Treat `docs/project-discovery/` as the factual product and architecture layer
- Use `docs/ai/workflows.md` for common execution patterns
- Keep changes small, explicit, and aligned with the approved MVP direction
