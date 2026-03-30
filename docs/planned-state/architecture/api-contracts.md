---
title: Planned MVP API Contracts
status: planned-state
---

# Planned MVP API Contracts

This file describes the **planned API surface** for the FoodSocial MVP. It is meant to align backend and frontend design work. It should **not** be read as proof that these endpoints already exist in the repository.

## Purpose and scope

The contracts in this file support the MVP capabilities already described in the architecture set:

- authentication and profile bootstrap
- food post creation and retrieval
- nearby discovery
- lightweight social interactions
- AI-assisted food suggestion
- image upload flow through object storage

## API style

### Base path

Planned base path:

- `/api`

### Payload format

- request and response bodies use JSON unless noted otherwise
- timestamps should use ISO 8601 UTC strings
- ids can be UUIDs or another durable opaque identifier, but the choice should stay consistent across modules

### Authentication model

Planned MVP assumption:

- JWT access token for authenticated API calls
- refresh token flow for session continuation
- anonymous read access for discovery can remain an open product decision

### Error model

The MVP should use one consistent error envelope.

Example:

```json
{
  "error": {
    "code": "validation_error",
    "message": "One or more validation errors occurred.",
    "details": [
      {
        "field": "dishName",
        "message": "Dish name is required."
      }
    ],
    "traceId": "00-7f1f..."
  }
}
```

## Common response shapes

### Post card

This shape supports feed, search, and AI result citations.

```json
{
  "id": "post_123",
  "dishName": "Bún bò Huế",
  "caption": "Nước dùng đậm vị, ăn tối rất hợp.",
  "primaryImageUrl": "https://...",
  "addressText": "123 Example Street, District 1",
  "distanceKm": 1.4,
  "tags": ["spicy", "dinner", "budget-friendly"],
  "likeCount": 12,
  "commentCount": 4,
  "createdAt": "2026-03-23T10:00:00Z"
}
```

### Pagination envelope

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "hasMore": true,
  "nextCursor": null
}
```

The MVP may use either page-based or cursor-based pagination, but the implementation should standardize on one approach once chosen.

## Identity endpoints

### `POST /api/auth/register`

Creates a new account.

Request:

```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!",
  "displayName": "Food Lover"
}
```

Response:

```json
{
  "user": {
    "id": "user_123",
    "email": "user@example.com",
    "displayName": "Food Lover",
    "avatarUrl": null
  },
  "accessToken": "jwt",
  "refreshToken": "refresh-token"
}
```

### `POST /api/auth/login`

Authenticates the user.

Request:

```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!"
}
```

Response shape matches register.

### `POST /api/auth/refresh`

Refreshes access credentials.

Request:

```json
{
  "refreshToken": "refresh-token"
}
```

### `POST /api/auth/logout`

Revokes or invalidates the current refresh token/session.

### `GET /api/me`

Returns the current authenticated user profile.

Response:

```json
{
  "id": "user_123",
  "email": "user@example.com",
  "displayName": "Food Lover",
  "avatarUrl": "https://...",
  "createdAt": "2026-03-23T10:00:00Z"
}
```

### `PATCH /api/me`

Updates editable profile fields.

Request:

```json
{
  "displayName": "New Display Name",
  "avatarUrl": "https://..."
}
```

## Media endpoints

### `POST /api/media/upload-intents`

Requests upload authorization and metadata for an image upload.

Request:

```json
{
  "fileName": "bun-bo.jpg",
  "contentType": "image/jpeg",
  "sizeBytes": 5242880
}
```

Response:

```json
{
  "mediaId": "media_123",
  "objectKey": "posts/user_123/media_123.jpg",
  "uploadUrl": "https://...",
  "expiresAt": "2026-03-23T10:15:00Z"
}
```

### `POST /api/media/{mediaId}/complete`

Marks an upload as completed after the frontend successfully pushes the object to storage.

Request:

```json
{
  "objectKey": "posts/user_123/media_123.jpg"
}
```

## Posts endpoints

### `POST /api/posts`

Creates a food post.

Request:

```json
{
  "dishName": "Bún bò Huế",
  "caption": "Đậm vị, nhiều topping.",
  "addressText": "123 Example Street, District 1",
  "latitude": 10.7767,
  "longitude": 106.7009,
  "priceLevel": 2,
  "tags": ["spicy", "dinner"],
  "mediaIds": ["media_123"]
}
```

Response:

```json
{
  "id": "post_123",
  "status": "active",
  "createdAt": "2026-03-23T10:00:00Z"
}
```

### `GET /api/posts/{postId}`

Returns post detail.

Response:

```json
{
  "id": "post_123",
  "author": {
    "id": "user_123",
    "displayName": "Food Lover",
    "avatarUrl": "https://..."
  },
  "dishName": "Bún bò Huế",
  "caption": "Đậm vị, nhiều topping.",
  "addressText": "123 Example Street, District 1",
  "latitude": 10.7767,
  "longitude": 106.7009,
  "priceLevel": 2,
  "tags": ["spicy", "dinner"],
  "images": [
    {
      "mediaId": "media_123",
      "url": "https://...",
      "isPrimary": true
    }
  ],
  "likeCount": 12,
  "commentCount": 4,
  "viewerHasLiked": true,
  "createdAt": "2026-03-23T10:00:00Z"
}
```

### `PATCH /api/posts/{postId}`

Updates an existing post owned by the current user.

### `DELETE /api/posts/{postId}`

Soft-deletes or hides a post owned by the current user.

## Discovery endpoints

### `GET /api/discovery/nearby`

Returns nearby posts using location and optional filters.

Query parameters:

- `lat`
- `lng`
- `radiusKm`
- `q` optional keyword
- `tags` optional repeated or comma-separated values
- `priceLevel` optional
- `page` / `pageSize` or cursor equivalent

Response:

```json
{
  "items": [
    {
      "id": "post_123",
      "dishName": "Bún bò Huế",
      "caption": "Đậm vị, nhiều topping.",
      "primaryImageUrl": "https://...",
      "addressText": "123 Example Street, District 1",
      "distanceKm": 1.4,
      "tags": ["spicy", "dinner"],
      "likeCount": 12,
      "commentCount": 4,
      "createdAt": "2026-03-23T10:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "hasMore": true,
  "nextCursor": null
}
```

### `GET /api/discovery/search`

Performs keyword or filter-based discovery, with the same post-card response shape used by nearby discovery.

## Social endpoints

### `GET /api/posts/{postId}/comments`

Returns comments for a post.

Response:

```json
{
  "items": [
    {
      "id": "comment_123",
      "author": {
        "id": "user_456",
        "displayName": "Noodle Hunter",
        "avatarUrl": "https://..."
      },
      "content": "Quán này ngon thật.",
      "createdAt": "2026-03-23T10:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "hasMore": false,
  "nextCursor": null
}
```

### `POST /api/posts/{postId}/comments`

Creates a comment.

Request:

```json
{
  "content": "Quán này ngon thật."
}
```

### `DELETE /api/comments/{commentId}`

Deletes a comment owned by the current user or hidden through moderation rules when those rules exist later.

### `POST /api/posts/{postId}/likes`

Adds a like from the current user.

### `DELETE /api/posts/{postId}/likes`

Removes a like from the current user.

## AI Assist endpoints

### `POST /api/ai/suggestions`

Returns AI-assisted food suggestions grounded in FoodSocial post data.

Request:

```json
{
  "query": "Tối nay muốn ăn món cay gần quận 1, giá vừa phải.",
  "latitude": 10.7767,
  "longitude": 106.7009,
  "radiusKm": 5
}
```

Response:

```json
{
  "summary": "Bạn có thể thử vài món cay gần khu vực hiện tại.",
  "appliedFilters": {
    "tags": ["spicy"],
    "priceLevel": 2,
    "radiusKm": 5
  },
  "results": [
    {
      "id": "post_123",
      "dishName": "Bún bò Huế",
      "caption": "Đậm vị, nhiều topping.",
      "primaryImageUrl": "https://...",
      "addressText": "123 Example Street, District 1",
      "distanceKm": 1.4,
      "tags": ["spicy", "dinner"],
      "likeCount": 12,
      "commentCount": 4,
      "createdAt": "2026-03-23T10:00:00Z"
    }
  ]
}
```

## Authorization notes

- Register, login, and refresh are public endpoints.
- Profile mutation, media upload intent, post creation, comments, and likes require authentication.
- Edit and delete operations should check ownership.
- Admin- or moderation-specific endpoints are intentionally out of scope for the MVP contract set.

## Contract decisions intentionally deferred

- exact pagination strategy
- whether anonymous discovery is allowed
- precise token transport strategy between browser and backend
- whether saves are part of the first public API set
- whether post detail includes comments inline or through a separate endpoint only

## Sources

- `docs/planned-state/architecture/README.md`
- `docs/planned-state/architecture/containers-and-modules.md`
- `docs/planned-state/architecture/key-flows.md`
- `Backend/README.md`
- `Frontend/README.md`
