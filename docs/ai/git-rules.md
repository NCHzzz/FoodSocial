# git-rules.md

## Purpose

This file defines the repository rules for Git operations (commit, pull, push) to maintain a clean, readable, and safe version history.

For rule-level definitions (MUST / SHOULD / MAY), see `docs/ai/engineering-rules.md`.

## Commit Rules

- Commits **MUST** use the Conventional Commits format: `type(scope): subject`.
- `type` **MUST** be one of: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`.
- `scope` **SHOULD** reflect the area changed (e.g., `backend`, `frontend`, `auth`, `ui`, `ops`).
- `subject` **MUST** be concise, written in the imperative mood, and lowercase (e.g., "add feature", not "Added feature").
- Commits **MUST** be atomic. Do not mix unrelated changes (e.g., fixing a bug and refactoring unrelated code) in a single commit.
- Commit messages **SHOULD** explain the "why" in the body if the "what" is complex or non-obvious.

## Pull Rules

- Pulls **SHOULD** use `git pull --rebase` to avoid unnecessary merge commits and keep a linear history.
- The local working tree **MUST** be clean (commit or stash changes) before pulling to prevent accidental conflict messes.
- If conflicts occur, resolve them carefully, keeping the canonical product/architecture truth intact.

## Push Rules

- Code **MUST** build and tests **MUST** pass locally before pushing (see `docs/context/product_launch_readiness.md` for verification commands).
- Developers **MUST NOT** push secrets, API keys, or `.env` files. Ensure `.gitignore` is respected.
- Developers **SHOULD** push small, incremental updates rather than massive monolithic pull requests.

## AI Agent Specific Rules

- Before creating a commit, AI agents **MUST** review the git status and diff to ensure only intended files are staged.
- AI agents **MUST NOT** auto-push to a remote branch without explicit user approval.
- AI agents **SHOULD** summarize the commit message clearly to the user after committing.
