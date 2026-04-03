# 06_user_flows.md

## Status

- Working draft

## Core User Flows

### Flow 1: Discover what to eat

Primary MVP flow.

The product should optimize this flow before place-first and recipe-first flows.

#### Recommended feed structure

- `Following` tab: primarily social graph content with limited mixed content allowed
- `Discover` tab: hybrid-ranked discovery content

#### Discover ranking inputs (recommended MVP direction)

- recency
- engagement
- food relevance
- lightweight user-interest signals

### Flow 2: Share a good dish or restaurant

Food-first posting should be the default mental model.

The ideal posting flow should likely begin with the dish or food item, then optionally attach restaurant/place context and supporting review details.

#### Recommended default steps

1. User taps create post
2. User selects food post
3. User uploads a primary photo
4. User selects or resolves dish identity
5. User writes a short caption or review
6. User optionally adds restaurant/place, area, rating, price range, and tags
7. User publishes the post

### Flow 3: Engage with food content

Recommended MVP engagement actions include:

- like content
- comment with nested replies
- save posts, dishes, and places
- share externally
- repost or reshare content inside the app with an optional short caption

### Flow 4: Search by dish / restaurant / location

The approved direction is advanced discovery search rather than basic text-only search.

#### Recommended MVP interpretation

1. User enters a search query or taps suggested discovery prompts
2. The system interprets the query in a food-centric way
3. The system returns relevant dishes, places, tags, or posts
4. The user applies filters or continues exploring

#### Notes

- The product should feel AI-native in search and discovery
- The initial implementation may still use structured search plus ranking/relevance assistance instead of full semantic search everywhere

### Flow 5: Follow a user and revisit saved content

Recommended MVP flow:

1. User follows another user or saves a post, dish, or place
2. The system stores the follow relationship or saved item under the user account
3. The user revisits saved items later from the saved area
4. The user receives follow and interaction notifications in the notification center
5. The user can navigate from saved items or notifications back into relevant content/detail contexts

Follow-notification direction:

- follow, like, and comment actions should generate user-visible notifications in MVP
- notifications should be accessible through a dedicated notification center
- notification design should preserve a path toward later moderation/account notifications
- repost or reshare actions should also generate user-visible notifications in MVP

#### Initial implication

Following is not only a relationship feature. It is also a primary retrieval path through the dedicated Following feed, even if the feed still allows limited mixed content.

### Flow 6: Report suspicious or low-quality content

Recommended MVP flow:

1. User reports a post, user, or comment
2. System records report reason and target
3. Admin reviews the report
4. Admin takes action such as dismiss, hide, delete, warn, suspend, or restrict
5. The outcome is logged for auditability

Initial report reason categories should include spam/seeding, false or inauthentic content, abuse/harassment, and illegal or sensitive content.

Appeal direction:

- no end-user appeal flow in MVP
- moderation outcomes should still be stored in a way that supports future appeal review flows
- no automatic content hiding in MVP; future phases may add score-based auto-hide behavior

### Flow 7: Receive AI-based suggestions

AI should support food decision-making, not just generic content ranking.

Example user intents:

- what should I eat now?
- what is good near me?
- what dish matches my recent interests?

Priority direction:

- suggest dishes first
- then connect the user to relevant places
- deliver the most important recommendation experience through the Discover feed in MVP
- use medium-depth personalization in MVP while preserving a path to stronger personalization later
- provide short explanation hints where recommendation trust or clarity benefits from it

## Alternate Flows

### AF-01 Dish cannot be matched canonically at publish time

- The system should guide the user to resolve dish identity more strictly than place identity
- The current MVP direction should block publish until canonical dish identity is resolved

### AF-02 Place cannot be matched canonically at publish time

- The user may continue with fallback text for the place
- The system should preserve enough data for later canonicalization or admin cleanup

### AF-03 AI recommendation is unavailable

- The feed and search experience should fall back to deterministic ranking and structured retrieval
- The user should still be able to browse and search without blocking errors

### AF-04 Report does not result in action

- A moderator may dismiss the report
- The report outcome should still be recorded for auditability

## Approval / Status Transitions

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

- MVP recommendation is medium-personalization
- later phases may evolve toward stronger personalization without changing the core user-facing flow model
