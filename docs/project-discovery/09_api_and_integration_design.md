# 09_api_and_integration_design.md

## Status

- Working draft

## Required APIs

Initial required API direction includes:

- create post
- get discovery feed
- search content
- get AI-assisted search suggestions
- get recommendation results
- search dishes
- get dish detail
- search places
- get place detail
- get user profile
- follow / unfollow user
- like / unlike post
- comment on post
- save / unsave post
- report post
- report user
- report comment
- moderation review actions (admin)

### Chosen MVP search direction

- PostgreSQL-first search
- structured retrieval with AI-assisted query understanding
- no heavyweight dedicated search engine in initial MVP

## Suggested Endpoints

Illustrative MVP endpoints:

- `POST /api/posts`
- `GET /api/feed/following`
- `GET /api/feed/discover`
- `GET /api/search`
- `POST /api/search/assist`
- `GET /api/recommendations`
- `GET /api/dishes/search`
- `GET /api/dishes/{dishId}`
- `GET /api/places/search`
- `GET /api/places/{placeId}`
- `GET /api/users/{userId}`
- `POST /api/users/{userId}/follow`
- `DELETE /api/users/{userId}/follow`
- `POST /api/posts/{postId}/likes`
- `DELETE /api/posts/{postId}/likes`
- `POST /api/posts/{postId}/comments`
- `POST /api/posts/{postId}/save`
- `DELETE /api/posts/{postId}/save`
- `POST /api/posts/{postId}/report`
- `POST /api/users/{userId}/report`
- `POST /api/comments/{commentId}/report`
- `GET /api/admin/reports`
- `POST /api/admin/reports/{reportId}/resolve`

Illustrative moderation action outcomes should support:

- dismiss
- hide content
- delete content
- warn user
- suspend or restrict user

MVP note:

- no user-facing appeal API is required in MVP
- moderation data contracts should remain extensible for future appeal workflows

## Request / Response Shapes

Illustrative create food post request:

```json
{
  "contentType": "food",
  "primaryMediaUrl": "string",
  "dishId": "string | null",
  "dishNameFallback": "string | null",
  "caption": "string",
  "placeId": "string | null",
  "restaurantNameFallback": "string | null",
  "locationText": "string | null",
  "rating": 4,
  "priceRange": "$$",
  "tags": ["spicy", "street-food"]
}
```

Illustrative discover feed query parameters:

```text
GET /api/feed/discover?cursor=...&location=...&dish=...&interest=...
```

Illustrative AI-assisted search request:

```json
{
  "query": "spicy noodles near district 1",
  "userContext": {
    "savedTags": ["spicy", "street-food"],
    "location": "District 1"
  }
}
```

Recommendation/explanation direction in MVP:

- short explanation text is allowed in recommendation responses
- explanation should remain simple and human-readable

## Auth Model

Approved direction:

- use an external identity provider
- the application should trust external authentication and map identities into internal platform users/profiles

Chosen provider for MVP:

- self-hosted Keycloak

Initial implications:

- frontend auth flow should integrate with the external provider
- backend services should validate externally issued tokens or session artifacts
- application roles and permissions remain an internal platform concern

Why this provider is recommended:

- self-hosted identity keeps identity control inside the platform boundary
- avoids early managed identity vendor lock-in
- fits better if the product owner explicitly prefers self-hosted control despite added operational cost

### Chosen frontend API interaction style

- API Gateway / BFF

Initial implications:

- Angular should call a frontend-facing API gateway or BFF instead of calling multiple microservices directly
- the gateway/BFF can aggregate feed, search, profile, and recommendation responses for UI use cases
- auth/session handling and token propagation can be centralized more cleanly at the edge

### Chosen internal integration style

- Hybrid REST + async events

Implications:

- use REST/JSON for synchronous request-response interactions that need immediate results
- use async events for eventual-consistency flows such as enrichment, moderation triggers, recommendation signals, and search projection refresh
- do not force all internal communication into synchronous chains

## External Integrations

### Planned / Future

- food delivery partner integration for direct ordering (future phase)

### Chosen AI provider strategy

- support multiple external AI providers
- route provider usage through an internal abstraction or gateway layer
- keep provider details out of public business APIs

### AI Agent Compatibility Direction

The platform may need interfaces that are easy for AI agents to consume or operate against.

Possible implications:

- stable and structured APIs
- machine-readable content metadata
- predictable action contracts for search, retrieval, posting, and engagement
- possible future support for agent tools or MCP-style interfaces (Assumption)

### Approved MVP direction

For MVP, the platform should be designed so external or companion AI agents can reliably:

- search content
- retrieve structured food and place data
- retrieve structured dish and place data
- obtain recommendation-oriented results
- perform safe user-authorized actions if exposed later

MVP exposure model:

- private or controlled-access agent-facing APIs rather than fully public developer APIs

Publish-time entity direction:

- dish identity should be stricter and more canonical at publish time
- place linkage may fall back to text when a canonical match is unavailable

Long-term direction:

- external AI agents may be allowed to perform broader system actions when proper authorization, auditing, and safety controls exist

### Internal integration direction

Recommended internal AI integration pattern:

- a provider-agnostic internal AI service or abstraction
- capability-based adapters such as chat/text generation, structured extraction, and embeddings if later enabled
- deterministic routing and fallback rules rather than complex orchestration in MVP

### Place-centric API implication

Because `Place` is a first-class entity in MVP, APIs should support canonical place retrieval and place-linked posting.

### Dish-centric API implication

Because `Dish` is a first-class entity in MVP, APIs should support canonical dish retrieval and dish-linked posting.

## Webhooks / Async Events

Likely async events or background triggers:

- post published
- dish/place enrichment requested
- recommendation signals updated
- moderation checks requested
- search read model refresh requested

These events are part of the approved Hybrid REST + async events integration model for MVP.

## Need Confirmation

- Whether maps/geolocation APIs are needed in MVP
- Whether AI search assistance should start with query understanding only or also include lightweight reranking or explanation generation
- Whether MVP agent compatibility requires public APIs, private internal APIs, or both
- Whether users must choose a canonical dish before publishing a food post
- Whether users must choose a canonical place before publishing a place-linked post
