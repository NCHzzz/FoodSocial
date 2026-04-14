# Angular 21+ Multi-App Frontend AI Agent Setup Guide

This document is a detailed Angular 21 reference guide for shaping frontend rules in this repo.

It is **not** the canonical operating contract for FoodSocial agents. The canonical repo rules live in:

- root `AGENTS.md`
- `Frontend/AGENTS.md`
- `docs/ai/repo-map.md`
- `docs/ai/workflows.md`
- `docs/ai/stack-rules/angular-tailwind.md`
- `docs/ai/stack-rules/frontend-ui-unification.md`

It is designed for repositories that contain multiple frontend apps such as:
- web
- admin
- mobile web / PWA / Ionic shell
- backoffice
- partner portal

It is also designed for workspaces where **each frontend domain maps to a backend service boundary**, and where shared domain libraries under `libs/<domain>/*` should follow **Clean Architecture**.

You can use this document as source material when refining frontend rules, architecture, and Angular implementation standards.

## Lightweight usage rule

This file is a **deep reference**, not part of the default agent workflow.

Default workflow should use:

- `AGENTS.md`
- `docs/ai/workflows.md`
- `docs/ai/stack-rules/angular-tailwind.md`
- `docs/ai/stack-rules/frontend-ui-unification.md` for UI work

Use this guide only when:

- a second Angular app is being introduced
- the repo is moving toward a real `apps/ + libs/` frontend workspace
- frontend architecture rules need deeper source material beyond the repo-specific stack rules

Recommended setup:
- keep the short mandatory contract in `AGENTS.md`
- keep repo-specific active Angular rules in `docs/ai/stack-rules/angular-tailwind.md`
- keep repo-specific UI rules in `docs/ai/stack-rules/frontend-ui-unification.md`
- link this file from `Frontend/README.md` only as a deeper reference, not as canonical workflow truth

---

## 1. When this guide applies

Use this guide when your frontend workspace has **two or more apps** and they share some combination of:
- design system primitives
- authentication and session handling
- API clients
- contracts or models
- business utilities
- route conventions
- engineering standards

Typical example:
- `web`: end-user product
- `admin`: operational or internal tool
- `mobile`: mobile web, PWA, or Angular-based mobile shell

If “mobile” is a separate native application that does not use Angular, do not force it into the same Angular workspace. In that case, only share contracts, design tokens, or documentation where appropriate.

### Current-repo interpretation rule

This guide is intentionally written for a mature **multi-app Angular workspace**, but a repository may still be in a valid earlier stage with only **one Angular app**.

When the repo has only one active Angular app, interpret this guide in two layers:

- **Strict now**: Angular 21 coding standards, standalone-only patterns, signals usage, lazy routing, strict diagnostics, Tailwind-first styling, explicit layering, and transport boundaries.
- **Future-state guidance**: full `apps/ + libs/` workspace shape, multi-app dependency governance, and service-aligned domain-library extraction.

Do **not** force a single-app repo to adopt `apps/*` and `libs/*` immediately just to satisfy the wording of this guide.

Until a second Angular app or real shared library surface exists, the repo remains compliant if it:

- keeps the workspace rooted cleanly
- uses standalone Angular 21 patterns
- organizes app code with clear `core/`, `shell/`, and lazy `features/` areas
- keeps HTTP transport details out of pages/components and behind explicit boundaries
- preserves a clean migration path toward `apps/*` and `libs/*` later

---

## 2. Core goals for the AI Agent

The AI Agent in this workspace must:
- understand that this is a **multi-app workspace**, not a single-app codebase
- preserve strict boundaries between apps
- preserve strict boundaries between domains
- follow Angular 21+ modern architecture
- use **Clean Architecture** inside shared domain libraries
- keep changes small, explicit, and reviewable
- avoid premature abstraction and premature sharing
- generate production-ready, testable, maintainable code

The AI Agent must **not** behave like an uncontrolled code generator.

Every change must have:
- a clear goal
- a clearly identified affected app or apps
- a clearly identified affected domain or domains
- a clearly identified affected layer
- a clear explanation for why the change belongs there

---

## 3. Technical baseline

This workspace follows these defaults:
- Angular 21+
- standalone components only
- do not explicitly add `standalone: true` in component metadata unless the repo has a specific compatibility reason
- lazy loading by route and feature
- `ChangeDetectionStrategy.OnPush` by default
- Angular Signals for local UI state
- `provideHttpClient()` with functional interceptors
- Reactive Forms
- TailwindCSS as the primary styling layer
- Phosphor Icons as the primary icon system
- SCSS only where Tailwind is not the right tool
- TypeScript strict mode
- ESLint + Prettier
- feature-first architecture inside each app
- domain-oriented shared libraries
- Clean Architecture inside `libs/<domain>/*`

### Styling baseline

This frontend stack uses **Angular + TailwindCSS**.

TailwindCSS should be the default choice for:
- layout
- spacing
- typography
- sizing
- flex/grid composition
- responsive behavior
- state variants such as hover, focus, disabled, selected
- dark mode or theme variants when applicable

SCSS or component-level styles should be used only when:
- a selector-based rule is clearer than utility classes
- a third-party component requires overrides
- a reusable visual pattern becomes too noisy in raw utility form
- advanced animation, pseudo-elements, or complex selectors are required

The AI Agent should assume **Tailwind-first styling**, not SCSS-first styling.

### Angular implementation baseline

The AI Agent must follow these Angular implementation rules:
- use standalone components only
- do not explicitly add `standalone: true` unless the repo specifically requires it
- use signals for state management when state is local or view-oriented
- lazy-load feature routes
- do not use `@HostBinding`
- do not use `@HostListener`
- use `host: {}` in component or directive metadata instead
- use `NgOptimizedImage` for all static images
- do not use `NgOptimizedImage` for inline base64 images

### Template and import baseline

The AI Agent must follow these strict template rules:
- do not import `CommonModule`
- do not use `ngClass`
- do not use `ngStyle`
- do not use `ngIf`
- do not use `ngFor`
- do not use `ngSwitch`
- use class bindings, style bindings, and Tailwind utilities instead
- use native Angular control flow
- prefer `@if`, `@for`, and `@switch`
- do not use `*ngIf`, `*ngFor`, or `*ngSwitch`

---

## 4. High-level workspace structure

Use an `apps + libs` workspace layout.

```text
frontend/
├── AGENTS.md
├── README.md
├── angular.json
├── eslint.config.js
├── package.json
├── tsconfig.json
├── .prettierrc
├── .editorconfig
├── tailwind.config.js
├── postcss.config.js
├── apps/
│   ├── web/
│   │   ├── src/
│   │   │   ├── main.ts
│   │   │   ├── styles.scss
│   │   │   └── app/
│   │   │       ├── app.config.ts
│   │   │       ├── app.routes.ts
│   │   │       ├── core/
│   │   │       ├── shell/
│   │   │       └── features/
│   │   └── project.json
│   ├── admin/
│   │   ├── src/
│   │   │   ├── styles.scss
│   │   │   └── app/
│   │   │       ├── app.config.ts
│   │   │       ├── app.routes.ts
│   │   │       ├── core/
│   │   │       ├── shell/
│   │   │       └── features/
│   │   └── project.json
│   └── mobile/
│       ├── src/
│       │   ├── styles.scss
│       │   └── app/
│       │       ├── app.config.ts
│       │       ├── app.routes.ts
│       │       ├── core/
│       │       ├── shell/
│       │       └── features/
│       └── project.json
├── libs/
│   ├── ui/
│   │   ├── design-system/
│   │   ├── icons/
│   │   └── tokens/
│   ├── shared/
│   │   ├── utils/
│   │   ├── types/
│   │   ├── constants/
│   │   └── testing/
│   ├── identity/
│   ├── catalog/
│   ├── orders/
│   ├── billing/
│   └── notifications/
└── docs/
    ├── architecture.md
    ├── dependency-rules.md
    ├── frontend-conventions.md
    └── ai-agent-guide.md
```

### Workspace model
- `apps/*` contains deployable applications
- `libs/ui/*` contains reusable design system and UI primitives
- `libs/shared/*` contains cross-domain utilities, base types, and testing helpers
- `libs/<domain>/*` contains one frontend domain aligned to one backend service boundary
- Tailwind configuration is shared at the workspace level unless app-specific theming requires additional layering

A domain library should not be treated as “just a folder of reused code.” It is a **bounded context** aligned with backend service ownership and business capability.

---

## 5. Domain model and backend alignment

Each `libs/<domain>` should represent **one backend service or one service boundary**.

Examples:
- `libs/identity/*` -> identity/authentication service
- `libs/catalog/*` -> catalog/product service
- `libs/orders/*` -> orders service
- `libs/billing/*` -> billing/payments service
- `libs/notifications/*` -> notifications service

This means:
- frontend domain boundaries should follow backend business boundaries
- contracts, use cases, repositories, and mapping logic should remain inside the matching domain
- code should not be placed in random “common” buckets when it clearly belongs to a service-aligned domain

Do **not** create vague shared folders such as:
- `libs/common-business`
- `libs/global-services`
- `libs/misc-domain`

If a piece of code belongs to the Orders service boundary, it belongs in `libs/orders/*`.

---

## 6. Clean Architecture inside `libs/<domain>/*`

Each domain library should follow a layered structure inspired by Clean Architecture.

Recommended structure:

```text
libs/orders/
├── domain/
│   ├── entities/
│   ├── value-objects/
│   ├── use-cases/
│   ├── ports/
│   ├── rules/
│   └── public-api.ts
├── application/
│   ├── facades/
│   ├── state/
│   ├── orchestrators/
│   ├── dto-mappers/
│   └── public-api.ts
├── infrastructure/
│   ├── api/
│   ├── repositories/
│   ├── adapters/
│   ├── persistence/
│   └── public-api.ts
├── ui/
│   ├── components/
│   ├── pages/
│   ├── forms/
│   ├── presenters/
│   └── public-api.ts
└── feature-shell/
    ├── routes/
    ├── containers/
    └── public-api.ts
```

This does **not** mean every domain must always have every folder immediately. Start with the minimum needed, but preserve the direction of dependencies.

### Layer responsibilities

#### `domain/`
Pure business concepts and business rules.

Typical contents:
- entities
- value objects
- domain rules
- use case contracts
- repository ports / interfaces
- validation rules that belong to the business domain

Rules:
- no Angular-specific code
- no `HttpClient`
- no UI code
- no direct persistence code
- should be as framework-agnostic as possible

#### `application/`
Application-specific orchestration for the domain.

Typical contents:
- facades
- store-like orchestration
- use case execution coordination
- data transformation between external DTOs and domain models
- query state for UI consumption

Rules:
- may use Angular DI if that is useful for app integration
- should not contain low-level HTTP implementation details
- should depend on domain contracts, not infrastructure internals

#### `infrastructure/`
Technical implementation details for the domain.

Typical contents:
- REST clients
- GraphQL adapters
- repository implementations
- DTO definitions from external services
- backend-specific mappers
- persistence adapters

Rules:
- contains `HttpClient` usage
- depends on backend transport details
- implements ports/interfaces defined in `domain/`
- must not own business rules that belong in `domain/`

#### `ui/`
Reusable UI pieces tied to the domain.

Typical contents:
- domain components
- domain-specific forms
- small presentational pages or widgets
- display helpers for the domain

Rules:
- may depend on `application/`
- must not bypass the application layer to call infrastructure directly
- should stay domain-focused, not app-focused

#### `feature-shell/`
Optional composition layer when the same domain feature needs to be packaged and plugged into multiple apps.

Typical contents:
- route entrypoints
- smart containers
- app integration glue

Rules:
- only add this when the same feature composition is actually reused across apps
- do not create it preemptively

---

## 7. Dependency direction rules

This is the most important architecture rule.

### Allowed dependency directions
- `apps/*` -> `libs/*`
- `libs/ui/*` -> `libs/shared/*`
- `libs/<domain>/ui` -> `libs/<domain>/application`
- `libs/<domain>/application` -> `libs/<domain>/domain`
- `libs/<domain>/infrastructure` -> `libs/<domain>/domain`
- `libs/<domain>/feature-shell` -> `libs/<domain>/ui`
- `libs/<domain>/feature-shell` -> `libs/<domain>/application`
- `libs/<domain>/*` -> `libs/shared/*` when appropriate

### Forbidden dependency directions
- `libs/*` -> `apps/*`
- `apps/web` -> `apps/admin`
- `apps/admin` -> `apps/mobile`
- `domain/` -> `application/`
- `domain/` -> `infrastructure/`
- `domain/` -> `ui/`
- `application/` -> `infrastructure/` concrete internals unless via explicit boundary controlled in the same domain
- one domain importing private implementation files from another domain
- shared libraries depending on feature shells or app code

### Practical rule
Inner layers must not know about outer layers.

In practice:
- `domain` knows nothing about Angular components or HTTP
- `application` knows business flow and orchestration
- `infrastructure` knows transport and backend details
- `ui` renders state and sends user intent inward

---

## 8. How to decide where code belongs

Use this decision order.

### Put code in an app when
- it is only used by one app
- it depends on app-specific layout, routing, or navigation
- it is tailored to a specific UX for web, admin, or mobile
- it is page composition rather than domain capability

### Put code in `libs/<domain>` when
- it belongs to a business capability aligned with one backend service
- it may be used by multiple apps now or very soon
- it defines domain rules or shared contracts
- it is domain data access or shared orchestration

### Put code in `libs/ui` when
- it is a reusable UI primitive
- it is not tied to one business domain
- it belongs to the design system

### Put code in `libs/shared` when
- it is cross-domain utility logic
- it is a base type or cross-cutting helper
- it is a testing helper or fixture builder

### When unsure
Default to the **smallest correct scope**:
- app-specific first
- then domain library if it clearly maps to a service boundary
- only then broader shared placement

---

## 9. Recommended structure inside each app

Each app should still follow feature-first organization.

```text
apps/web/src/app/
├── app.config.ts
├── app.routes.ts
├── core/
│   ├── guards/
│   ├── interceptors/
│   ├── services/
│   └── config/
├── shell/
│   ├── app-shell.component.ts
│   ├── public-shell.component.ts
│   └── authenticated-shell.component.ts
└── features/
    ├── home/
    ├── profile/
    ├── checkout/
    └── support/
```

### App-level responsibilities

#### `core/`
Only app-wide infrastructure for that app:
- app-specific guards
- app-specific interceptors
- app-specific runtime config
- analytics or feature flags specific to that app

#### `shell/`
Layout composition for that app:
- navbar
- sidebar
- responsive frame
- shell-level navigation

#### `features/`
App-specific pages and flows:
- page assembly
- app-only feature variants
- route integration

Do not move app-specific pages into `libs/<domain>/ui` just because they happen to consume the same domain.

---

## 10. TailwindCSS rules

TailwindCSS is the default styling system across this workspace.

### Primary rules
- prefer Tailwind utility classes directly in Angular templates
- prefer composition through semantic Angular components rather than large custom CSS files
- keep class lists readable and grouped logically
- prefer reusable UI primitives in `libs/ui/design-system` when patterns repeat
- do not create custom utility classes prematurely when standard Tailwind utilities already express the intent clearly

### Recommended class ordering
When generating Tailwind class strings, keep them grouped in a stable order:
- layout and positioning
- box model and sizing
- flex/grid
- spacing
- borders and radius
- background
- typography
- effects and transitions
- state and responsive modifiers

Example:

```html
<button
  class="inline-flex items-center justify-center rounded-lg border px-4 py-2 text-sm font-medium transition disabled:pointer-events-none disabled:opacity-50"
>
  Save
</button>
```

### When to extract UI patterns
If a Tailwind pattern repeats across multiple screens or apps, prefer one of these approaches:
- extract a reusable Angular component into `libs/ui/design-system`
- extract a domain-specific reusable component into `libs/<domain>/ui`
- extract shared tokens into the Tailwind theme configuration

Avoid extracting a custom CSS class solely to hide a long utility string unless the pattern is truly reused or semantically meaningful.

### Tailwind theme rules
Use the Tailwind theme to define shared design tokens such as:
- colors
- spacing scales if customized
- font families
- border radius
- shadows
- breakpoints

Do not hardcode inconsistent values throughout templates if those values should be system-level tokens.

### Tailwind and Angular rules
- use Angular components for semantic composition
- use Tailwind utilities for visual styling
- do not overload a single component with massive conditional class logic if it should be split into smaller pieces
- prefer computed class maps or small helpers only when template readability clearly improves

### Tailwind and responsiveness
The AI Agent should build responsive behavior with Tailwind utilities first, for example:
- `sm:`
- `md:`
- `lg:`
- `xl:`

Do not create separate mobile and desktop components unless behavior or information architecture truly differs.

### Tailwind and dark mode
If the workspace supports theming or dark mode:
- follow the repo’s configured Tailwind dark mode strategy
- prefer token-driven colors over one-off color combinations
- do not introduce inconsistent theme behavior at component level

### Tailwind anti-patterns
The AI Agent must avoid:
- extremely long unreadable class strings without structure
- mixing large SCSS files with Tailwind for the same concern
- arbitrary values everywhere when theme tokens should be used instead
- duplicating the same utility bundle across many files without extraction
- using inline `style` attributes unless there is a strong reason

---

## 11. Naming conventions

### Apps
- `web`
- `admin`
- `mobile`
- `partner-portal`

### Domains
- `identity`
- `catalog`
- `orders`
- `billing`
- `notifications`

### Domain layers
- `domain`
- `application`
- `infrastructure`
- `ui`
- `feature-shell`

### Files
- page component: `order-list.page.ts`
- UI component: `order-card.component.ts`
- application facade: `orders.facade.ts`
- application state: `orders.store.ts`
- domain entity: `order.entity.ts`
- value object: `money.value-object.ts`
- use case: `submit-order.use-case.ts`
- repository port: `orders-repository.port.ts`
- infrastructure API client: `orders.api.ts`
- repository implementation: `orders.repository.ts`
- mapper: `orders.mapper.ts`
- routes file: `orders.routes.ts`
- public barrel: `public-api.ts`

### Selectors
- component selector: `app-order-card`
- directive selector: `[appAutofocus]`

### Folder names
- use `kebab-case` or lowercase
- do not use vague names such as `misc`, `final-common`, or `business-stuff`

---

## 11. Rules for Angular components

### Standard component rule
Every new Angular component should default to:
- standalone-only architecture
- no explicit `standalone: true` unless repo compatibility requires it
- `OnPush`
- minimal public surface
- no direct low-level API access
- Tailwind-first styling

Example:

```ts
import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';

@Component({
  selector: 'app-counter-card',
  host: {
    class: 'block',
  },
  templateUrl: './counter-card.component.html',
  styleUrl: './counter-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CounterCardComponent {
  readonly count = signal(0);
  readonly doubled = computed(() => this.count() * 2);

  increment(): void {
    this.count.update((value) => value + 1);
  }
}
```

### Component rules
- components must stay focused and small
- page components should orchestrate, not implement low-level data access
- components must not call `HttpClient` directly
- avoid hidden side effects in getters
- do not place business rules in templates
- reusable domain UI should go in `libs/<domain>/ui`
- app-specific page assembly should stay in `apps/*/features`
- do not use `@HostBinding`
- do not use `@HostListener`
- use `host: {}` instead
- use `NgOptimizedImage` for static images
- do not use inline base64 with `NgOptimizedImage`

## 12. Rules for Signals, RxJS, and state

Use:
- `signal()` for local UI state
- `computed()` for derived state
- `effect()` only for explicit side effects

Use RxJS when:
- dealing with long-lived streams
- polling
- websockets
- cancellation-heavy flows
- operator-rich pipelines

State placement guideline:
- local page state -> component or page
- domain query state reused across pages -> `application/state`
- cross-app domain orchestration -> `application/facades` or domain application layer

### Pagination and fetch standard

If a page or feature uses paginated fetching, the AI Agent should follow this repo-proven signal convention unless the existing feature already uses another established pattern.

Required signals:
- `ready$`
- `isLoading$`
- `refresh$`
- `forceNewOnce$`
- `pageIndex1$` for UI and URL, 1-based
- `pageIndex0$` as computed API page index, 0-based
- `items$`
- `total$`

Note: this repo convention keeps the `$` suffix even for signals. The AI Agent should follow the repo naming convention instead of renaming them for stylistic purity.

### URL sync rules
- read the `page` query param on init
- write the `page` query param on pagination changes
- use `queryParamsHandling: 'merge'`
- use `replaceUrl: true`

### Query pattern

```ts
protected readonly pageIndex1$ = signal(1);
private readonly pageIndex0$ = computed(() => Math.max(0, this.pageIndex1$() - 1));

protected readonly query$ = computed(() => ({
  pageIndex: this.pageIndex0$(),
  pageSize: this.pageSize,
  // filters / search
}));
```

### Pagination rules
- UI state and URL must use 1-based paging
- API queries must use 0-based paging if the backend contract expects it
- `pageIndex0$` should be derived from `pageIndex1$`, not maintained independently
- refresh and forced re-fetch flows should be explicit through signals
- pagination state must remain URL-synchronized for shareable and reload-safe pages

## 13. Rules for forms

- Reactive Forms are the default
- strongly typed forms where practical
- validation belongs in form models or domain/application helpers as appropriate
- domain validation rules should not be duplicated across apps

If a validation rule is tied to a business invariant of a backend service boundary, prefer placing it in the domain layer.

---

## 14. Rules for HTTP and infrastructure

Low-level transport must live in `infrastructure/`.

Example structure:

```text
libs/orders/infrastructure/
├── api/
│   └── orders.api.ts
├── repositories/
│   └── orders.repository.ts
├── adapters/
│   └── orders-http.adapter.ts
└── public-api.ts
```

Rules:
- `HttpClient` only in infrastructure
- DTOs from backend stay close to infrastructure unless intentionally promoted
- repository implementations live in infrastructure
- infrastructure implements ports defined by domain
- do not leak backend DTOs directly into app UI unless deliberately modeled that way

### Single-app adaptation

In a single-app workspace that does not yet have `libs/<domain>/infrastructure`, apply the same rule inside the app by creating an explicit infrastructure-style boundary under the closest domain/app slice, for example:

```text
src/app/core/auth/infrastructure/
src/app/features/discover/infrastructure/
```

That boundary may remain app-local until reuse or domain complexity justifies extraction into shared libraries.

---

## 15. Clean Architecture example for one domain

Example for Orders:

```text
libs/orders/
├── domain/
│   ├── entities/
│   │   └── order.entity.ts
│   ├── value-objects/
│   │   └── order-status.value-object.ts
│   ├── ports/
│   │   └── orders-repository.port.ts
│   ├── use-cases/
│   │   └── submit-order.use-case.ts
│   └── public-api.ts
├── application/
│   ├── facades/
│   │   └── orders.facade.ts
│   ├── state/
│   │   └── orders.store.ts
│   ├── dto-mappers/
│   │   └── orders.mapper.ts
│   └── public-api.ts
├── infrastructure/
│   ├── api/
│   │   └── orders.api.ts
│   ├── repositories/
│   │   └── orders.repository.ts
│   └── public-api.ts
├── ui/
│   ├── components/
│   │   └── order-summary-card.component.ts
│   ├── forms/
│   │   └── submit-order-form.component.ts
│   └── public-api.ts
└── feature-shell/
    ├── containers/
    │   └── orders-page.container.ts
    ├── routes/
    │   └── orders.routes.ts
    └── public-api.ts
```

---

## 16. Route rules

Each app owns its own route tree.

### App routes example

```ts
import { Routes } from '@angular/router';
import { AppShellComponent } from './shell/app-shell.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'home',
      },
      {
        path: 'home',
        loadChildren: () => import('./features/home/home.routes'),
      },
      {
        path: 'orders',
        loadChildren: () => import('./features/orders/orders.routes'),
      },
    ],
  },
  {
    path: '**',
    redirectTo: '404',
  },
];
```

### Route rules
- each app may have a different shell and route tree
- do not force route reuse across apps unless the feature composition is truly shared
- large route areas should lazy-load
- a domain feature-shell may expose routes for reuse across apps if justified

---

## 17. Template rules

- templates must stay readable
- avoid complex expressions
- avoid repeatedly calling expensive methods in templates
- invoke signals correctly, for example `user()` instead of `user`
- break large pages into smaller components
- do not import `CommonModule`
- do not use `ngClass`, `ngStyle`, `ngIf`, `ngFor`, or `ngSwitch`
- do not use `*ngIf`, `*ngFor`, or `*ngSwitch`
- prefer class bindings, style bindings, and Tailwind utilities

Prefer modern Angular control flow:
- `@if`
- `@for`
- `@switch`
- `@defer` when appropriate

Example:

```html
@if (loading()) {
  <app-loading-spinner />
} @else if (error(); as message) {
  <app-empty-state [message]="message" />
} @else {
  <app-order-table [orders]="orders()" />
}
```

## 18. Styling rules

### Default rule
TailwindCSS is the primary styling mechanism.

Use Tailwind first for:
- spacing
- layout
- typography
- responsive behavior
- visual states
- most component styling

### When SCSS is acceptable
Use SCSS only when:
- Tailwind utilities would make the template significantly less readable
- advanced selectors or pseudo-elements are required
- a third-party library requires explicit overrides
- the style is not practical to express with utilities alone

### Placement rules
- most styles should live directly in templates as Tailwind utility classes
- app-specific visual patterns stay in the corresponding app
- reusable primitives belong in `libs/ui/*`
- reusable domain visual patterns belong in `libs/<domain>/ui`
- global stylesheet files should stay minimal

### Global stylesheet rules
Global stylesheet files should mainly contain:
- Tailwind directives
- reset or normalize rules if needed
- CSS variables or token bridges if the workspace uses them
- very broad base rules

Example root stylesheet:

```scss
@tailwind base;
@tailwind components;
@tailwind utilities;
```

### Design system rule
Do not treat Tailwind as an excuse to skip component design.

The preferred model is:
- Tailwind for low-level visual styling
- Angular components for semantic and reusable UI composition
- Tailwind theme configuration for shared visual tokens

### Iconography rules
- use Phosphor Icons as the primary icon system
- do not mix icon systems in the same view
- if a view already uses Phosphor Icons, continue using Phosphor Icons for all added icons in that view
- introducing another icon set requires an explicit design-system reason
- repeated icon usage patterns should be wrapped in reusable UI components when appropriate

### Multi-app styling rule
- web may have one density and layout system
- admin may have another
- mobile may have another
- reuse tokens and primitives where it makes sense
- do not force every app into identical visual composition when product needs differ

## 19. Testing rules

The AI Agent should add or update tests whenever important behavior changes.

### Test when
- adding business mapping logic
- adding or changing validation
- changing store or facade behavior
- changing guards or interceptors with meaningful logic
- fixing a confirmed bug
- moving logic from app code into a shared domain layer
- changing repository or use-case behavior
- changing pagination or query-param synchronization logic
- changing i18n-driven rendering behavior

### Testing guidance
- prefer unit tests for pure domain logic
- test application layer orchestration
- test infrastructure adapters where behavior is significant
- test critical component interactions
- reusable domain libraries should be tested more rigorously than app-only glue code
- missing translation keys should be treated as blocking in CI or validation scripts where available

## 20. Commit and PR rules

### Commit messages
Use conventional commits:
- `feat:`
- `fix:`
- `refactor:`
- `test:`
- `docs:`
- `chore:`

Examples:
- `feat(orders): add submit order use case`
- `fix(identity): handle expired access token`
- `refactor(catalog): move mapper into application layer`

### PR checklist
The AI Agent should ensure:
- affected app builds successfully
- lint passes
- type checking passes
- no dead code is introduced
- no unused imports remain
- dependency direction rules are preserved
- app boundaries are preserved
- tests are updated when important logic changes
- affected apps and libs are clearly listed
- no mixed icon systems are introduced in the same view
- no hard-coded UI text is introduced
- required translation keys exist in both locale files when new UI copy is added
- remaining risks are clearly stated

## 21. Verification commands

Before considering work complete, the AI Agent should run or at least recommend the relevant commands.

Examples:

```bash
npm run lint
npm run typecheck
npm run test
npm run build:web
npm run build:admin
npm run build:mobile
```

If a change only affects one app, build that app and the relevant libs.
If a change affects a shared domain library, assess impact across all consuming apps.

### i18n verification
The AI Agent must respect this strict i18n rule:
- English source file: `projects/resources/i18n/en.dev.json`
- Vietnamese source file: `projects/resources/i18n/vi.dev.json`
- no hard-coded UI text
- every new UI key must exist in both files
- missing keys are blocking

## 22. Things the AI Agent must never do

- do not re-architect the entire workspace unless explicitly asked
- do not create new `NgModule`s for new code
- do not perform wide refactors just because they look cleaner
- do not rename large sets of files without a strong reason
- do not move app-specific code into shared libs prematurely
- do not move tiny duplicated code into shared libs without real reuse value
- do not add dependencies without a clear reason
- do not change unrelated code outside the task scope without calling it out
- do not break API contracts without impact analysis
- do not bypass typing just to make code compile
- do not disable linting or type checks to make builds pass
- do not import directly from another app
- do not import private internal files from another library
- do not place domain business rules in infrastructure or UI layers
- do not import `CommonModule`
- do not use `ngClass`, `ngStyle`, `ngIf`, `ngFor`, or `ngSwitch`
- do not use `*ngIf`, `*ngFor`, or `*ngSwitch`
- do not use `@HostBinding` or `@HostListener`
- do not hard-code UI text
- do not introduce missing i18n keys
- do not mix icon systems in the same view

## 23. Standard workflow for the AI Agent

### Step 1: identify scope
The agent must identify:
- which app or apps are affected
- which domain or domains are affected
- whether the change is app-specific or shared
- which layer is the correct target

### Step 2: classify the task
Classify the work as one of:
- app-specific feature
- shared domain feature
- app-specific bug fix
- shared domain bug fix
- small refactor
- structural refactor
- testing improvement
- performance work
- tooling or DX improvement

### Step 3: choose the correct placement
Examples:
- admin-only page rendering -> `apps/admin/...`
- identity service contract change -> `libs/identity/...`
- reusable button primitive -> `libs/ui/design-system/...`
- orders repository implementation -> `libs/orders/infrastructure/...`
- orders business rule -> `libs/orders/domain/...`
- orders query orchestration -> `libs/orders/application/...`

### Step 4: implement the smallest correct change
Prefer minimal, low-risk patches.

### Step 5: self-review architecture impact
The agent must ask:
- are app boundaries preserved?
- are domain boundaries preserved?
- is the layer correct?
- is this code shared too early?
- is this code in infrastructure when it should be in domain?
- does the change require tests?

### Step 6: summarize clearly
The final report should include:
- what changed
- which apps and libs were affected
- which layers were touched
- remaining risks
- recommended verification commands

---

## 24. Definition of Done

A task is complete when:
- the code satisfies the request
- app boundaries remain correct
- domain boundaries remain correct
- Clean Architecture direction is preserved
- the workspace structure is not degraded
- linting and type checking are accounted for
- tests are added or updated when needed
- no vague TODOs are left behind
- the change summary is easy for reviewers to understand

---

## 25. Short prompt for AI Agents

```text
You are working in an Angular 21+ multi-app frontend workspace.
Follow AGENTS.md.

This repo uses:
- app boundaries under apps/*
- shared code under libs/*
- one frontend domain per backend service boundary
- Clean Architecture inside libs/<domain>/*
- standalone components only
- no explicit standalone: true unless repo-specific compatibility requires it
- OnPush by default
- signals for local state
- reactive forms
- TailwindCSS as the primary styling layer
- Phosphor Icons as the primary icon system
- lazy-loaded routes
- strict typing
- strict i18n with mirrored keys in en.dev.json and vi.dev.json

Do not create new NgModules.
Do not call HttpClient directly from pages or components.
Do not use any.
Do not import CommonModule.
Do not use ngClass, ngStyle, ngIf, ngFor, ngSwitch, *ngIf, *ngFor, or *ngSwitch.
Do not use @HostBinding or @HostListener.
Do not import from one app into another.
Do not import private internal files from another library.
Do not hard-code UI text.
Do not mix icon systems in the same view.

Prefer Tailwind utility classes for styling.
Use host: {} instead of HostBinding or HostListener.
Use NgOptimizedImage for static images.
Choose the smallest correct scope.
State which app(s), domain(s), and layer(s) are affected.
Keep changes minimal and end with verification steps.
```text
You are working in an Angular 21+ multi-app frontend workspace.
Follow AGENTS.md.

This repo uses:
- app boundaries under apps/*
- shared code under libs/*
- one frontend domain per backend service boundary
- Clean Architecture inside libs/<domain>/*
- standalone components
- OnPush by default
- signals for local state
- reactive forms
- TailwindCSS as the primary styling layer
- lazy-loaded routes
- strict typing

Do not create new NgModules.
Do not call HttpClient directly from pages or components.
Do not use any.
Do not import from one app into another.
Do not import private internal files from another library.

Prefer Tailwind utility classes for styling.
Use SCSS only when Tailwind is not the right tool.
Choose the smallest correct scope.
State which app(s), domain(s), and layer(s) are affected.
Keep changes minimal and end with verification steps.
```text
You are working in an Angular 21+ multi-app frontend workspace.
Follow AGENTS.md.

This repo uses:
- app boundaries under apps/*
- shared code under libs/*
- one frontend domain per backend service boundary
- Clean Architecture inside libs/<domain>/*
- standalone components
- OnPush by default
- signals for local state
- reactive forms
- lazy-loaded routes
- strict typing

Do not create new NgModules.
Do not call HttpClient directly from pages or components.
Do not use any.
Do not import from one app into another.
Do not import private internal files from another library.

Choose the smallest correct scope.
State which app(s), domain(s), and layer(s) are affected.
Keep changes minimal and end with verification steps.
```

---

## 26. Full prompt for AI Agents

```text
You are working inside an Angular 21+ multi-app frontend workspace.

Architecture rules:
1. Treat the repo as a multi-app workspace, not a single-app project.
2. Apps live under apps/* and must not import directly from each other.
3. Shared code lives under libs/*.
4. Each libs/<domain> maps to one backend service boundary.
5. Inside each libs/<domain>, follow Clean Architecture:
   - domain
   - application
   - infrastructure
   - ui
   - optional feature-shell
6. Business rules belong in domain.
7. HTTP and transport details belong in infrastructure.
8. Orchestration belongs in application.
9. Reusable domain UI belongs in ui.
10. App-specific composition stays in apps/*.
11. Use standalone components only.
12. Do not explicitly add `standalone: true` unless the repo specifically requires it.
13. Use OnPush by default.
14. Use signals for local state and RxJS where streaming behavior requires it.
15. Use Reactive Forms.
16. Use TailwindCSS as the primary styling layer.
17. Use Phosphor Icons as the primary icon system.
18. Use SCSS only when Tailwind is not the right tool.
19. Use `host: {}` instead of `@HostBinding` or `@HostListener`.
20. Use `NgOptimizedImage` for static images, but not inline base64 images.
21. Do not import `CommonModule`.
22. Do not use `ngClass`, `ngStyle`, `ngIf`, `ngFor`, `ngSwitch`, `*ngIf`, `*ngFor`, or `*ngSwitch`.
23. Prefer native Angular control flow with `@if`, `@for`, and `@switch`.
24. Do not use any.
25. Do not hard-code UI text.
26. Every new i18n key must exist in both `projects/resources/i18n/en.dev.json` and `projects/resources/i18n/vi.dev.json`.
27. Missing i18n keys are blocking.
28. Do not mix icon systems in the same view.
29. Keep changes minimal and scoped.

When implementing:
- identify affected apps
- identify affected domains
- identify the correct architecture layer
- prefer Tailwind utilities for styling work
- preserve pagination and URL sync conventions where applicable
- preserve i18n completeness
- avoid premature sharing
- avoid boundary violations
- add tests when important behavior changes
- summarize affected files, layers, risks, and verification steps

If information is missing, choose the smallest reasonable assumption instead of restructuring the workspace.
```text
You are working inside an Angular 21+ multi-app frontend workspace.

Architecture rules:
1. Treat the repo as a multi-app workspace, not a single-app project.
2. Apps live under apps/* and must not import directly from each other.
3. Shared code lives under libs/*.
4. Each libs/<domain> maps to one backend service boundary.
5. Inside each libs/<domain>, follow Clean Architecture:
   - domain
   - application
   - infrastructure
   - ui
   - optional feature-shell
6. Business rules belong in domain.
7. HTTP and transport details belong in infrastructure.
8. Orchestration belongs in application.
9. Reusable domain UI belongs in ui.
10. App-specific composition stays in apps/*.
11. Use standalone components for new code.
12. Use OnPush by default.
13. Use signals for local state and RxJS where streaming behavior requires it.
14. Use Reactive Forms.
15. Use TailwindCSS as the primary styling layer.
16. Use SCSS only when Tailwind is not the right tool.
17. Do not use any.
18. Keep changes minimal and scoped.

When implementing:
- identify affected apps
- identify affected domains
- identify the correct architecture layer
- prefer Tailwind utilities for styling work
- avoid premature sharing
- avoid boundary violations
- add tests when important behavior changes
- summarize affected files, layers, risks, and verification steps

If information is missing, choose the smallest reasonable assumption instead of restructuring the workspace.
```text
You are working inside an Angular 21+ multi-app frontend workspace.

Architecture rules:
1. Treat the repo as a multi-app workspace, not a single-app project.
2. Apps live under apps/* and must not import directly from each other.
3. Shared code lives under libs/*.
4. Each libs/<domain> maps to one backend service boundary.
5. Inside each libs/<domain>, follow Clean Architecture:
   - domain
   - application
   - infrastructure
   - ui
   - optional feature-shell
6. Business rules belong in domain.
7. HTTP and transport details belong in infrastructure.
8. Orchestration belongs in application.
9. Reusable domain UI belongs in ui.
10. App-specific composition stays in apps/*.
11. Use standalone components for new code.
12. Use OnPush by default.
13. Use signals for local state and RxJS where streaming behavior requires it.
14. Use Reactive Forms.
15. Do not use any.
16. Keep changes minimal and scoped.

When implementing:
- identify affected apps
- identify affected domains
- identify the correct architecture layer
- avoid premature sharing
- avoid boundary violations
- add tests when important behavior changes
- summarize affected files, layers, risks, and verification steps

If information is missing, choose the smallest reasonable assumption instead of restructuring the workspace.
```

---

## 27. Short root-level `AGENTS.md`

```md
# AGENTS.md

## Workspace model
- This is a multi-app Angular 21+ workspace
- Apps live under `apps/*`
- Shared code lives under `libs/*`
- Each `libs/<domain>` maps to one backend service boundary
- Do not treat the repo as a single-app codebase

## Domain architecture
Each domain library should follow Clean Architecture:
- `domain/`: entities, value objects, ports, business rules, use cases
- `application/`: facades, orchestration, state, mappers
- `infrastructure/`: API clients, repository implementations, adapters
- `ui/`: reusable domain UI
- `feature-shell/`: optional reusable feature composition

## Frontend stack
- Angular 21+
- Standalone components only
- Do not explicitly add `standalone: true` unless required by the repo
- OnPush by default
- Signals for local state
- Reactive Forms
- TailwindCSS as the primary styling layer
- Phosphor Icons as the primary icon system
- SCSS only when needed for cases Tailwind does not express well

## Mandatory rules
- Do not create new NgModules for new code
- Do not call HttpClient directly from pages/components
- Do not use `any`
- Do not import `CommonModule`
- Do not use `ngClass`, `ngStyle`, `ngIf`, `ngFor`, `ngSwitch`, `*ngIf`, `*ngFor`, or `*ngSwitch`
- Do not use `@HostBinding` or `@HostListener`
- Do not import directly from one app into another app
- Do not import private internal files from another library
- Do not place business rules in infrastructure or UI
- Do not hard-code UI text
- New i18n keys must exist in both `projects/resources/i18n/en.dev.json` and `projects/resources/i18n/vi.dev.json`
- Do not mix icon systems in the same view
- Prefer Tailwind utility classes for styling
- Use `host: {}` instead of `@HostBinding` / `@HostListener`
- Use `NgOptimizedImage` for static images
- Only move code to shared libs when reuse or service alignment is clear
- Lazy-load feature routes
- Keep changes minimal and scoped

## Placement rules
- App-specific code stays in the app
- Shared UI goes to `libs/ui/*`
- Cross-domain utilities go to `libs/shared/*`
- Service-aligned business code goes to `libs/<domain>/*`

## Implementation workflow
1. Identify affected app(s)
2. Identify affected domain(s)
3. Choose the correct layer
4. Preserve boundaries
5. Add tests when behavior changes
6. Summarize affected apps/libs/layers and verification steps
```md
# AGENTS.md

## Workspace model
- This is a multi-app Angular 21+ workspace
- Apps live under `apps/*`
- Shared code lives under `libs/*`
- Each `libs/<domain>` maps to one backend service boundary
- Do not treat the repo as a single-app codebase

## Domain architecture
Each domain library should follow Clean Architecture:
- `domain/`: entities, value objects, ports, business rules, use cases
- `application/`: facades, orchestration, state, mappers
- `infrastructure/`: API clients, repository implementations, adapters
- `ui/`: reusable domain UI
- `feature-shell/`: optional reusable feature composition

## Frontend stack
- Angular 21+
- Standalone components
- OnPush by default
- Signals for local state
- Reactive Forms
- TailwindCSS as the primary styling layer
- SCSS only when needed for cases Tailwind does not express well

## Mandatory rules
- Do not create new NgModules for new code
- Do not call HttpClient directly from pages/components
- Do not use `any`
- Do not import directly from one app into another app
- Do not import private internal files from another library
- Do not place business rules in infrastructure or UI
- Prefer Tailwind utility classes for styling
- Only move code to shared libs when reuse or service alignment is clear
- Lazy-load feature routes
- Keep changes minimal and scoped

## Placement rules
- App-specific code stays in the app
- Shared UI goes to `libs/ui/*`
- Cross-domain utilities go to `libs/shared/*`
- Service-aligned business code goes to `libs/<domain>/*`

## Implementation workflow
1. Identify affected app(s)
2. Identify affected domain(s)
3. Choose the correct layer
4. Preserve boundaries
5. Add tests when behavior changes
6. Summarize affected apps/libs/layers and verification steps
```md
# AGENTS.md

## Workspace model
- This is a multi-app Angular 21+ workspace
- Apps live under `apps/*`
- Shared code lives under `libs/*`
- Each `libs/<domain>` maps to one backend service boundary
- Do not treat the repo as a single-app codebase

## Domain architecture
Each domain library should follow Clean Architecture:
- `domain/`: entities, value objects, ports, business rules, use cases
- `application/`: facades, orchestration, state, mappers
- `infrastructure/`: API clients, repository implementations, adapters
- `ui/`: reusable domain UI
- `feature-shell/`: optional reusable feature composition

## Mandatory rules
- Do not create new NgModules for new code
- Do not call HttpClient directly from pages/components
- Do not use `any`
- Do not import directly from one app into another app
- Do not import private internal files from another library
- Do not place business rules in infrastructure or UI
- Only move code to shared libs when reuse or service alignment is clear
- Lazy-load feature routes
- Keep changes minimal and scoped

## Placement rules
- App-specific code stays in the app
- Shared UI goes to `libs/ui/*`
- Cross-domain utilities go to `libs/shared/*`
- Service-aligned business code goes to `libs/<domain>/*`

## Implementation workflow
1. Identify affected app(s)
2. Identify affected domain(s)
3. Choose the correct layer
4. Preserve boundaries
5. Add tests when behavior changes
6. Summarize affected apps/libs/layers and verification steps
```

---

## 28. Recommended supporting docs

The workspace will be easier for AI Agents to navigate if the repo also includes:
- `AGENTS.md`
- `README.md`
- `docs/architecture.md`
- `docs/dependency-rules.md`
- `docs/frontend-conventions.md`
- `eslint.config.js`
- `.prettierrc`
- `.editorconfig`
- `tsconfig.json`
- scripts for lint, typecheck, test, and per-app builds

---

## 29. Maintainer checklist

Before enabling AI Agent usage widely across the repo, verify that:
- [ ] `apps/*` and `libs/*` boundaries are clear
- [ ] domain-to-backend-service mapping is documented
- [ ] dependency direction rules are defined
- [ ] lint and typecheck scripts exist
- [ ] coding conventions are reasonably consistent
- [ ] standalone components are the default for new code
- [ ] shared domain data-access rules are documented
- [ ] AI-generated code review expectations are documented

---

## 30. Final note

This guide is intended to turn the AI Agent into a disciplined engineering collaborator inside a **multi-app Angular workspace with service-aligned domain libraries and Clean Architecture**, not into an uncontrolled code generator.

The final goal is:
- correct code
- clean boundaries
- service-aligned domain ownership
- safe changes
- predictable reviews
- a workspace that does not degrade over time

When applied consistently, AI assistance can increase delivery speed without sacrificing production quality.
