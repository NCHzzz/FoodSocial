# 15_prompt_for_codegen.md

## Status

- Working draft
- Not ready for final use yet

## Draft Prompt for AI Code Generation

Build a responsive web-first AI-native food-focused social media platform.

Implement the approved product scope from `03_product_scope.md`, `04_functional_requirements.md`, and `16_maturity_roadmap.md`.

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
- user/profile service skeleton
- dish service skeleton
- place service skeleton
- posts/media service skeleton

Do not generate dedicated Feed, Search/Recommendation, Notification, Moderation/Trust, Media, Analytics, or Python AI services by default. Those are maturity-gated extractions defined in `16_maturity_roadmap.md` and `10_architecture.md`.

## Suggested Build Order

Suggested build order follows the same intent as the file-generation list above. Keep the first implementation inside current launch boundaries unless an approved maturity gate requires extraction.

## Need Confirmation

- recommendation implementation scope details beyond the currently approved direction

Approved monetization, recommendation, and agent-direction truth should be read from the canonical discovery and architecture docs instead of being restated here.
