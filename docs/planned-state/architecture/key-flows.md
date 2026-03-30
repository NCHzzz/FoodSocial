---
title: Planned Key Flows
status: planned-state
---

# Planned Key Flows

These flows describe the **expected MVP behavior**. They should be treated as architecture targets, not as proof of implemented product behavior.

## How to read these flows

- Each flow focuses on the shortest useful product path.
- The steps reflect responsibility boundaries between frontend, backend, database, and storage.
- Deferred optimizations are intentionally omitted.

## Flow 1 — User enters the app and reaches the core experience

### Goal

Help the user land quickly in a nearby food discovery experience.

### Planned sequence

1. User opens the Angular web app.
2. Frontend checks session state.
3. If needed, user authenticates or continues into a limited anonymous entry point if that becomes a product decision later.
4. Frontend requests location permission.
5. Frontend calls the discovery endpoint with user location and default feed parameters.
6. Backend validates request and queries discovery logic.
7. Discovery uses PostgreSQL and PostGIS to retrieve nearby food posts.
8. Frontend renders the nearby feed.

### Expected result

The user sees a list of relevant food posts near them with enough context to decide what to inspect next.

## Flow 2 — User creates a food post with image and location

### Goal

Allow users to contribute new food content that becomes part of the discovery system.

### Planned sequence

1. Authenticated user enters the create-post screen.
2. Frontend collects dish name, caption, tags, price hints if present, location, and image selection.
3. Frontend asks backend for upload intent or signed upload details.
4. Backend validates the upload request and returns upload metadata.
5. Frontend uploads the image to object storage.
6. Frontend submits the post create request with metadata and uploaded image reference.
7. Backend validates business rules and creates the post.
8. Post metadata is stored in PostgreSQL.
9. The new post becomes eligible for discovery queries.

### Expected result

The system stores a new food post with stable references to image and location data.

## Flow 3 — User opens post detail and interacts socially

### Goal

Support lightweight social engagement around a discovered food item.

### Planned sequence

1. User opens a post from the feed.
2. Frontend requests post detail, media references, social summary, and current interaction state.
3. Backend composes the post detail response from relevant modules.
4. Frontend renders images, dish information, location context, and comments.
5. User likes the post or adds a comment.
6. Frontend calls the social endpoint.
7. Backend validates auth and updates social records.
8. Frontend refreshes or optimistically updates the interaction state.

### Expected result

Users can react to and discuss posts without turning the MVP into a heavy social graph product.

## Flow 4 — User asks AI for food suggestions

### Goal

Let the user express discovery intent in natural language without replacing the underlying discovery model.

### Planned sequence

1. User enters a natural-language request such as wanting a spicy dinner nearby.
2. Frontend sends the request and relevant context to the backend.
3. Backend retrieves candidate posts using discovery rules and contextual filters.
4. AI Assist converts the user request plus retrieved post context into a grounded prompt.
5. The LLM returns a suggestion response tied to FoodSocial post data.
6. Backend returns the suggestion text along with cited post cards.
7. Frontend renders a result that the user can inspect and act on.

### Expected result

AI improves query expression and result explanation, while FoodSocial post data remains the foundation of the answer.

## Cross-cutting concerns in all flows

### Authentication and authorization

- Create-post, comments, likes, and profile updates require authentication.
- Ownership rules apply to post edits and deletes.

### Validation

- Input validation should happen at API boundaries.
- File type and size should be validated before accepting uploads.
- Latitude and longitude should be validated as structured values, not free text.

### Error handling

- Failures should return consistent API error shapes.
- Frontend should be able to present recoverable user-facing messages.

### Observability

- Requests should be traceable through logs and correlation identifiers.
- AI and upload paths should emit enough telemetry for debugging and cost awareness.

## Deferred post-MVP flows

- follow graph and social feed personalization
- push notifications
- advanced moderation workflows
- image enrichment pipelines
- semantic or hybrid search flows
- deep recommendation profiles

## Sources

- `Backend/README.md`
- `Frontend/README.md`
