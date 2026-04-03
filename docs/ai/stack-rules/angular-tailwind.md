# angular-tailwind.md

## Purpose

This file defines frontend engineering rules for the Angular application and TailwindCSS usage in this repository.

## Angular Architecture Rules

- Prefer standalone, feature-oriented Angular structure
- Keep components small and focused on one clear purpose
- Separate orchestration-heavy components from presentational UI when complexity grows
- Keep business logic out of templates and out of purely visual components
- Prefer explicit component APIs using clear inputs and outputs

## Angular State and Template Rules

- Prefer signals for local reactive state
- Keep templates declarative and shallow
- Avoid non-trivial logic in templates
- Prefer reactive forms for non-trivial input flows
- Prefer pragmatic feature-scoped state over heavyweight global state by default

## Angular Performance Rules

- Default to `ChangeDetectionStrategy.OnPush` for components unless a strong reason exists not to
- Lazy load route areas where appropriate
- Keep AI-assisted behavior off the critical rendering path when possible
- Use deterministic fallback UX when AI is unavailable or slow
- Use Angular image optimization and intentional media sizing for content-heavy pages

## Accessibility and UX Rules

- Accessibility is mandatory, not a later polish step
- Prefer native semantic HTML before custom interaction patterns
- All interactive elements must support keyboard access, visible focus, and clear labels
- Do not make AI-assisted UI the only way to complete a task
- Respect responsive-first UX and reduced-motion-friendly behavior where relevant

## TailwindCSS Rules

- Use Tailwind as a disciplined design-system implementation tool
- Keep class names static and fully spelled out in source
- Do not build Tailwind classes dynamically
- Prefer centralized tokens and theme values over scattered arbitrary values
- Use mobile-first responsive utilities
- Prefer CSS/state variants over extra JS branching when styling can express the behavior
- Extract repeated UI patterns into Angular components before resorting to large custom CSS layers

## Frontend Maintainability Rules

- Prefer readable templates over dense utility-class noise; extract components when markup becomes hard to reason about
- Keep API interaction behind stable frontend-facing interfaces aligned with the BFF
- Avoid unnecessary global state, premature abstraction, and hidden UI behavior
- Preserve clear user expectations for Following vs Discover, notifications, and AI-assisted suggestions
