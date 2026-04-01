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
- saved content page (Need confirmation)
- moderation/report flow entry points

## Page Responsibilities

### Angular route-level direction

- `feed` area: Following and Discover experiences
- `post-create` area: food-first creation flow
- `search` area: deterministic results plus AI-assisted suggestions
- `post-detail` area: content engagement and report entry points
- `profile` area: user identity, authored content, and follow relationship views
- `saved` area: saved food/place content if retained in MVP
- `notifications` area: dedicated notification center for follow-related notifications in MVP
- `notifications` area: dedicated notification center for follow, like, and comment notifications in MVP, with a future path for account/moderation notifications
- repost notifications should also be supported in the MVP notification center
- recipe creation and viewing should fit inside the existing post creation and post detail experiences rather than a deep standalone recipe module

Saved experience should support revisiting posts, dishes, and places.

Profile MVP direction should include authored posts, saved items, follow counts, and a basic bio or taste profile, while preserving a future path to richer activity and recommendation-signal views.

Taste profile direction:

- MVP should allow basic manual taste-profile editing
- later evolution may combine manual taste input with AI-inferred preference understanding
- MVP taste-profile editing should focus on cuisine/tag preferences and dislike/avoid preferences
- later evolution may add richer taste attributes such as spice level or flavor dimensions

Guest users should be able to access basic feed and post viewing paths, while authenticated routes and deeper actions remain gated.

## Component / Module Suggestions

### Recommended Angular structure

- standalone or feature-oriented route areas
- shared UI library for cards, media, search inputs, chips, dialogs, and moderation/report interactions
- domain-facing frontend modules for feed, search, posts, profiles, and auth
- dedicated API client layer for gateway/BFF communication

Repost/reshare UI should support a lightweight short-caption flow rather than a heavy quote-post composition model in MVP.

## State Management Direction

### Recommended MVP direction

- keep state management pragmatic and feature-scoped
- local/component state for forms and simple UI state
- shared service/store approach for auth session, feed query state, and search state
- avoid heavyweight global state patterns unless complexity proves necessary

## Form Validation Needs

### MVP validation focus

- post creation validation for required media, dish identity, and text input
- search form validation for query shape and filter normalization
- auth/profile forms with explicit validation rules
- graceful validation for unresolved canonical dish/place cases if fallback text is allowed

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
