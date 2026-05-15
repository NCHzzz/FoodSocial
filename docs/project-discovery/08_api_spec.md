# API Specification

## Overview

This document defines the API specification for the FoodSocial platform. It describes the browser-facing BFF route shape first, then records planned product endpoints only where current implementation has not reached the product requirement yet.

## Core Endpoints

### Authentication
- `GET /api/auth/login` - Start browser login
- `POST /api/auth/logout` - User logout
- `POST /api/auth/refresh` - Refresh authentication token
- `GET /api/auth/session` - Get current browser session
- `GET /api/auth/csrf` - Get CSRF token for cookie-authenticated writes

### User Management
- `GET /api/users/{userId}` - Get user profile
- `PATCH /api/users/{userId}` - Update user profile
- `GET /api/users/{userId}/saved` - Get user's saved items
- `GET /api/users/{userId}/followers` - Get user's followers
- `GET /api/users/{userId}/following` - Get user's following list
- `GET /api/posts/search?authorId={userId}` - Get a user's authored posts through the Post search/read path

### Content Management
- `POST /api/posts` - Create post
- `GET /api/posts/search` - Search or list posts with bounded filters
- `GET /api/posts/{postId}` - Get specific post
- `POST /api/media/uploads` - Upload post media
- `GET /api/media/uploads/{uploadId}/{variant}` - Read approved media upload variant

### Feed and Discovery
- `GET /api/feed/following` - Get following feed
- `GET /api/feed/discover` - Get discovery feed

### Search and Recommendations
- frontend search can aggregate post, dish, and place BFF search endpoints
- `POST /api/search/assist` - AI-assisted search
- `GET /api/recommendations` - Get recommendations
- `GET /api/dishes/search` - Search dishes
- `GET /api/dishes/{dishId}` - Get specific dish
- `GET /api/places/search` - Search places
- `GET /api/places/{placeId}` - Get specific place

### Social Features
- `POST /api/users/{userId}/follow` - Follow user
- `DELETE /api/users/{userId}/follow` - Unfollow user
- `GET /api/users/{userId}/followers` - Get user's followers
- `GET /api/users/{userId}/following` - Get user's following list
- `POST /api/users/{userId}/block` - Planned block endpoint
- `DELETE /api/users/{userId}/block` - Planned unblock endpoint
- `POST /api/users/{userId}/mute` - Planned mute endpoint
- `DELETE /api/users/{userId}/mute` - Planned unmute endpoint
- `GET /api/users/{userId}/relationship` - Planned current-user relationship state endpoint

### Engagement
- `POST /api/posts/{postId}/likes` - Like a post
- `DELETE /api/posts/{postId}/likes` - Unlike a post
- `POST /api/posts/{postId}/comments` - Comment on post
- `DELETE /api/comments/{commentId}` - Delete comment
- `PUT /api/users/{userId}/saved/posts/{postId}` - Save post
- `DELETE /api/users/{userId}/saved/posts/{postId}` - Unsave post
- `PUT /api/users/{userId}/saved/dishes/{dishId}` - Save dish
- `DELETE /api/users/{userId}/saved/dishes/{dishId}` - Unsave dish
- `PUT /api/users/{userId}/saved/places/{placeId}` - Save place
- `DELETE /api/users/{userId}/saved/places/{placeId}` - Unsave place
- `POST /api/posts/{postId}/repost` - Repost content

### Reporting and Moderation
- `POST /api/posts/{postId}/report` - Report post
- `POST /api/comments/{commentId}/report` - Report comment
- `GET /api/admin/reports` - Get all reports
- `POST /api/admin/reports/{reportId}/resolve` - Resolve report
- `POST /api/users/{userId}/report` - Planned user-report endpoint when user-target report ownership is implemented
- `POST /api/admin/users/{userId}/suspend` - Planned user-enforcement endpoint when account-level moderation action ownership is implemented

### Notifications
- `GET /api/me/notifications` - Get user notifications
- `PATCH /api/notifications/{notificationId}/read` - Mark notification as read
- `GET /api/users/{userId}/notifications` - Get owner-scoped user notifications where supported
- `PATCH /api/users/{userId}/notifications/{notificationId}/read` - Mark owner-scoped notification as read where supported

### Analytics and Events
- internal-only event ingestion for product and operational events
- no public browser endpoint should be required for analytics if the platform can emit events from trusted backend flows

## Data Models for Key Entities

Canonical entity and domain truth live in `07_domain_model.md`.

This section should describe API-facing shapes and important deltas only.

### Shared rule

If the API-facing shape and domain model stay effectively identical, prefer updating `07_domain_model.md` first and keep only API-specific notes here.

### User

- API response shape matches `07_domain_model.md` `#### User`.
- Use the domain-model definition as canonical unless the API needs transport-specific omissions or expansions.

### Post (base)

- API response shape matches `07_domain_model.md` `#### Post (base)`.
- Keep API payloads explicit in examples, but do not treat this section as the authoritative domain-model home.

### FoodPost (extends Post)

- API shape matches `07_domain_model.md` `#### FoodPost (extends Post)`.
- Keep request/response examples where a payload differs materially from the canonical entity shape.
- `placeId` is nullable when no canonical place is known.

### Place

- API shape matches `07_domain_model.md` `#### Place`.
- A minimal response may use lighter nested examples than the domain-model document as long as field meaning remains the same.

### Dish

- API shape matches `07_domain_model.md` `#### Dish`.

### Comment

- API shape matches `07_domain_model.md` `#### Comment`.

### Report

- API shape matches `07_domain_model.md` `#### Report`.

## API Design Principles

1. **Authorization matrix**: endpoints must be classified as public, authenticated, owner-only, moderator/admin, or trusted-service-only.
2. **Error Handling**: standard HTTP status codes with detailed error bodies.
3. **Versioning**: MVP browser-facing APIs are currently unversioned behind the BFF. Introduce `/api/v1/...` first at the BFF boundary when the API becomes a stable external or mobile-client contract; version internal service routes only when they need independent contract evolution.
4. **Pagination**: list endpoints must remain bounded today. New or revised collection contracts should move toward cursor-style pagination with an opaque `cursor` request parameter and a response page object containing `limit`, `nextCursor`, and `hasMore`.
5. **Filtering and Sorting**: list endpoints support filtering via query parameters. Sorting should use whitelisted values such as `recent`, `popular`, or `relevance` rather than exposing arbitrary persistence field names.
6. **Error Handling**: API errors use RFC 7807 `ProblemDetails` with stable `type`, `title`, `status`, actionable `detail`, and a safe `traceId` extension where available.
7. **Browser safety**: cookie-authenticated state-changing endpoints require CSRF protection.
8. **Relationship safety**: block, mute, visibility, and moderation state must be respected by profile, feed, interaction, and notification endpoints.

## Endpoint Access Model

Public endpoints:

- public feed/discovery reads allowed by product visibility policy
- public post detail reads allowed by post visibility state
- public profile reads allowed by profile visibility state
- public dish/place reads and search

Authenticated endpoints:

- create/update posts
- like, comment, save, share, repost
- follow, block, mute
- report content or users
- read and mutate own notifications

Owner-only endpoints:

- update own profile
- read own saved items
- manage own notification read state
- perform account/session operations

Moderator/admin endpoints:

- review reports
- resolve reports
- hide/delete moderated content
- warn, restrict, or suspend users

Trusted-service-only endpoints:

- internal identity/profile bootstrap
- internal media cleanup
- internal projection refresh, notification creation, and moderation automation hooks

## Request/Response Examples

### Create Food Post
```json
POST /api/posts
{
  "contentType": "food",
  "primaryMediaUrl": "https://example.com/image.jpg",
  "dishId": "dish-uuid-here",
  "caption": "Delicious pho!",
  "placeId": "place-uuid-here",
  "rating": 5,
  "priceRange": "$$",
  "tags": ["pho", "noodles", "soup"]
}
```

### Discover Feed Request
```text
GET /api/feed/discover?cursor=...&location=...&dish=...&interest=...
```

### AI-Assisted Search
```json
POST /api/search/assist
{
  "query": "spicy noodles near district 1",
  "userContext": {
    "savedTags": ["spicy", "street-food"],
    "location": "District 1"
  }
}
```

## Integration Patterns

### Authentication Model

Canonical architecture truth for auth integration lives in `10_architecture.md`.

### Frontend Integration

Canonical BFF/API edge truth lives in `10_architecture.md`. Browser clients should treat these routes as BFF-facing routes, not direct service URLs.

### Internal Services

Canonical internal integration truth lives in `10_architecture.md`.

### Media and file handling model

Canonical media lifecycle direction lives in `10_architecture.md`.

### Runtime feature control model

Canonical rollout and fallback direction lives in `10_architecture.md` and `05_non_functional_requirements.md`.

## Webhooks and Events

Key async events include:
- Post published
- Post reposted
- Post liked
- Comment added
- User followed
- User blocked
- User muted
- Dish/place enrichment requested
- Feed projection refresh requested
- Recommendation signals updated
- Moderation checks requested
- Search read model refresh requested
- Notification creation requested
- Media derivative generation requested
- Deletion or cleanup requested for content-related assets where asynchronous cleanup is appropriate

These events follow the approved Hybrid REST + async events integration model for the product.

Product-critical event contracts should document owner, schema version, producer, consumer, retry policy, dead-letter behavior, and replay strategy.

---

This file is the canonical API and integration summary for project-discovery docs.
