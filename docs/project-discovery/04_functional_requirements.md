# 04_functional_requirements.md

## Status

- Reviewed working source

## Functional Requirement List

### FR-01 User Account and Identity

Users must be able to create and manage accounts.

#### Why this exists

Social interaction, personalization, moderation, and saved content require identifiable user accounts.

#### Acceptance criteria

- The platform authenticates users through an external identity provider
- Authenticated users receive application-specific profiles inside the platform
- The platform can distinguish guest, registered user, and moderator/admin access levels
- Guests can browse basic public content but cannot perform deeper authenticated actions

#### Identity direction

- Authentication is delegated to an external identity provider
- The product should avoid building custom identity from scratch unless required for integration reasons
- Account lifecycle states must support active, restricted, suspended, and deleted users
- Production account flows should preserve a path to MFA, external login providers, and account recovery without forcing those features into the first implementation

### FR-02 User Profile

Users must have profile pages that represent their identity and activity.

#### Acceptance criteria

- Profiles show authored posts
- Profiles show saved items
- Profiles show follow counts
- Profiles support a basic bio or taste profile
- The product supports basic manual taste-profile editing and preserves a path to richer preference learning
- Profiles support basic visibility controls so a user can restrict profile/content visibility as the social graph matures

### FR-03 Food / Restaurant / Recipe Post Creation

Users must be able to create posts related to food, restaurants, and recipes.

#### Notes

- The content model is hybrid, but Food-first
- Food-related posts are the primary content type to optimize for first
- Restaurant/place content and recipe content are secondary but supported content types
- Recipe content should support basic structured fields such as ingredients, steps, and media

#### Recommended default for a food-first post

Required fields:

- primary photo
- canonical dish identity
- short caption / review text
- content type = food post

Optional fields:

- restaurant / place name
- location or area
- rating
- price range
- tags / cuisine tags
- multiple photos

Deferred from default posting flow unless proven necessary:

- video-first post creation
- structured taste profile
- order link
- advanced recipe structure inside food posts

#### Updated architectural direction

Because `Dish` is approved as a first-class product entity, the publishing flow should prefer canonical dish linkage when possible.

Food-post publishing should require canonical dish identity before publish completes.

### FR-04 Discovery Feed

Users must be able to browse a content feed focused on food-related discovery.

#### Product feed model

- Separate `Following` and `Discover` experiences
- `Following` should prioritize content from followed users, but limited mixed content is allowed
- `Discover` should use hybrid ranking
- Feed serving must have a clear path from database-backed query to precomputed timeline/read-model serving as usage grows
- Feed ranking must avoid a single opaque rule; ranking inputs should remain observable enough for debugging and abuse review

#### Hybrid ranking direction for Discover

The Discover feed should combine signals such as:

- recency
- engagement
- food relevance
- user interests and interaction history
- freshness and diversity so a few creators or dishes do not permanently dominate the experience

#### Feed maturity requirements

- The first production implementation may serve feeds from Post/UserProfile-backed queries and bounded BFF composition.
- A dedicated feed service becomes required when feed correctness, latency, personalization, or operational ownership can no longer be handled cleanly inside Post/BFF composition.
- The dedicated feed direction should support timeline projections, idempotent event consumption, replay/backfill, cache invalidation, and separate strategies for normal creators and high-fanout accounts.

### FR-05 Engagement

Users must be able to like, comment, save, and share content.

#### Save direction

- users can save posts, dishes, and places at launch

#### Share direction

- users can share content externally
- users can also repost or reshare content inside the app
- in-app repost or reshare may include a short caption

### FR-06 Search

Users must be able to search by dish, restaurant, and location.

#### Approved launch direction

Advanced discovery search.

#### Expected launch capabilities

- structured search by dish, restaurant, and location
- tag and category filters
- discovery-oriented suggestions
- relevance beyond exact text match
- support for AI-assisted food exploration
- practical nearby and area-aware discovery behavior when location context is available

Implementation direction for search and ranking lives in `10_architecture.md`.

### FR-07 Follow Graph

Users must be able to follow other users.

#### Social graph requirements

- users can follow and unfollow other users
- users can block users to prevent interaction and profile/content access where applicable
- users can mute users so muted content is removed from personalized surfaces without changing the public follow relationship
- graph reads must support follower/following lists, counts, and relationship state for profile and feed composition
- social graph writes must be idempotent and safe under repeated client requests
- friendship and close-friend semantics are not required until product direction proves they are needed

### FR-08 AI Food Recommendation

The platform should recommend food-related content or suggestions to users.

#### Current product direction

This feature is not a side feature. It is part of the AI-native identity of the product.

#### Approved priority direction

- Primary recommendation target: dishes
- Secondary recommendation target: places
- Primary recommendation surface: Discover feed
- Personalization level: staged from deterministic and medium-depth personalization toward stronger personalization as signals mature
- Architectural direction: preserve a clean upgrade path toward stronger personalization later
- Recommendation explanations should be short, useful, and safe to show to users
- Recommendations must degrade to deterministic retrieval when AI providers fail, time out, or are disabled

### FR-09 Reporting and Moderation

Users must be able to report problematic content, and admins/moderators must be able to review and act on reports.

#### Approved direction

Moderate trust system.

#### Product capabilities

- report post, user, and comment
- admin review and action
- basic rule-based spam / seeding detection
- dismiss, hide/delete content, warn user, and suspend/restrict user actions
- appeal can be staged after first public release, but moderation records must preserve a path to appeal workflows
- report reasons should cover spam/seeding, false or inauthentic content, abusive or harassing content, and illegal or sensitive content
- moderation remains manual-review-first for enforcement decisions until automation has review, appeal, audit, and rollback controls
- moderation outcomes and evidence must remain auditable enough that later appeal or secondary review design does not require redesigning core moderation records
- rate limits, repeated-action rules, and obvious spam/seeding checks are allowed as system-enforced safety controls before full trust scoring exists

Later-phase trust evolution should remain compatible with core moderation records, but deep automated trust scoring is staged through `16_maturity_roadmap.md`.

### FR-10 Notifications

Users must be able to receive and review product notifications.

#### Product capabilities

- follow, like, comment, repost, report outcome, and account/system notifications
- notification center retrieval and read-state management
- graceful partial-source behavior if one notification source is unavailable
- future path to push, email, or realtime delivery without redesigning notification records

### FR-11 Product Analytics and Learning

The platform must capture enough product and operational events to run a real product.

#### Product capabilities

- activation, retention, posting, search, recommendation, feed, engagement, moderation, and media lifecycle events
- event records must avoid unnecessary PII and support redaction where needed
- analytics should not block the core user path
- event naming and ownership must be stable enough that later reporting does not require rethinking core contracts

## Use Cases

### UC-01 Create a food-first post

1. User starts a new post
2. User selects or defaults to food post type
3. User uploads a primary food photo
4. User selects canonical dish identity
5. User adds a short caption or review
6. User may optionally add restaurant/place, location, rating, price range, and tags
7. User publishes the post

#### Acceptance criteria

See `FR-03`, `BR-01`, `BR-02`, and `BR-03`.

### UC-02 Browse Following feed

1. User opens the app
2. User navigates to the Following feed
3. User sees posts primarily from followed users
4. User engages with or saves content

#### Acceptance criteria

See `FR-04`, `BR-04`, and `BR-05`.

### UC-03 Browse Discover feed

1. User opens the app
2. User navigates to the Discover feed
3. The system ranks content using hybrid signals
4. User explores dishes, places, and creators beyond the current follow graph

#### Acceptance criteria

See `FR-04` and `BR-05`.

### UC-04 Use AI-assisted search and discovery

1. User expresses food intent through search or discovery actions
2. The system interprets dish, place, tag, and preference signals
3. The system returns relevant food content, places, or suggestions
4. The user refines or acts on the recommendation

#### Acceptance criteria

See `FR-06` and `BR-08`.

### UC-05 Report suspicious or low-quality content

1. User opens a post or profile
2. User submits a report reason
3. The system stores the report
4. Admin or moderator reviews the case
5. The system records the moderation outcome

#### Acceptance criteria

See `FR-09`.

### UC-06 Block or mute another user

1. User opens a profile or content action menu
2. User blocks or mutes the target user
3. The system records the relationship state idempotently
4. Future profile, interaction, and feed behavior respects the relationship state

#### Acceptance criteria

See `FR-07`.

### UC-07 Review notifications

1. User opens the notification center
2. The system shows follow, interaction, and account/system notifications
3. User marks items as read
4. The system preserves read state and degrades gracefully if one notification source is unavailable

#### Acceptance criteria

See `FR-10`.

## Business Rules

### BR-01 Food-first posting must remain lightweight

The default food post creation flow must minimize required inputs to reduce posting friction.

### BR-02 Dish identity is mandatory for food-first posts

Food posts must include canonical dish identity so the system can support food-centric search and recommendation.

Dish resolution should happen before publish completes in the current launch direction.

### BR-03 Place linkage is optional

Restaurant or location linkage should be encouraged but not required for all food posts.

### BR-04 Following and Discover must be separated

The system should not mix follow-graph content and discovery content into a single undifferentiated feed.

### BR-05 Discover ranking is hybrid, not single-signal

The Discover feed should combine multiple ranking signals instead of relying only on recency or only on popularity.

### BR-06 Search must support food-discovery intent, not only raw text retrieval

The search experience should help users explore food options and not behave like a plain keyword-only database search.

### BR-07 Moderation must support rule-based anti-spam and anti-seeding controls

The first release should include lightweight system-enforced rules to reduce obvious spam or seeding behavior.

### BR-08 Architecture must preserve safe degradation of AI-assisted behavior

AI-assisted discovery and recommendation features must fail back to deterministic retrieval or ranking behavior rather than becoming a hard dependency for the core user journey.

### BR-09 Media handling must preserve cleanup and derivative expectations

Media upload support is not sufficient by itself. The platform must preserve enough metadata and lifecycle control to support derivative assets, deletion cleanup, and moderation-sensitive review needs.

### BR-10 Block and mute must be enforced consistently

Block and mute state must be respected by profile, feed, interaction, and notification behavior according to the product visibility policy.

## Edge Cases / Exception Flows

### EF-01 User does not know the exact restaurant name

See `BR-03`.

### EF-02 The food is home-cooked

See `BR-03`.

### EF-03 User wants to post multiple images

The system should allow multiple images, but one primary image should be required.
