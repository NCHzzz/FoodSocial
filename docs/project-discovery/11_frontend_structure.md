# 11_frontend_structure.md

## Status

- Working draft

## Screen List

Initial MVP screen direction:

- home feed with `Following` and `Discover` tabs
- post creation flow
- search page
- AI-assisted search / suggestion experience
- post detail page
- user profile page
- saved content page
- notification center
- moderation/report flow entry points

## Page Responsibilities

### Angular route-level direction

- `feed` area: Following and Discover experiences
- `post-create` area: food-first creation flow
- `search` area: deterministic results plus AI-assisted suggestions
- `post-detail` area: content engagement and report entry points
- `profile` area: user identity, authored content, and follow relationship views
- `saved` area: saved food/place content if retained in MVP
- `notifications` area: dedicated notification center for follow, like, comment, and repost notifications in MVP, with a future path for account/moderation notifications
- recipe creation and viewing should fit inside the existing post creation and post detail experiences rather than a deep standalone recipe module

Saved experience should support revisiting posts, dishes, and places.

## Frontend Decision Summary

- keep Following and Discover visually distinct
- make food photos and dish context primary in browsing flows
- expose AI-assisted discovery clearly but keep fallback deterministic when AI is unavailable
- keep nearby and area-aware discovery understandable without requiring a map-heavy MVP UX
- route high-risk capabilities through explicit UI states so feature controls and kill switches degrade gracefully

This section exists to shape frontend decomposition and UX delivery.

Canonical product behavior lives in `04_functional_requirements.md`.

Profile MVP direction should include authored posts, saved items, follow counts, and a basic bio or taste profile, while preserving a future path to richer activity and recommendation-signal views.

Canonical profile product truth lives in `04_functional_requirements.md`.

Taste profile direction:

- MVP should allow basic manual taste-profile editing
- later evolution may combine manual taste input with AI-inferred preference understanding
- MVP taste-profile editing should focus on cuisine/tag preferences and dislike/avoid preferences
- later evolution may add richer taste attributes such as spice level or flavor dimensions

Canonical taste-profile product truth lives in `04_functional_requirements.md`.

Guest users should be able to access basic feed and post viewing paths, while authenticated routes and deeper actions remain gated.

## Component / Module Suggestions

### Recommended Angular structure

- standalone or feature-oriented route areas
- shared UI library for cards, media, search inputs, chips, dialogs, and moderation/report interactions
- domain-facing frontend modules for feed, search, posts, profiles, and auth
- dedicated API client layer for gateway/BFF communication
- explicit media presentation helpers for primary assets, derived thumbnails, and failure/fallback states

Repost/reshare UI should support a lightweight short-caption flow rather than a heavy quote-post composition model in MVP.

## State Management Direction

### Recommended MVP direction

- keep state management pragmatic and feature-scoped
- local/component state for forms and simple UI state
- shared service/store approach for auth session, feed query state, and search state
- avoid heavyweight global state patterns unless complexity proves necessary
- keep runtime feature flags or capability toggles readable at the UI boundary rather than scattering hidden checks across components

## Form Validation Needs

### MVP validation focus

- post creation validation for required media, dish identity, and text input
- search form validation for query shape and filter normalization
- auth/profile forms with explicit validation rules
- graceful validation for unresolved canonical dish/place cases if fallback text is allowed
- explicit user feedback for media upload failure, derivative unavailability, and disabled AI-assist behavior

## Frontend Technology Direction

- frontend framework: Angular
- delivery shape: responsive web SPA
- API interaction: API Gateway / BFF

## UX Notes for AI Code Generation

- prioritize mobile-responsive layouts because the MVP is responsive web first
- optimize for fast content browsing and simple posting flows
- food photos and place context should be visually prominent
- optimize primary navigation and feed design around food-first discovery
- make dish context more prominent than restaurant metadata in the default posting and browsing experience
- keep `Following` and `Discover` visually distinct to reduce user confusion
- make AI-assisted discovery visible enough that the product feels AI-native, not merely searchable
- if Following includes limited mixed content, the UI should still make follower-priority clear to users

This section exists to shape frontend implementation and decomposition, not to redefine product truth already captured in `04_functional_requirements.md` and `docs/ai/stack-rules/frontend-ui-unification.md`.

## Media and Discovery UX Direction

- the post creation flow should clearly separate upload success from publish success when asynchronous media work continues after initial submission
- search and discovery UX should support practical nearby, district, city, and area-based exploration without requiring a complex map-first information architecture
- if feature controls disable AI assistance or a provider is degraded, the UI should still feel intentionally reduced rather than broken or inconsistent
