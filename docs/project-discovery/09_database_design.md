# 09_database_design.md

## Status

- Working draft

## Proposed Tables

These tables represent a logical data model and service-owned storage direction.

- tenants
- users
- profiles
- user_relationships
- posts
- dishes
- places
- post_media
- post_media_uploads
- comments
- likes
- reposts
- saved_posts
- saved_dishes
- saved_places
- reports
- moderation_actions
- notifications
- product_events
- integration_outbox

Food-first field modeling may start in `posts` and move to a more specialized detail table only if content separation proves necessary.

Recipe should remain a structured post type at launch rather than a separate platform module.

Because `Place` and `Dish` are first-class launch entities, the schema should support canonical storage and post linkage for both from the start.

Service-owned persistence should keep these models within the owning service boundary.

## Logical References and Physical Foreign Keys

Because FoodSocial uses service-owned PostgreSQL databases, references across service boundaries are logical identifiers, not physical cross-database foreign keys.

Physical foreign keys should stay inside the owning database:

- `profiles.tenant_id -> tenants.id` when tenant records are stored in the same service boundary
- `post_media.post_id -> posts.id`
- `post_media_uploads.owner_post_id -> posts.id` when attached
- `comments.post_id -> posts.id`
- `likes.post_id -> posts.id`
- `reposts.post_id -> posts.id`
- `reports.target_id -> posts.id` only for Post-owned report targets
- `moderation_actions.report_id -> reports.id`
- `notifications.source_entity_id` should remain typed metadata unless the notification owner has a same-database source table

Logical references should be stored as IDs and validated through service contracts where needed:

- `posts.author_id` references Identity/UserProfile identity
- `posts.dish_id` references Dish
- `posts.place_id` references Place and remains nullable when no canonical place is known
- `saved_posts.post_id` references Post
- `saved_dishes.dish_id` references Dish
- `saved_places.place_id` references Place
- `user_relationships.source_user_id` and `target_user_id` reference UserProfile identities
- `reports.reporter_id`, `moderator_id`, and user-target reports reference Identity/UserProfile identities

## Important Fields

Important post fields for launch:

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
- status
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

Important relationship fields:

- id
- tenant_id
- source_user_id
- target_user_id
- relationship_type
- status
- created_at
- updated_at

Important notification fields:

- id
- tenant_id
- user_id
- type
- source_entity_type
- source_entity_id
- read_at
- created_at

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
- `user_relationships(source_user_id, target_user_id, relationship_type)` unique
- `user_relationships(target_user_id, relationship_type)`
- `likes(user_id, post_id)` unique
- `reposts(user_id, post_id)` unique
- `saved_posts(user_id, post_id)` unique
- `saved_dishes(user_id, dish_id)` unique
- `saved_places(user_id, place_id)` unique
- `notifications(user_id, created_at desc)`
- `reports(status, created_at desc)`
- `integration_outbox(status, created_at)`

## Soft Delete / Audit Fields

- soft delete for posts and comments
- optional soft delete for places if admin reversibility is needed
- audit fields for moderation-sensitive tables
- deletion and retention behavior for relationship, notification, media, and analytics records must follow product safety and compliance policy

## Initial Notes

This file should stay focused on logical schema direction, keys, and indexes.
