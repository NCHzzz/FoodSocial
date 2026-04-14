# API Specification

## Overview

This document defines the API specification for the FoodSocial platform. It consolidates the high-level API design with detailed endpoint specifications, request/response shapes, and integration patterns.

## Core Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/logout` - User logout
- `POST /api/auth/refresh` - Refresh authentication token

### User Management
- `POST /api/users` - Create user profile
- `GET /api/users/{userId}` - Get user profile
- `PATCH /api/users/{userId}` - Update user profile
- `GET /api/users/{userId}/posts` - Get user's posts
- `GET /api/users/{userId}/saved` - Get user's saved items

### Content Management
- `POST /api/posts` - Create post
- `GET /api/posts/{postId}` - Get specific post
- `PATCH /api/posts/{postId}` - Update post
- `DELETE /api/posts/{postId}` - Delete post

### Feed and Discovery
- `GET /api/feed/following` - Get following feed
- `GET /api/feed/discover` - Get discovery feed
- `GET /api/feed/trending` - Get trending content

### Search and Recommendations
- `GET /api/search` - Search content
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

### Engagement
- `POST /api/posts/{postId}/likes` - Like a post
- `DELETE /api/posts/{postId}/likes` - Unlike a post
- `POST /api/posts/{postId}/comments` - Comment on post
- `DELETE /api/comments/{commentId}` - Delete comment
- `POST /api/posts/{postId}/save` - Save post
- `DELETE /api/posts/{postId}/save` - Unsave post
- `POST /api/dishes/{dishId}/save` - Save dish
- `DELETE /api/dishes/{dishId}/save` - Unsave dish
- `POST /api/places/{placeId}/save` - Save place
- `DELETE /api/places/{placeId}/save` - Unsave place
- `POST /api/posts/{postId}/repost` - Repost content

### Reporting and Moderation
- `POST /api/posts/{postId}/report` - Report post
- `POST /api/users/{userId}/report` - Report user
- `POST /api/comments/{commentId}/report` - Report comment
- `GET /api/admin/reports` - Get all reports
- `POST /api/admin/reports/{reportId}/resolve` - Resolve report
- `POST /api/admin/users/{userId}/suspend` - Suspend user

### Notifications
- `GET /api/me/notifications` - Get user notifications
- `PATCH /api/notifications/{notificationId}/read` - Mark notification as read

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

1. **Authentication**: all endpoints except auth endpoints require a valid token.
2. **Error Handling**: standard HTTP status codes with detailed error bodies.
3. **Versioning**: if explicit path versioning is introduced later, apply it consistently.
4. **Pagination**: list endpoints use cursor-based pagination.
5. **Filtering and Sorting**: list endpoints support filtering and sorting via query parameters.

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

Canonical BFF/API edge truth lives in `10_architecture.md`.

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
- Dish/place enrichment requested
- Recommendation signals updated
- Moderation checks requested
- Search read model refresh requested
- Media derivative generation requested
- Deletion or cleanup requested for content-related assets where asynchronous cleanup is appropriate

These events follow the approved Hybrid REST + async events integration model for MVP.

---

This consolidated API and integration document supersedes the previous split between `08_api_spec.md` and `09_api_and_integration_design.md`; `09_api_and_integration_design.md` should be treated as removed.
