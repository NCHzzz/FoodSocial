# 06_user_flows.md

## Status

- Working draft

## Core User Flows

This file is the sequence-oriented user-flow view.

Canonical product behavior, acceptance rules, and business rules live in `04_functional_requirements.md`.

### Flow 1: Discover what to eat

Primary product flow.

The product should optimize this flow before place-first and recipe-first flows.

Detailed feed behavior and ranking truth live in `04_functional_requirements.md`.

### Flow 2: Share a good dish or restaurant

Food-first posting should be the default mental model.

The ideal posting flow should likely begin with the dish or food item, then optionally attach restaurant/place context and supporting review details.

Canonical posting requirements and mandatory publish conditions live in `04_functional_requirements.md`.

#### Recommended default steps

1. User taps create post
2. User selects food post
3. User uploads a primary photo
4. User selects or resolves dish identity
5. User writes a short caption or review
6. User optionally adds restaurant/place, area, rating, price range, and tags
7. User publishes the post

### Flow 3: Engage with food content

Recommended engagement actions include:

- like content
- comment on posts
- save posts, dishes, and places
- share externally
- repost or reshare content inside the app with an optional short caption

### Flow 4: Search by dish / restaurant / location

The approved direction is advanced discovery search rather than basic text-only search.

#### Recommended interpretation

1. User enters a search query or taps suggested discovery prompts
2. The system interprets the query in a food-centric way
3. The system returns relevant dishes, places, tags, or posts
4. The user applies filters or continues exploring

Detailed search and AI-assist behavior live in `04_functional_requirements.md`.

### Flow 5: Follow a user and revisit saved content

Recommended flow:

1. User follows another user or saves a post, dish, or place
2. The system stores the follow relationship or saved item under the user account
3. The user revisits saved items later from the saved area
4. The user receives follow and interaction notifications in the notification center
5. The user can navigate from saved items or notifications back into relevant content/detail contexts

Follow-notification direction:

- follow, like, and comment actions should generate user-visible notifications
- notifications should be accessible through a dedicated notification center
- notification design should preserve a path toward later moderation/account notifications
- repost or reshare actions should also generate user-visible notifications

Following as a dedicated retrieval path is canonical product truth in `04_functional_requirements.md`.

### Flow 6: Report suspicious or low-quality content

Recommended flow:

1. User reports a post, user, or comment
2. System records report reason and target
3. Admin reviews the report
4. Admin takes action such as dismiss, hide, delete, warn, suspend, or restrict
5. The outcome is logged for auditability

Detailed moderation behavior, report categories, and appeal direction live in `04_functional_requirements.md`.

### Flow 7: Receive AI-based suggestions

Detailed recommendation intent, targets, and explanation behavior live in `04_functional_requirements.md`.

### Flow 8: Block or mute a user

Recommended flow:

1. User opens a profile or content action menu
2. User blocks or mutes the target user
3. The system stores the relationship state under the user account
4. Feed, profile, notification, and interaction surfaces respect the relationship state

Detailed relationship behavior lives in `04_functional_requirements.md`.

## Alternate Flows

### AF-01 Dish cannot be matched canonically at publish time

Canonical publish blocking rules live in `04_functional_requirements.md`.

### AF-02 Place cannot be matched canonically at publish time

Canonical place fallback behavior lives in `04_functional_requirements.md`.

### AF-03 AI recommendation is unavailable

Canonical degraded-AI behavior lives in `04_functional_requirements.md`.

### AF-04 Report does not result in action

Canonical moderation outcome behavior lives in `04_functional_requirements.md`.

## Approval / Status Transitions

These remain useful as sequence-oriented flow/state references and intentionally stay thinner than the full requirement set.

### Moderation report status direction

- `submitted`
- `under_review`
- `resolved_no_action`
- `resolved_content_action`
- `resolved_user_action`

### Moderation target action direction

- content may transition to hidden or deleted states
- users may transition to warned, restricted, or suspended states

### Recommendation maturity direction

- Initial recommendation is medium-personalization
- later phases may evolve toward stronger personalization without changing the core user-facing flow model
