# 04_functional_requirements.md

## Status

- Working draft

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

- TBD

### FR-03 Food / Restaurant / Recipe Post Creation

Users must be able to create posts related to food, restaurants, and recipes.

#### Notes

- The content model is hybrid, but Food-first
- Food-related posts are the primary content type to optimize for in MVP
- Restaurant/place content and recipe content are secondary but supported content types
- Recipe content in MVP should support basic structured fields such as ingredients, steps, and media

#### Initial technical implication

- The post creation flow should likely start from a shared post model with content-type-specific metadata or sections
- The feed and search experience should prioritize food entities, dish names, and dish context

#### Recommended MVP default for a food-first post

Required fields:

- primary photo
- dish selection or dish name that resolves toward a canonical dish
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

#### Why this default is recommended

- It keeps post creation lightweight enough for a consumer social product
- It preserves enough structure for search and future recommendation
- It avoids making restaurant linkage mandatory, which would slow down posting and exclude non-restaurant food content

#### Updated architectural direction

Because `Dish` is now approved as a first-class MVP entity, the publishing flow should prefer canonical dish linkage when possible.

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

#### Why this default is recommended

- It keeps the social graph experience clear and predictable
- It allows exploration beyond the follow graph
- It creates a natural place for future recommendation improvements without overcomplicating the Following feed

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

#### Recommended implementation interpretation for MVP

The product experience should feel AI-native, but the first implementation can still use a staged approach, such as:

- structured filtering
- ranking heuristics
- lightweight AI assistance for query understanding or suggestion generation

This is recommended to keep the product feasible for a solo 2-month build while preserving the intended user experience.

### FR-07 Follow Graph

Users must be able to follow other users.

### FR-08 AI Food Recommendation

The platform should recommend food-related content or suggestions to users.

#### Need confirmation

- Recommend dishes only, or dishes + restaurants + posts?
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

#### Later-phase direction

- intelligent auto-hide based on report type and trust/risk scoring

- suspicious behavior scoring
- trust score / reputation mechanisms
- deeper anti-seeding intelligence

## Use Cases

TBD

### UC-01 Create a food-first post

1. User starts a new post
2. User selects or defaults to food post type
3. User uploads a primary food photo
4. User selects or enters a dish that can resolve to a canonical `Dish`
5. User adds a short caption or review
6. User may optionally add restaurant/place, location, rating, price range, and tags
7. User publishes the post

#### Acceptance criteria

- A user cannot publish a food post without a primary photo
- A user cannot publish a food post without dish identity
- A user cannot publish a food post without caption or review text
- Optional metadata can be omitted without blocking publishing

### UC-02 Browse Following feed

1. User opens the app
2. User navigates to the Following feed
3. User sees posts primarily from followed users
4. User engages with or saves content

#### Acceptance criteria

- The Following feed is visually and behaviorally distinct from Discover
- The Following feed only or primarily shows posts from followed users

### UC-03 Browse Discover feed

1. User opens the app
2. User navigates to the Discover feed
3. The system ranks content using hybrid signals
4. User explores dishes, places, and creators beyond the current follow graph

#### Acceptance criteria

- The Discover feed ranks content using more than one signal
- The Discover feed can show content from users the current user does not follow

### UC-04 Use AI-assisted search and discovery

1. User expresses food intent through search or discovery actions
2. The system interprets dish, place, tag, and preference signals
3. The system returns relevant food content, places, or suggestions
4. The user refines or acts on the recommendation

#### Acceptance criteria

- Search should support more than exact keyword lookup
- Users should be able to discover relevant food content through structured signals and AI-assisted relevance

### UC-05 Report suspicious or low-quality content

1. User opens a post or profile
2. User submits a report reason
3. The system stores the report
4. Admin or moderator reviews the case
5. The system records the moderation outcome

#### Acceptance criteria

- Users can report posts, users, and comments
- Reported items enter a moderation review process
- Moderators can dismiss reports, hide/delete content, warn users, and suspend/restrict users

## Business Rules

### BR-01 Food-first posting must remain lightweight

The default food post creation flow must minimize required inputs to reduce posting friction.

### BR-02 Dish identity is mandatory for food-first posts

Food posts must include a dish name so the system can support food-centric search and recommendation.

Where possible, the system should resolve dish identity to a canonical `Dish` entity.

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

## Edge Cases / Exception Flows

### EF-01 User does not know the exact restaurant name

The user can still publish the post without a linked restaurant.

### EF-02 The food is home-cooked

The user can publish the post as a food post without restaurant metadata.

### EF-03 User wants to post multiple images

The system should allow multiple images, but one primary image should be required.
