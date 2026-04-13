# Archived OpenSpec Change: add-authentication-ui

## Why

The platform already supported Keycloak-backed browser authentication through the BFF, but the Angular frontend had no user-facing login or logout entry points. A first auth UI surface was needed so guests could discover how to sign in and authenticated users could exit their session without relying on backend URLs.

## What Changed

- Add frontend authentication UI entry points for login and logout in the Angular web app.
- Add guest-versus-authenticated session presentation so the shell can show the right auth actions.
- Define the initial BFF-backed browser session checks the frontend uses to decide whether to show login or logout affordances.
- Keep the existing authentication architecture unchanged by reusing the current BFF and Keycloak flow instead of introducing direct frontend identity handling.

## Capability

- `authentication-ui`: user-facing login/logout entry points and session-aware auth controls for the web frontend.

## Historical Note

This OpenSpec change is archived for reference. The repo’s active operating rules and current implementation state live under `docs/`.
