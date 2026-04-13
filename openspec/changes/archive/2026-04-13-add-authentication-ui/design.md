# Archived OpenSpec Design: add-authentication-ui

## Intent

Introduce the first frontend auth surface without changing the underlying identity architecture.

## Core Design Decisions

1. Put auth controls in the shared application shell.
2. Resolve frontend auth state through the BFF session endpoint.
3. Keep login as browser navigation and logout as explicit POST via the BFF.
4. Use a small shared frontend auth-state service.

## Scope Boundary

- No direct OIDC/token handling in the Angular app.
- No signup or account-management expansion.
- No backend auth architecture changes.

## Historical Note

This file is retained as archived OpenSpec rationale only. Current repo guidance lives under `docs/ai/` and current implementation state lives under `docs/context/`.
