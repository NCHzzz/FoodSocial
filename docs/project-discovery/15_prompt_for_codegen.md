# 15_prompt_for_codegen.md

## Status

- Working draft
- Not ready for final use yet

## Draft Prompt for AI Code Generation

Build a responsive web-first AI-native food-focused social media platform.

Implement the approved MVP scope from `03_scope_mvp.md` and `04_functional_requirements.md`.

Use `AGENTS.md`, `docs/ai/repo-map.md`, and the approved project-discovery documents as source material.

This file is an implementation bootstrap helper, not primary product truth.

## Stack

Canonical stack and architecture truth live in `10_architecture.md`.

## Coding Conventions

Use `AGENTS.md`, `docs/ai/workflows.md`, `docs/ai/engineering-rules.md`, and the relevant stack-rule files as the canonical implementation-rule sources.

## Files to Generate First

- Aspire AppHost and shared service defaults
- Angular shell and route structure
- identity/auth service skeleton
- posts/media service skeleton
- dish service skeleton
- place service skeleton
- search/recommendation service skeleton
- notification service skeleton
- Python AI capability service skeleton for query understanding or enrichment
- moderation/trust service skeleton

## Suggested Build Order

Suggested build order follows the same intent as the file-generation list above; keep only one sequencing source when this helper is used.

## Need Confirmation

- recommendation implementation scope details beyond the currently approved direction

Approved monetization, recommendation, and agent-direction truth should be read from the canonical discovery and architecture docs instead of being restated here.
