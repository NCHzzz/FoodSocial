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
- The product should avoid building custom identity from scratch in MVP unless required for integration reasons

### FR-02 User Profile

Users must have profile pages that represent their identity and activity.

#### Acceptance criteria

- Profiles show authored posts
- Profiles show saved items
- Profiles show follow counts
- Profiles support a basic bio or taste profile
- MVP supports basic manual taste-profile editing

### FR-03 Food / Restaurant / Recipe Post Creation

Users must be able to create posts related to food, restaurants, and recipes.

#### Notes

- The content model is hybrid, but Food-first
- Food-related posts are the primary content type to optimize for in MVP
- Restaurant/place content and recipe content are secondary but supported content types
- Recipe content in MVP should support basic structured fields such as ingredients, steps, and media

#### Recommended MVP default for a food-first post

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

Deferred from default MVP posting flow unless proven necessary:

- video-first post creation
- structured taste profile
- order link
- advanced recipe structure inside food posts

#### Updated architectural direction

Because `Dish` is now approved as a first-class MVP entity, the publishing flow should prefer canonical dish linkage when possible.

For the current MVP, food-post publishing should require canonical dish identity before publish completes.

### FR-04 Discovery Feed

Users must be able to browse a content feed focused on food-related discovery.

#### Recommended MVP feed model

- Separate `Following` and `Discover` experiences
- `Following` should prioritize content from followed users, but limited mixed content is allowed
- `Discover` should use hybrid ranking

#### Hybrid ranking direction for Discover

The Discover feed should combine signals such as:

- recency
- engagement
- food relevance
- user interests and interaction history (lightweight MVP personalization)

### FR-05 Engagement

Users must be able to like, comment, save, and share content.

#### Save direction

- users can save posts, dishes, and places in MVP

#### Share direction

- users can share content externally
- users can also repost or reshare content inside the app in MVP
- in-app repost or reshare may include a short caption

### FR-06 Search

Users must be able to search by dish, restaurant, and location.

#### Approved MVP direction

Advanced discovery search.

#### Expected MVP capabilities

- structured search by dish, restaurant, and location
- tag and category filters
- discovery-oriented suggestions
- relevance beyond exact text match
- support for AI-assisted food exploration
- practical nearby and area-aware discovery behavior when location context is available

Implementation direction for search and ranking lives in `10_architecture.md`.

### FR-07 Follow Graph

Users must be able to follow other users.

### FR-08 AI Food Recommendation

The platform should recommend food-related content or suggestions to users.

#### Need confirmation

- Input signals and scope of personalization are TBD

#### Current product direction

This feature is not a side feature. It is part of the AI-native identity of the product.

#### Approved priority direction

- Primary recommendation target: dishes
- Secondary recommendation target: places
- Primary recommendation surface in MVP: Discover feed
- Personalization level in MVP: medium
- Architectural direction: preserve a clean upgrade path toward stronger personalization later
- Recommendation explanations in MVP should be short and simple rather than deeply detailed

### FR-09 Reporting and Moderation

Users must be able to report problematic content, and admins/moderators must be able to review and act on reports.

#### Approved MVP direction

Moderate trust system.

#### MVP capabilities

- report post, user, and comment
- admin review and action
- basic rule-based spam / seeding detection
- dismiss, hide/delete content, warn user, and suspend/restrict user actions
- no end-user appeal flow in MVP, but moderation records should preserve a future path to appeal workflows
- report reasons should cover spam/seeding, false or inauthentic content, abusive or harassing content, and illegal or sensitive content
- moderation remains manual-review-first in MVP with no automatic content hiding
- moderation outcomes and evidence should remain auditable enough that later appeal or secondary review design does not require redesigning core moderation records

Later-phase trust evolution should remain compatible with the MVP moderation records, but detailed later-phase direction is not canonical here.

## Use Cases

TBD

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

## Business Rules

### BR-01 Food-first posting must remain lightweight

The default food post creation flow must minimize required inputs to reduce posting friction.

### BR-02 Dish identity is mandatory for food-first posts

Food posts must include canonical dish identity so the system can support food-centric search and recommendation.

Dish resolution should happen before publish completes in the current MVP direction.

### BR-03 Place linkage is optional in MVP

Restaurant or location linkage should be encouraged but not required for all food posts.

### BR-04 Following and Discover must be separated

The system should not mix follow-graph content and discovery content into a single undifferentiated feed in MVP.

### BR-05 Discover ranking is hybrid, not single-signal

The Discover feed should combine multiple ranking signals instead of relying only on recency or only on popularity.

### BR-06 Search must support food-discovery intent, not only raw text retrieval

The search experience should help users explore food options and not behave like a plain keyword-only database search.

### BR-07 MVP moderation must support rule-based anti-spam and anti-seeding controls

The first release should include lightweight system-enforced rules to reduce obvious spam or seeding behavior.

### BR-08 MVP architecture must preserve safe degradation of AI-assisted behavior

AI-assisted discovery and recommendation features must fail back to deterministic retrieval or ranking behavior rather than becoming a hard dependency for the core user journey.

### BR-09 MVP media handling must preserve cleanup and derivative expectations

Media upload support is not sufficient by itself. The platform must preserve enough metadata and lifecycle control to support derivative assets, deletion cleanup, and moderation-sensitive review needs.

## Edge Cases / Exception Flows

### EF-01 User does not know the exact restaurant name

See `BR-03`.

### EF-02 The food is home-cooked

See `BR-03`.

### EF-03 User wants to post multiple images

The system should allow multiple images, but one primary image should be required.
