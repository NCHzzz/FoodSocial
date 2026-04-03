# 08_database_design.md

## Status

- Working draft

## Proposed Tables

Initial recommended direction for MVP:

These tables represent a logical data model and service-owned storage direction, not a single shared physical database schema for all services.

- tenants
- users
- profiles
- posts
- dishes
- places
- post_media
- comments
- likes
- saved_posts
- saved_dishes
- saved_places
- follows
- reports

Possible MVP approach for food-first fields:

- keep core food fields in `posts` for speed of implementation, or
- use a `food_post_details` table if content-type separation becomes clearer

Recipe direction in MVP:

- keep recipe as a structured post type
- support fields for ingredients and steps without introducing a fully separate recipe platform module yet

Because `Place` is a first-class entity in MVP, the schema should support canonical place storage and post-to-place linkage from the start.

Because `Dish` is also a first-class entity in MVP, the schema should support canonical dish storage and post-to-dish linkage from the start.

Service-owned persistence should keep these models within the owning service boundary and use projections or replicated read models where cross-domain reads are needed.

## Primary Keys / Foreign Keys

Recommended direction:

- `users.tenant_id -> tenants.id`
- `profiles.tenant_id -> tenants.id`
- `posts.author_id -> users.id`
- `posts.tenant_id -> tenants.id`
- `posts.dish_id -> dishes.id` (dish should generally resolve canonically at publish time)
- `posts.place_id -> places.id` (nullable only if unresolved place flow is allowed)
- `post_media.post_id -> posts.id`
- `comments.post_id -> posts.id`
- `comments.author_id -> users.id`
- `likes.post_id -> posts.id`
- `likes.user_id -> users.id`
- `saved_posts.post_id -> posts.id`
- `saved_posts.user_id -> users.id`
- `saved_dishes.dish_id -> dishes.id`
- `saved_dishes.user_id -> users.id`
- `saved_places.place_id -> places.id`
- `saved_places.user_id -> users.id`
- `follows.follower_id -> users.id`
- `follows.following_id -> users.id`
- `reports.reporter_id -> users.id`

## Important Fields

Recommended important post fields for MVP:

- id
- tenant_id
- author_id
- content_type
- primary_media_url
- caption
- dish_id
- place_id
- restaurant_name_fallback
- location_text
- rating
- price_range
- created_at
- updated_at

Recommended important place fields:

- id
- tenant_id
- name
- normalized_name
- address_text
- area
- city
- latitude
- longitude
- status
- created_at
- updated_at

Recommended important dish fields:

- id
- tenant_id
- name
- normalized_name
- aliases
- cuisine_type
- category
- status
- created_at
- updated_at

## Suggested Indexes

Recommended initial indexes:

- `posts(created_at desc)`
- `posts(tenant_id, created_at desc)`
- `posts(content_type, created_at desc)`
- `posts(dish_id, created_at desc)`
- `posts(place_id, created_at desc)`
- `posts(author_id, created_at desc)`
- `dishes(normalized_name)`
- `places(normalized_name)`
- `dishes(tenant_id, normalized_name)`
- `places(tenant_id, normalized_name)`
- `places(city, area)`
- `follows(follower_id, following_id)` unique
- `likes(user_id, post_id)` unique
- `saved_posts(user_id, post_id)` unique
- `saved_dishes(user_id, dish_id)` unique
- `saved_places(user_id, place_id)` unique

## Soft Delete / Audit Fields

Recommended direction:

- soft delete for posts and comments
- optional soft delete for places if admin reversibility is needed
- audit fields for moderation-sensitive tables

## Initial Notes

- The data model will likely need to support mixed content types under a shared post model or a polymorphic content strategy
- Search requirements may influence indexing and denormalization decisions
- First-class place modeling improves long-term discovery but increases MVP scope
- First-class dish modeling further increases MVP scope, but strongly supports the Food-first AI-native direction
