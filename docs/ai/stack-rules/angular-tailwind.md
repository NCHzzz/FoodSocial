# angular-tailwind.md

## Purpose

This file defines frontend engineering rules for the Angular application and TailwindCSS usage in this repository.

This file is the **general frontend implementation rules file**.

It governs:

- Angular architecture and layering
- standalone component and route composition rules
- state, template, diagnostics, and quality rules
- Tailwind usage discipline
- performance, failure-state, and maintainability rules for frontend code
- frontend testing, security, and observability rules

It does **not** define the shared FoodSocial UI/UX design system.

For the active FoodSocial visual system standard, also read:

- root `DESIGN.md`

For rule-level definitions (MUST / SHOULD / MAY), see `docs/ai/engineering-rules.md`.

## Boundary With `DESIGN.md`

Use this file when the question is:

- how should this frontend code be structured?
- where should this Angular logic live?
- how should Tailwind be written safely and consistently?
- how should frontend state, templates, routing, diagnostics, and quality be handled?

Use `DESIGN.md` when the question is:

- what should the UI feel like?
- what is the canonical page pattern?
- how should shared UI/UX behavior remain consistent across the system?
- what visual hierarchy, CTA hierarchy, shell behavior, and content framing should the product use?

## Current Frontend Baseline

- Angular 21+
- workspace rooted directly at `Frontend/`
- standalone bootstrap via `bootstrapApplication(...)`
- standalone components and directives only
- route-first structure under `src/app/`
- `core/`, `shell/`, and lazy `features/` areas inside the app
- TailwindCSS v4 as the primary styling layer
- TypeScript strict mode and Angular strict template diagnostics
- signals for local and view-oriented state
- `provideHttpClient()` and `provideRouter()` in `app.config.ts`
- single-app workspace today, with a future path to `apps/ + libs/` when this repo has multiple deployable Angular apps or real shared frontend libraries

## Angular Architecture Rules

- Prefer standalone, feature-oriented Angular structure
- Keep the Angular workspace rooted at `Frontend/`; do not reintroduce nested one-app workspace folders such as `Frontend/foodsocial-web/`
- Organize app code under `src/app/` using:
  - `core/` for app-wide infrastructure, auth/session services, guards, runtime config, and browser/BFF boundaries
  - `shell/` for layout, navigation, and auth-aware frame composition
  - `features/` for lazy route areas and page-level composition
- Prefer page/route composition in feature folders over overloading `app.ts` or one giant root component
- Use route files per feature area, for example `discover.routes.ts`, `profile.routes.ts`, and `notifications.routes.ts`
- Keep components small and focused on one clear purpose
- Separate orchestration-heavy components from presentational UI when complexity grows
- Keep business logic out of templates and out of purely visual components
- Prefer explicit component APIs using clear inputs and outputs
- Do not regress to NgModule-based app composition, shared module patterns, or `AppModule` bootstrapping
- Apply the Angular 21 setup guide strictly for coding/layering rules now, but treat its full multi-app `apps/ + libs/` workspace shape as future-state guidance until this repo actually has multiple Angular apps or real shared frontend libraries

## Frontend Naming Rules

Follow universal naming semantics in `docs/ai/engineering-rules.md`.

This section adds only frontend-specific naming mechanics and Angular-specific conventions.

### File naming

- Angular and TypeScript file names **MUST** use kebab-case.
- File suffixes **MUST** reflect role clearly, for example:
  - `*.component.ts`
  - `*.directive.ts`
  - `*.service.ts`
  - `*.guard.ts`
  - `*.routes.ts`
- `*.api.ts`
- `*.models.ts`
- `*.token.ts`
- `*.spec.ts`
- Feature files **SHOULD** be named by feature + role, for example `discover.page.ts`, `profile.routes.ts`, `auth-session.service.ts`.

### Type and class naming

- Angular classes, interfaces, and types **MUST** use PascalCase.
- Class names **MUST** match file purpose and suffix role clearly, for example `AuthSessionService`, `UiButtonDirective`, `AppShellComponent`.
- Injection tokens **SHOULD** use clear exported constant names in upper snake or explicit constant style only when that matches existing Angular token usage, for example `BROWSER_WINDOW`.

### Function and variable naming

- Functions, methods, properties, and variables **MUST** use camelCase.
- Observable-like or stream-like values **SHOULD** use names that imply streaming only when they are truly streams.
- Signal values **SHOULD** be named for the state they expose, not for the mechanism used to implement them.

Examples that match this rule:

- `isAuthenticated`
- `hasResults`
- `canSubmit`
- `startLogin`
- `markLogoutPending`
- `loadSession`

### Template naming

- Avoid naming collisions between component fields, computed values, and template loop variables when they represent different concepts.

## Angular 21 Implementation Rules

- Use standalone components only
- Default components to `ChangeDetectionStrategy.OnPush` unless a strong reason exists not to
- Prefer `signal()` for local state, `computed()` for derived state, and `effect()` only for intentional side effects
- Keep constructors light; prefer `ngOnInit()` or explicit methods for async bootstrap work when lifecycle clarity matters
- Prefer route guards and route boundaries for authenticated-only route areas instead of template-only gating
- Keep `app.ts` minimal; it should usually host the root `router-outlet` and very little else
- Keep bootstrap providers obvious in `app.config.ts`
- Prefer lazy `loadChildren` / `loadComponent` for larger route areas
- Angular apps **SHOULD** keep application behavior behind explicit service/client/store boundaries rather than spreading fetch/orchestration logic across pages and shell components

## Angular State and Template Rules

- Prefer signals for local reactive state
- Prefer small view models exposed through computed signals rather than repeating raw object traversal throughout templates
- Keep templates declarative and shallow
- Avoid non-trivial logic in templates
- Prefer reactive forms for non-trivial input flows
- Prefer pragmatic feature-scoped state over heavyweight global state by default
- Use native Angular control flow: `@if`, `@for`, `@switch`, and `@defer` when appropriate
- Do not use `*ngIf`, `*ngFor`, `*ngSwitch`, `ngClass`, or `ngStyle`
- Do not import `CommonModule` only to restore legacy structural directives in new code
- Prefer explicit property/class/style bindings over dynamic Tailwind class generation
- Visual or route components **SHOULD NOT** call transport code directly when a core or feature-owned client/service boundary is more appropriate
- User-facing screens **SHOULD** make loading, empty, error, degraded, and gated states explicit rather than leaving them implicit in missing-data behavior
- Request and async state **SHOULD** stay explicit in services, stores, or feature-owned state surfaces rather than being inferred indirectly from template null checks

## Angular Diagnostics and Quality Rules

- Keep TypeScript `strict` enabled
- Keep Angular `strictTemplates` enabled
- Treat Angular and TypeScript diagnostics as design feedback, not as something to suppress
- Do not use `as any`, `@ts-ignore`, or `@ts-expect-error` to bypass frontend typing or template issues
- Keep template parser output clean before considering build/test work complete
- Route and feature boundaries **SHOULD** remain easy to trace from route -> page -> state/service -> transport boundary

## Angular Performance Rules

- Default to `ChangeDetectionStrategy.OnPush` for components unless a strong reason exists not to
- Lazy load route areas where appropriate
- Keep AI-assisted behavior off the critical rendering path when possible
- Use deterministic fallback UX when AI is unavailable or slow
- Use Angular image optimization and intentional media sizing for content-heavy pages
- Keep the initial shell light and let route areas load lazily
- Reuse in-flight bootstrap/session work when guards and shell both need the same auth state
- Frontend composition **SHOULD** prefer the BFF and stable backend-facing service boundaries over chatty multi-endpoint page orchestration

## Accessibility Implementation Rules

- Accessibility is mandatory, not a later polish step
- Prefer native semantic HTML before custom interaction patterns
- All interactive elements must support keyboard access, visible focus, and clear labels
- Prefer implementation choices that preserve responsive-first and reduced-motion-friendly behavior

Canonical user-facing accessibility, responsive behavior, shell language, CTA hierarchy, page patterns, guest/auth honesty, and AI fallback UX belong to root `DESIGN.md`.

## Frontend Security Rules

- User input **MUST** be sanitized before rendering in templates to prevent XSS
- Do not bypass Angular's built-in sanitization using `bypassSecurityTrust*` unless there is a documented, reviewed reason
- Auth tokens **MUST NOT** be stored in `localStorage`; prefer `httpOnly` cookies managed by the BFF
- Frontend code **MUST NOT** contain secrets, API keys, or credentials
- CORS behavior is owned by the API Gateway and BFF; frontend code **SHOULD NOT** work around CORS restrictions with proxy hacks in production
- Content Security Policy headers **SHOULD** be configured at the API Gateway or BFF level; frontend code **SHOULD NOT** use inline scripts or `eval`
- Third-party scripts and dependencies **SHOULD** be audited before addition; `npm audit` **SHOULD** remain clean

## Frontend Observability Rules

- Unhandled errors **SHOULD** be captured by a global error handler and reported
- Frontend performance metrics (route navigation timing, API call latency) **SHOULD** be measurable when observability tooling is in place
- Console logging **SHOULD** be intentional and structured; do not leave debug `console.log` in production code
- Frontend error states **SHOULD** produce useful context for debugging, not just generic "something went wrong" messages

## Frontend Failure-State Implementation Rules

- Async UI flows **MUST** model realistic loading, empty, retryable error, hard error, and gated states explicitly in code rather than leaving them implicit in component null-check behavior
- Session-expired or auth-invalid states **SHOULD** be represented explicitly in frontend state handling instead of collapsing into unknown failure buckets
- Retry behavior **SHOULD** be modeled intentionally rather than hidden in silent loops or ad hoc retry branching

Visible user-facing async-state treatment, state naming, state hierarchy, and degraded AI UX belong to root `DESIGN.md`.

## TailwindCSS Rules

- Keep class names static and fully spelled out in source
- Do not build Tailwind classes dynamically
- Use mobile-first responsive utilities
- Prefer CSS/state variants over extra JS branching when styling can express the behavior
- Keep global stylesheet files minimal; global styles should mostly contain Tailwind imports, resets, and a very small set of app-wide tokens
- Group long utility lists in a stable, readable order
- SCSS or component CSS should be the exception, not the default

Semantic roles, design-token intent, and primitive growth policy belong to root `DESIGN.md`.

## Frontend Testing Rules

- Components with meaningful logic **SHOULD** have unit tests
- Feature-critical user flows **SHOULD** have integration or E2E tests where practical
- Test files **MUST** use the `*.spec.ts` suffix and live alongside the code they test
- Tests **MUST** run with `npm test -- --watch=false` and pass before frontend work is considered complete
- Do not write tests that only assert that Angular renders a component without errors unless the component has no meaningful behavior
- Prefer testing observable behavior (user interactions, rendered output, service calls) over internal implementation details
- Mock HTTP calls and external services; do not make real API calls in unit tests
- Keep test descriptions clear and behavior-focused

## Frontend Maintainability Rules

- Prefer readable templates over dense utility-class noise; extract components when markup becomes hard to reason about
- Keep API interaction behind stable frontend-facing interfaces aligned with the BFF
- Avoid unnecessary global state, premature abstraction, and hidden UI behavior
- Update repo context docs when the frontend workspace shape or local-dev frontend auth flow materially changes
- Keep route naming, folder naming, and feature naming explicit and product-aligned
- Prefer the smallest correct scope for reuse: app-specific first, then broader sharing only when a pattern is genuinely repeated
- In the current single-app workspace, keep transport details behind explicit infrastructure-style folders within the app when a full shared-library extraction would be premature
- Frontend changes are not complete until the relevant diagnostics, tests, and build paths are clean for the affected work
- AI-generated frontend code **SHOULD** prefer small components, explicit state transitions, and direct route-to-page traceability over wrapper-heavy or speculative abstractions
- AI-generated frontend code **MUST NOT** hide meaningful behavior in template cleverness, silent side effects, or unnecessary reusable layers

Design-system primitive rules, semantic roles, and intentional exception policy belong to root `DESIGN.md`.

## Frontend Verification Checklist

Before frontend work is complete, verify:

- TypeScript and Angular template diagnostics are clean
- `npm run lint` passes
- `npm test -- --watch=false` passes
- `npm run build` succeeds
- components use `ChangeDetectionStrategy.OnPush`
- no legacy directives (`*ngIf`, `*ngFor`, `CommonModule`) were introduced
- no `as any`, `@ts-ignore`, or `bypassSecurityTrust*` were introduced
- loading, empty, and error states are handled for new async flows
- accessibility basics are met (keyboard, focus, labels, semantic HTML)
- route and feature boundaries remain traceable
- no secrets or credentials exist in frontend code
- BFF-facing service boundaries are used instead of direct multi-endpoint orchestration

## Frontend Exception Handling Rule

- Deviations from a major frontend **SHOULD** rule **SHOULD** be intentional and documented with:
  - the rule being deviated from
  - why the default does not fit
  - the risk introduced
  - how the risk is mitigated
