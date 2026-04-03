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

### User
```json
{
  "id": "uuid",
  "username": "string",
  "email": "string",
  "displayName": "string",
  "avatarUrl": "string",
  "bio": "string",
  "tasteProfile": {
    "preferredCuisines": ["string"],
    "dislikedIngredients": ["string"],
    "dietaryRestrictions": ["string"]
  },
  "role": "enum: guest | user | admin",
  "status": "enum: active | suspended | deleted",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

### Post (base)
```json
{
  "id": "uuid",
  "type": "enum: food | restaurant | recipe",
  "authorId": "uuid",
  "caption": "string",
  "media": [{
    "type": "enum: image | video",
    "url": "string",
    "thumbnailUrl": "string",
    "caption": "string"
  }],
  "tags": ["string"],
  "location": {
    "latitude": "float",
    "longitude": "float",
    "address": "string",
    "city": "string",
    "country": "string"
  },
  "engagement": {
    "likes": "integer",
    "comments": "integer",
    "shares": "integer",
    "saves": "integer"
  },
  "status": "enum: published | hidden | deleted",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

### FoodPost (extends Post)
```json
{
  "id": "uuid",
  "dishName": "string",
  "dishId": "uuid",
  "restaurantId": "uuid",
  "rating": "integer (1-5)",
  "priceRange": "enum: $ | $$ | $$$ | $$$$",
  "tasteNotes": ["string"]
}
```

### Restaurant
```json
{
  "id": "uuid",
  "name": "string",
  "description": "string",
  "address": "string",
  "location": {"latitude": "float", "longitude": "float"},
  "cuisineTypes": ["string"],
  "priceRange": "enum: $ | $$ | $$$ | $$$$",
  "operatingHours": {"monday": {"open": "time", "close": "time"}},
  "contact": {"phone": "string", "website": "string", "email": "string"},
  "rating": {"average": "float", "count": "integer"},
  "status": "enum: active | closed | suspended",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

### Dish
```json
{
  "id": "uuid",
  "name": "string",
  "description": "string",
  "category": "string",
  "cuisine": "string",
  "ingredients": ["string"],
  "dietaryInfo": {"isVegetarian": "boolean", "isVegan": "boolean", "isGlutenFree": "boolean", "isSpicy": "boolean"},
  "nutrition": {"calories": "integer", "protein": "float", "carbs": "float", "fat": "float"},
  "status": "enum: active | deprecated",
  "createdAt": "datetime"
}
```

### Comment
```json
{
  "id": "uuid",
  "postId": "uuid",
  "parentId": "uuid",
  "authorId": "uuid",
  "content": "string",
  "status": "enum: visible | hidden | deleted",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

### Report
```json
{
  "id": "uuid",
  "reporterId": "uuid",
  "targetType": "enum: post | user | comment",
  "targetId": "uuid",
  "reason": "enum: spam | inauthentic | abuse | illegal | other",
  "description": "string",
  "status": "enum: submitted | under_review | resolved_no_action | resolved_content_action | resolved_user_action",
  "moderatorId": "uuid",
  "resolution": "string",
  "createdAt": "datetime",
  "resolvedAt": "datetime"
}
```

## API Design Principles

1. **RESTful Design**: All APIs follow REST principles with clear resource-based URLs and standard HTTP methods.
2. **JSON Format**: All request and response bodies use JSON format.
3. **Authentication**: All endpoints (except auth endpoints) require a valid JWT token in the Authorization header.
4. **Error Handling**: Standard HTTP status codes are used with detailed error messages in the response body.
5. **Versioning**: APIs are versioned using URL path (e.g., /api/v1/endpoint).
6. **Pagination**: All list endpoints implement cursor-based pagination.
7. **Filtering and Sorting**: Endpoints that return lists support filtering and sorting via query parameters.

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
- External identity provider (Keycloak) integration
- Application validates externally issued tokens
- Internal platform manages roles and permissions

### Frontend Integration
- API Gateway / BFF pattern for Angular frontend
- Centralized auth/session handling at the edge
- Edge-layer controls such as rate limiting, feature gating, and safe fallback shaping should be applied in the BFF or equivalent frontend-facing boundary where appropriate

### Internal Services
- Hybrid REST + async events for internal communication
- REST/JSON for synchronous operations
- Async events for eventual-consistency flows
- Internal integration should preserve idempotency, additive contract evolution, and a clear distinction between source-of-truth writes versus rebuildable read-model updates

### Media and file handling model
- Binary uploads and media retrieval should follow an explicit storage and access model rather than treating media URLs as opaque implementation leftovers
- API contracts should preserve enough media metadata for derivative handling, moderation review, and cleanup workflows

### Runtime feature control model
- AI-assist behavior, rollout-sensitive capabilities, and provider routing may be governed by runtime configuration or lightweight feature controls
- Public contracts should fail safely when a feature is disabled or degraded rather than returning unclear behavior

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
