# 08_database_design.md

## Status

- Working draft

## Proposed Tables

These tables represent a logical data model and service-owned storage direction.

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

Food-first field modeling may start in `posts` and move to a more specialized detail table only if content separation proves necessary.

Recipe should remain a structured post type in MVP rather than a separate platform module.

Because `Place` and `Dish` are first-class MVP entities, the schema should support canonical storage and post linkage for both from the start.

Service-owned persistence should keep these models within the owning service boundary.

## Primary Keys / Foreign Keys

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

Important post fields for MVP:

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

Important place fields:

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

Important dish fields:

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

- soft delete for posts and comments
- optional soft delete for places if admin reversibility is needed
- audit fields for moderation-sensitive tables

## Initial Notes

This file should stay focused on logical schema direction, keys, and indexes.
