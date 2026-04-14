# frontend-ui-unification.md

## 1. Scope and Precedence

### Purpose

This file is the **system-wide UI/UX design source of truth** for FoodSocial.

### Governs

- product-facing visual identity
- shell presentation and page-pattern consistency
- content hierarchy and CTA hierarchy
- shared UI behavior across features
- design primitives and their intended usage
- user-facing async, AI, and degraded-state experience

### Does not govern

- Angular architecture
- route/state/service placement
- Tailwind authoring mechanics
- diagnostics, maintainability, and frontend engineering discipline

Those belong to `docs/ai/stack-rules/angular-tailwind.md`.

### Required companion files

- `docs/project-discovery/04_functional_requirements.md`
- `docs/project-discovery/06_user_flows.md`
- `docs/project-discovery/11_frontend_structure.md`
- `docs/project-discovery/07_domain_model.md` when entity hierarchy matters
- `docs/ai/stack-rules/angular-tailwind.md`
- this file

### Precedence

1. product truth in `docs/project-discovery/` wins
2. `angular-tailwind.md` wins on frontend engineering discipline
3. this file wins on UI behavior, hierarchy, and visual consistency
4. older page-local styling in code is implementation history, not future direction

### Use this file when the question is

- what should the system UI look and feel like?
- how should pages, shell, CTAs, and content hierarchy stay consistent?
- which design primitives represent the shared UI system?
- what UI/UX behavior should future agents preserve across the product?

## 2. Product UI Identity

### Purpose

Define the non-negotiable product feel for FoodSocial.

### Rules

- UI **MUST** feel food-first, warm, editorial, and responsive-web-native.
- UI **MUST** feel like a **calm, modern, thumb-friendly social utility**.
- UI **MUST** make food decision support clearer than decorative styling.
- UI **MUST** feel AI-assisted, not AI-gimmicky.
- UI **MUST NOT** look like a generic SaaS dashboard, delivery promo surface, or short-video social clone.

### Allowed inspiration

- fast content scanning
- low-friction primary actions
- quiet but confident typography
- persistent primary navigation
- smooth, orientation-preserving motion
- minimal clutter around social interactions

### Forbidden direction

- creator-first hierarchy that demotes food decisions
- over-styled social spectacle
- noisy badge-heavy feeds
- dashboard chrome or admin-like density
- aggressive promotional color blocking

### Exceptions

- None by default. Product truth must justify any visual direction that departs from this identity.

## 3. Cross-Cutting Experience Rules

### Purpose

Define experience rules that apply across all pages and primitives.

### Rules

#### Navigation

- Primary navigation **MUST** feel persistent and immediately understandable.
- Small-screen primary navigation **SHOULD** stay limited to a compact set of top-level destinations.
- Primary destinations **MUST NOT** be hidden behind drawer-first mobile navigation.
- Active destination state **MUST** be obvious through more than one cue when possible.

#### Density and rhythm

- The product **SHOULD** use medium density: more breathable than X/Twitter, tighter than a card-heavy lifestyle app.
- Feed items **MUST** be fast to scan and visually stable.
- Metadata and social counts **MUST NOT** compete with the food decision layer.

#### Typography feel

- Typography **MUST** feel modern, quiet, and highly readable.
- The system **SHOULD** favor neutral sans-serif typography with strong hierarchy discipline.
- Dish names and useful verdicts **MUST** read stronger than usernames, timestamps, and counts.

#### Motion and interaction

- Motion **MUST** support clarity, continuity, and perceived smoothness.
- Motion **MUST NOT** exist only as decoration.
- Default motion **SHOULD** prefer fade, slight translate, and subtle scale.
- Press feedback **MUST** be quick and clear.
- Reduced-motion support **MUST** remain first-class.

#### Social actions

- Core social actions **SHOULD** stay close to the content they affect.
- Like, comment, save, and share/repost **SHOULD** feel lightweight and low-friction.
- Secondary actions **SHOULD** open through menus or sheets instead of cluttering the surface.

#### Accessibility and responsive experience

- Keyboard access, focus-visible behavior, contrast, labels, and practical form usability are mandatory.
- Primary interaction **MUST NOT** depend on hover alone.
- Focused controls **MUST NOT** be obscured by shell or layout behavior.
- Responsive layouts **MUST** preserve hierarchy at small widths.
- The page **MUST NOT** require two-dimensional scrolling for ordinary content.

### Exceptions

- A richer motion or navigation treatment is allowed only when it improves orientation and remains reduced-motion safe.

## 4. Hierarchy and Feed Model

### Purpose

Define how food-first content should be ranked visually and behaviorally.

### Rules

#### Domain hierarchy

- `Dish` and `Place` are first-class entities.
- Food-first browsing and recommendation surfaces **MUST** prioritize dish identity before place metadata.
- Place context **SHOULD** enrich the choice, not replace the dish-first hierarchy.
- Social proof and engagement **MUST NOT** outrank the food decision layer in `Discover`.

#### Canonical content order

1. media slot
2. dish identity block
3. decision-support or verdict block
4. place context block
5. creator provenance block
6. engagement row
7. overflow / secondary actions
8. optional AI rationale slot

#### Discover vs Following

- `Discover` **MUST** feel exploratory, assistive, and recommendation-oriented.
- `Following` **MUST** feel relational, trusted, and calmer than `Discover`.
- `Discover` may use stronger editorial framing.
- `Following` **MUST** make social provenance clearer than recommendation theater.
- These two surfaces **MUST NOT** collapse into the same feed pattern with only label changes.

#### Public/auth honesty

- Guests **MAY** browse public-safe content surfaces.
- Authenticated-only route areas **MUST** remain clearly gated.
- The UI **MUST NOT** imply that guests can perform gated creation, editing, or deeper account actions.
- Auth uncertainty **MUST** be explained plainly, not hidden behind vague failure copy.

#### Canonical route language

- public-safe navigation: `Discover`, `Posts`, `Profiles`
- authenticated navigation: `Discover`, `Following`, `Create`, `Notifications`, `Profile`

### Exceptions

- If limited mixed content appears in `Following`, follower-priority **MUST** remain visually obvious.

## 5. Semantic Roles

### Purpose

Define the semantic design-system roles that future primitives and pages should converge toward.

### Rules

FoodSocial should use a two-layer token model:

1. foundational values
2. semantic UI roles

The design-system doc should speak in **semantic roles**, not raw utility values.

| Group | Roles / items |
|---|---|
| Foundational categories | neutral colors, warm brand/accent colors, state colors, radius values, spacing values, elevation values, motion values |
| Surface roles | `surface-canvas`, `surface-default`, `surface-subtle`, `surface-soft`, `surface-inverse`, `surface-hero` |
| Text roles | `text-primary`, `text-secondary`, `text-muted`, `text-inverse`, `text-accent` |
| Border roles | `border-subtle`, `border-default`, `border-strong`, `border-accent` |
| Action roles | `action-primary`, `action-accent`, `action-secondary`, `action-danger` |
| State roles | `state-success`, `state-warning`, `state-danger`, `state-info`, `state-gated`, `state-degraded` |
| Typography roles | `type-display`, `type-page-title`, `type-section-title`, `type-card-title`, `type-body`, `type-body-support`, `type-metadata`, `type-eyebrow`, `type-status-label`, `type-button-label` |
| Shape / spacing / elevation | shape: `shape-pill`, `shape-panel`, `shape-feature`; spacing: `space-compact`, `space-default`, `space-comfortable`, `space-section`, `space-page`; elevation: `elevation-flat`, `elevation-soft`, `elevation-raised`, `elevation-hero` |
| Icon roles | `icon-nav`, `icon-action`, `icon-inline-meta`, `icon-status`, `icon-decorative-support` |

- Semantic roles **MUST** be named by purpose, not by pigment or Tailwind class.
- Repeated UI **MUST** map to semantic roles before introducing new one-off values.
- Theme changes **SHOULD** happen through role remapping, not component-by-component restyling.

### Current truth

- The full semantic token layer is **not yet implemented in code**.
- New UI **SHOULD** stay compatible with the current warm-editorial palette and stable spacing rhythm.

### Exceptions

- If a semantic role is needed but not implemented yet, agents **MUST** describe it honestly as a target role instead of pretending it exists.

## 6. Iconography

### Purpose

Define structural icon behavior for the system.

### Rules

- Phosphor Icons are the preferred icon system for FoodSocial.
- Do not mix multiple icon families at the same hierarchy level unless there is a product-specific exception.
- Avoid emoji as structural UI icons.

#### Weight mapping

- `regular` **MUST** be the default icon weight.
- `fill` **SHOULD** be used for active or selected states.
- `bold` **SHOULD** be rare and reserved for strong emphasis.
- `duotone` **MUST NOT** become the default system-control style.

#### Preferred mapping

- inactive navigation item = `regular`
- active navigation item = `fill`
- standard inline action = `regular`
- rare emphasis action = `bold`

#### Usage rules

- Primary navigation **MUST** use icon + label, not icon-only navigation.
- Ambiguous actions **MUST** keep visible text.
- Each icon role **MUST** have a consistent default size and spacing relationship to text.
- Icons **MUST** reduce reading load, not decorate empty space.

### Current truth

- The current shell nav does not yet render icons, so nav icon slots remain a target system rule.

### Exceptions

- Non-Phosphor icons **MAY** appear for brand marks, partner marks, or rare illustrations only.
- Non-Phosphor icons **MUST NOT** mix with structural UI icons at the same hierarchy level.

## 7. Shared Primitives

### Purpose

Define the current shared UI primitives and the contract for using them.

### Component spec contract

Every shared primitive should be documentable with the same fields:

- purpose
- use for
- do not use for
- anatomy
- variants
- supported states
- responsive behavior
- accessibility requirements
- content/copy rules
- token hooks

### Primitive: `UiPageScaffoldComponent`

- **Use for:** simple placeholder or route-intro pages
- **Do not use for:** rich feature layouts
- **Anatomy:** eyebrow, title, description, optional appended content
- **Current truth:** implemented in `Frontend/src/app/shared/ui/page-scaffold/ui-page-scaffold.component.ts`

### Primitive: `UiButtonDirective`

- **Use for:** repeated CTA/button styling
- **Do not use for:** every interactive element in the app
- **Anatomy:** visible label, or icon + visible label
- **Variants:** `primary`, `accent`, `secondary`
- **Current truth:** `danger` intent exists as a design rule but is not yet a shared tone in code

### Primitive: `UiSurfaceDirective`

- **Use for:** repeated informational or state panels
- **Do not use for:** hero/editorial surfaces with materially different hierarchy
- **Anatomy:** panel container, optional title, optional support text, optional inset status/action region
- **Variants:** `default`, `subtle`, `soft`

### Primitive: `ShellNavLinkDirective`

- **Use for:** shell navigation pills only
- **Do not use for:** generic links
- **Anatomy:** label, future icon slot if shell nav adds icons later

### Primitive rules

- Agents **MUST** use existing shared primitives before re-copying equivalent local markup.
- Agents **SHOULD** extend existing primitives before creating adjacent new ones when the visual contract is clearly the same.
- Agents **MUST NOT** force a primitive onto a materially different pattern just to maximize reuse.

### Current truth

Current shared primitive source files:

- `Frontend/src/app/shared/ui/page-scaffold/ui-page-scaffold.component.ts`
- `Frontend/src/app/shared/ui/button/ui-button.directive.ts`
- `Frontend/src/app/shared/ui/shell/shell-nav-link.directive.ts`
- `Frontend/src/app/shared/ui/surface/ui-surface.directive.ts`

## 8. State Models

### Purpose

Define how interactive, async, and degraded states should be treated.

### Rules

| State family | States / scope |
|---|---|
| Interactive state model | `rest`, `hover`, `focus-visible`, `pressed`, `selected`, `disabled` |
| Async/product state model | `loading`, `empty`, `retryable-error`, `hard-error`, `gated`, `degraded-ai`, `success`, `pending` |
| State-surface contract | inline state = field/local component; section state = list/module/subsection; page state = route-level major state; banner/message state = cross-page or contextual operational state |

- Interactive state and async/product state **MUST NOT** be conflated.
- `empty`, `error`, `gated`, and `degraded-ai` **MUST** remain distinct user-facing states.
- “No results” and “AI unavailable” **MUST NOT** share the same copy or visual treatment.
- `focus-visible` **MUST** remain obvious and never be removed for cosmetic reasons.

### Current truth

- This file owns the **user-facing** state contract.
- `angular-tailwind.md` governs how those states are modeled in frontend code.

### Exceptions

- None by default. If a state is unique, it must still be classifiable against this model.

## 9. Page Families

### Purpose

Define how the system should reason about pages beyond individual route names.

### Rules

| Page family | Core expectation |
|---|---|
| placeholder / route-intro page | simple scaffold, clear orientation, honest route framing |
| feed page | fast scanning, stable hierarchy, content-first browsing |
| detail page | deeper context without losing primary content hierarchy |
| create/edit page | low-friction input flow with clear required vs optional structure |
| search/discovery page | intent-first retrieval with structured exploration |
| profile page | authored content, saved context, and identity grouped clearly |
| notification page | chronology and scanability over ornament |

- Each page family **SHOULD** define header anatomy, content priority, and state placement.
- Page families **MUST** preserve the same meaning hierarchy across breakpoints.
- `Discover` may use richer editorial framing.
- `Following` **SHOULD** remain calmer, denser, and more provenance-forward than `Discover`.
- Small screens **MUST** preserve meaning order before preserving metadata density.
- At narrow widths, the UI **MUST** keep the sequence: dish/media -> decision support -> place -> author -> counts/actions.

### Current truth

- `Discover` is the current justified route-level exception to the simple scaffold pattern.
- Placeholder route areas already default to `app-ui-page-scaffold`.

### Exceptions

- A page may depart from the default family anatomy only when product truth or a materially different hierarchy demands it.

## 10. Current System Status

### Purpose

Keep the document honest about what exists today versus what is still target-state guidance.

### Implemented today

- page scaffold primitive
- button tones: `primary`, `accent`, `secondary`
- shell nav link primitive
- surface tones: `default`, `subtle`, `soft`

### Implied but not shared yet

- chip primitive
- alert/state badge primitive
- hero surface primitive
- icon-bearing nav item pattern
- fuller feed/content-card primitive family

### Required by policy but not yet implemented

- shared danger button tone
- formal semantic token layer in code

### Current implementation reference

Until the shared system grows further, future UI **SHOULD** stay visually compatible with the live Angular shell and `Discover` page:

- warm orange / amber accents
- slate-driven neutrals
- quiet shell framing
- rounded panels
- rounded pill navigation and action buttons
- uppercase eyebrow labels with wider tracking
- centered `max-w-6xl` responsive frame
- stronger editorial treatment on `Discover`

### Inventory rule

- Agents **MUST** distinguish between what is implemented, what is implied, and what is only a target system rule.

## 11. Operational Appendix

### Purpose

Provide a compact implementation appendix for future agents.

### Adding or extending primitives

Use this decision order:

1. If the page is a simple placeholder or route introduction, use `app-ui-page-scaffold`.
2. If a panel is a repeated informational or state surface, start with `appUiSurface`.
3. If an interaction is a repeated CTA/button, start with `appUiButton`.
4. If a navigation pill matches shell behavior, use `appShellNavLink`.
5. If a repeated pattern already exists in 2+ route areas, extend a current primitive before creating a new one.
6. If the needed semantic role is not yet tokenized, use the closest current compatible implementation family and document it honestly.
7. If the pattern is genuinely distinct, promote it deliberately into a new primitive instead of forking ad hoc markup.

#### Add a new primitive only when

- the pattern is repeated or clearly about to repeat
- an existing primitive cannot absorb it cleanly
- the new primitive has a stable purpose and boundary
- it can be documented with the shared component spec contract

#### Extend an existing primitive when

- the hierarchy is the same
- the usage intent is the same
- the visual change is a variant, not a different component family
- extension reduces duplication without weakening the primitive boundary

### Intentional system-breaking exceptions

A page may break the default system intentionally only when:

- product truth requires a materially different hierarchy
- a feature introduces a clearly different content model
- a hero/editorial moment requires a distinct presentation family
- a brand or partner mark requires a non-system icon or visual treatment

Required safeguards:

- the exception is intentional
- FoodSocial identity is preserved
- dish-first hierarchy, honesty, and accessibility are not weakened
- the exception is documented here if it is likely to repeat

Known exceptions:

- `Discover` may use richer editorial framing than the default page scaffold
- hero/editorial surfaces are a separate family and must not be forced into `UiSurfaceDirective`
- brand marks and rare illustrations may use non-Phosphor visuals without redefining the icon system

### Refactor order

1. use existing shared primitives where they already fit
2. normalize shared scaffolds and state surfaces
3. normalize CTA and shell controls
4. normalize repeated content surfaces
5. formalize semantic token mappings when repetition is proven
6. refine page patterns route by route

### Design system checklist for agents

- Does the screen still read as FoodSocial rather than generic SaaS/admin UI?
- Is dish-first hierarchy preserved where relevant?
- Is `Discover` clearly different from `Following`?
- Are guest-visible actions and authenticated-only actions honest?
- Does AI assistance clarify value rather than decorate the screen?
- Are loading, empty, error, gated, and degraded states explicit where realistic?
- Does the layout still work on responsive-web mobile widths?
- Did you reuse an existing primitive where it already fit?
- If you introduced a new primitive, was the pattern truly distinct and documented honestly?
- If you broke the system intentionally, is the exception justified, accessible, and still consistent with FoodSocial identity?

### Short rule for future agents

- keep the shell quiet
- keep food content primary
- keep dishes clearer than places
- keep `Discover` exploratory and `Following` relational
- keep guests safe and informed
- keep AI useful and fallback-safe
- keep abstractions honest, incremental, and grounded in the actual Angular app
