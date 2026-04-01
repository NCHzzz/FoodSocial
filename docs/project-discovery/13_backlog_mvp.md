# 13_backlog_mvp.md

## Status

- Working draft

## Epics

### Epic 1: Account and identity

Goal: support guest browsing and authenticated user identity through an external identity provider.

Profile direction:

- MVP profile should include authored posts, saved items, follow counts, and a basic bio or taste profile
- later profile evolution may include richer activity and recommendation-signal views
- MVP should allow basic manual taste-profile editing
- later evolution may combine manual taste preferences with AI-inferred taste understanding
- MVP taste-profile editing should include cuisine/tag preferences and dislike/avoid preferences
- later evolution may add richer taste-attribute capture such as spice or flavor preferences

### Epic 2: Content creation and publishing

Goal: let users create food-first posts, place-aware posts, and structured recipe posts with minimal posting friction.

Potential stories:

- As a user, I can create a food post with media, dish identity, and caption/review text
- As a user, I can optionally attach canonical or fallback place information to a post
- As a user, I can create a recipe post with ingredients, steps, and media
- As a user, I can repost or reshare content with an optional short caption

### Epic 3: Discovery feed and engagement

Goal: support social engagement through likes, nested comments, saves, external sharing, and in-app repost/reshare behavior.

Repost direction:

- in-app repost or reshare may include a short caption in MVP

### Epic 4: Search and food discovery

Goal: help users find dishes, places, and relevant food content through structured and AI-assisted discovery.

Potential stories:

- As a user, I can search by dish, place, and location
- As a user, I can receive AI-assisted query suggestions and relevant retrieval results
- As a user, I can navigate from discovery results into dish, place, and post details

### Epic 5: Follow graph and personalization

Goal: support following behavior plus saved items that help users return to posts, dishes, and places.

Additional MVP direction:

- follow, like, and comment actions should create notifications visible in a dedicated notification center
- notification design should preserve a future path toward moderation or account-level notifications
- repost or reshare actions should also create notifications in MVP

### Epic 6: AI recommendation

Goal: recommend dishes first, then places, in a way that supports the Food-first AI-native product direction.

Initial MVP surface priority: Discover feed.

Initial personalization depth: medium, with a planned upgrade path toward stronger personalization later.

Explanation direction: short recommendation explanations in MVP where useful.

### Epic 7: Agent-friendly platform capabilities

Goal: keep platform contracts structured enough for controlled-access agent consumption.

Potential stories:

- As a platform owner, I can expose controlled-access APIs for agent retrieval and safe actions
- As the system, I can keep agent-facing contracts auditable and authorization-aware

### Epic 8: Moderation and reporting

Goal: support trustworthy community behavior in MVP through reporting and moderate trust controls.

Potential stories:

- As a user, I can report suspicious or low-quality content
- As a user, I can report a post, a user, or a comment
- As an admin, I can review reports and take moderation actions
- As an admin, I can dismiss, hide/delete content, warn a user, and suspend/restrict a user
- As the system, I can flag basic spam or suspicious posting patterns using rules

Future direction:

- appeals are not in MVP, but moderation records should support later appeal workflows
- MVP moderation is manual-review-first with no automatic content hiding
- later phases may add intelligent auto-hide based on report type and scoring

Initial report categories:

- spam / advertising / seeding
- false or inauthentic content
- abusive or harassing content
- illegal or sensitive content

### Epic 9: Service infrastructure and contracts

Goal: make the chosen microservices architecture operable enough for MVP without overwhelming product delivery.

Potential stories:

- As a developer, I can run the MVP services locally with a reproducible setup
- As a developer, I can define and validate service contracts between core domains
- As the system, I can degrade gracefully when a downstream service is unavailable

## User Stories

Initial prioritized examples:

- As a guest, I can browse basic public food content before signing in
- As a registered user, I can create a food-first post tied to a dish and optionally a place
- As a user, I can save posts, dishes, and places for later
- As a user, I can follow creators and receive follow/interaction notifications
- As a user, I can discover dish recommendations in the Discover feed
- As a moderator, I can review reports and take content-level or user-level actions

## Acceptance Criteria

Initial MVP-level examples:

- Guests can browse basic content but cannot perform authenticated actions
- Food posts require primary media, dish identity, and caption/review text
- Recommendation in MVP is visible in the Discover feed and prioritizes dishes first
- Saved content supports posts, dishes, and places
- Notification center supports follow, like, comment, and repost notifications
- Reports can target posts, users, and comments

## Priority

Primary MVP product outcome:

- achieve a limited real-user beta with repeated usage and demonstrated usefulness among a small target group

## Dependencies

Initial dependency direction:

- identity and auth precede authenticated social actions
- canonical dish and place support improve publishing, search, and recommendation quality
- feed and recommendation depend on content creation plus interaction signals
- moderation depends on reporting plus admin-facing review capabilities
- notification center depends on follow and interaction event generation
