# FoodSocial Design System

> Status: Canonical UI/UX design source of truth
> Last updated: 2026-05-15
> Inspiration: Claude / Anthropic editorial warmth
> Product fit: AI-native, food-first social platform for Vietnam launch market

## 1. Scope and Precedence

This file is the system-wide UI/UX design source of truth for FoodSocial. It owns product-facing visual identity, page hierarchy, shared primitives, user-facing async states, and the warm editorial visual layer.

Precedence:

1. `docs/project-discovery/` defines product truth.
2. `docs/ai/stack-rules/angular-tailwind.md` defines Angular and Tailwind engineering discipline.
3. `DESIGN.md` wins on UI behavior, hierarchy, visual consistency, primitives, and product-facing state treatment.
4. Older page-local styling is implementation history, not future direction.

## 2. Product Identity

FoodSocial UI must feel food-first, warm, editorial, responsive-web-native, calm, modern, and thumb-friendly. It must feel AI-assisted without becoming AI-gimmicky.

Forbidden direction:

- generic SaaS dashboard
- delivery promo surface
- short-video social clone
- creator-first hierarchy that demotes food decisions
- noisy badge-heavy feeds
- dashboard chrome or admin-like density

## 3. Visual Theme

Use a parchment-toned canvas, warm neutral palette, terracotta accent, editorial serif headings, quiet sans UI text, rounded panels, and ring-based depth.

Key rules:

- Use Parchment `#f5f4ed` as the primary page background.
- Use Ivory `#faf9f5` for cards and elevated surfaces.
- Use Anthropic Near Black `#141413` for primary text and dark sections.
- Use Terracotta Brand `#c96442` for primary CTA and highest-signal brand moments.
- Use Coral Accent `#d97757` for lighter accent text and dark-surface links.
- Keep all grays warm: olive, stone, brown, cream. Avoid cool blue-grays.
- Use ring shadows (`0 0 0 1px`) over heavy drop shadows.
- Use serif headings for editorial hierarchy; sans-serif for controls and metadata.

## 4. Food-First Hierarchy

`Dish` and `Place` are first-class entities. Food browsing and recommendation surfaces must prioritize dish identity before place metadata.

Canonical content order:

1. media slot
2. dish identity block
3. decision-support or verdict block
4. place context block
5. creator provenance block
6. engagement row
7. overflow / secondary actions
8. optional AI rationale slot

`Discover` must feel exploratory and assistive. `Following` must feel relational, trusted, and calmer than `Discover`.

## 5. Semantic Roles

| Role | Value |
|---|---:|
| `surface-canvas` | Parchment `#f5f4ed` |
| `surface-default` | Ivory `#faf9f5` |
| `surface-subtle` | Warm Sand `#e8e6dc` |
| `surface-soft` | Border Cream `#f0eee6` |
| `surface-inverse` | Near Black `#141413` |
| `text-primary` | Near Black `#141413` |
| `text-secondary` | Olive Gray `#5e5d59` |
| `text-muted` | Stone Gray `#87867f` |
| `text-inverse` | Ivory `#faf9f5` |
| `text-accent` | Terracotta `#c96442` |
| `border-subtle` | Border Cream `#f0eee6` |
| `border-default` | Border Warm `#e8e6dc` |
| `border-strong` | Ring Deep `#c2c0b6` |
| `action-primary` | Terracotta `#c96442` |
| `action-secondary` | Warm Sand `#e8e6dc` |
| `action-danger` | Error Crimson `#b53333` |
| `state-info` | Focus Blue `#3898ec` |

Repeated UI must map to semantic roles before adding one-off values.

## 6. Typography

| Role | Font | Size | Weight | Line height |
|---|---|---:|---:|---:|
| Display | Georgia / serif | 64px | 500 | 1.10 |
| Page title | Georgia / serif | 52px | 500 | 1.20 |
| Section heading | Georgia / serif | 36px | 500 | 1.30 |
| Card title | Georgia / serif | 25px | 500 | 1.20 |
| Body | system-ui / sans | 16px | 400 | 1.60 |
| Metadata | system-ui / sans | 14px | 400-500 | 1.43 |
| Label | system-ui / sans | 12px | 500 | 1.25-1.60 |

Rules:

- Serif headings use weight `500` or below.
- Body text should use generous line height.
- Dish names and useful verdicts must read stronger than usernames, timestamps, and counts.
- Monospace is only for code.

## 7. Shape, Spacing, and Elevation

- Base spacing unit: `8px`.
- Main container max width: about `1200px`.
- Major sections: `80px` to `120px` vertical rhythm on desktop.
- Standard card radius: `8px` to `16px`.
- Hero/media radius: up to `32px`.
- Touch target: at least `44px` where practical.
- Whisper shadow: `0 4px 24px rgba(0,0,0,.05)`.
- Ring shadow: `0 0 0 1px` with warm ring colors.

## 8. Components

Buttons:

- Primary terracotta: `#c96442` background, Ivory text, `8px` to `12px` radius.
- Secondary warm sand: `#e8e6dc` background, Charcoal Warm text, warm ring.
- Dark charcoal: `#30302e` background, Ivory text.

Cards:

- Ivory or warm soft surfaces on light pages.
- Dark Surface on inverse sections.
- Border Cream or Border Warm borders.
- Use ring/whisper depth, not heavy shadows.

Inputs:

- Warm borders, Near Black text, `12px` radius.
- Focus Blue ring only for accessibility.

## 9. State and Accessibility

Keep interactive states distinct from product states.

Product states: `loading`, `empty`, `retryable-error`, `hard-error`, `gated`, `degraded-ai`, `success`, `pending`.

Rules:

- “No results” and “AI unavailable” must not share the same copy or treatment.
- Focus-visible must remain obvious.
- Primary interaction must not depend on hover alone.
- Guest-visible actions and authenticated-only actions must be honest.

## 10. Deferred Design Decisions

- **Dark mode**: not in scope for beta/launch. Do not generate dark mode variants unless explicitly approved.
- **Animation timing and easing**: no canonical spec yet. Prefer subtle, fast transitions (150–250ms ease-out). Respect `prefers-reduced-motion`.
- **Responsive breakpoints**: follow TailwindCSS v4 defaults until a custom breakpoint set is approved.

## 11. Short Rule

- keep shell quiet
- keep food content primary
- keep dishes clearer than places
- keep `Discover` exploratory and `Following` relational
- keep guests safe and informed
- keep AI useful and fallback-safe
