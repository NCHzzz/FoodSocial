---
title: Planned MVP Database Schema
status: planned-state
---

# Planned MVP Database Schema

This file describes the **planned relational schema** for the FoodSocial MVP. It is a design target for implementation planning and should **not** be read as proof that these tables already exist.

## Schema goals

The MVP schema should support:

- account and profile management
- food posts with images, tags, and location data
- nearby discovery queries
- lightweight social interactions
- AI request logging if that becomes useful in the first release

## Database posture

Planned MVP storage:

- one PostgreSQL database
- PostGIS extension enabled
- binary media stored outside the database in object storage
- relational records hold metadata, ownership, and references

## Schema ownership model

The MVP can use either one shared schema or logical schemas by module. The recommended planned-state layout is:

- `identity`
- `posts`
- `social`
- `ai`

This keeps domain ownership explicit without forcing separate databases too early.

## Identity tables

### `identity.users`

Core user account record.

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | primary key |
| `email` | text | unique, normalized |
| `password_hash` | text | nullable only if social auth is introduced later |
| `status` | text | active, disabled, pending, or similar |
| `created_at_utc` | timestamptz | required |
| `updated_at_utc` | timestamptz | required |

Suggested indexes:

- unique index on normalized email

### `identity.user_profiles`

User-facing profile data.

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `user_id` | uuid | primary key, foreign key to `identity.users.id` |
| `display_name` | text | required |
| `avatar_media_id` | uuid | nullable, can reference media metadata if that path is chosen |
| `bio` | text | optional |
| `created_at_utc` | timestamptz | required |
| `updated_at_utc` | timestamptz | required |

### `identity.refresh_tokens`

Session continuation records.

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | primary key |
| `user_id` | uuid | foreign key |
| `token_hash` | text | required |
| `expires_at_utc` | timestamptz | required |
| `revoked_at_utc` | timestamptz | nullable |
| `created_at_utc` | timestamptz | required |

## Posts tables

### `posts.posts`

Primary food post entity.

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | primary key |
| `author_user_id` | uuid | foreign key to `identity.users.id` |
| `dish_name` | text | required |
| `caption` | text | optional but recommended |
| `address_text` | text | required |
| `latitude` | numeric or double precision | required if geography column is not used directly |
| `longitude` | numeric or double precision | required if geography column is not used directly |
| `location_point` | geography(Point, 4326) | recommended with PostGIS |
| `price_level` | smallint | nullable, constrained to a small range such as 1..4 |
| `status` | text | active, hidden, deleted |
| `created_at_utc` | timestamptz | required |
| `updated_at_utc` | timestamptz | required |
| `deleted_at_utc` | timestamptz | nullable for soft delete |

Suggested indexes:

- index on `author_user_id`
- index on `created_at_utc desc`
- GiST index on `location_point`
- optional text-search index for dish name and caption

### `posts.post_media`

Associates uploaded media with posts.

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | primary key |
| `post_id` | uuid | foreign key to `posts.posts.id` |
| `media_id` | uuid | references media metadata record |
| `sort_order` | integer | required |
| `is_primary` | boolean | default false |
| `created_at_utc` | timestamptz | required |

Suggested indexes:

- index on `post_id`
- unique partial index for one primary image per post if desired

### `posts.media_assets`

Stores metadata about uploaded files in object storage.

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | primary key |
| `owner_user_id` | uuid | foreign key to `identity.users.id` |
| `object_key` | text | unique |
| `content_type` | text | required |
| `size_bytes` | bigint | required |
| `storage_status` | text | pending, uploaded, failed, attached |
| `created_at_utc` | timestamptz | required |
| `updated_at_utc` | timestamptz | required |

Suggested indexes:

- unique index on `object_key`
- index on `owner_user_id`
- index on `storage_status`

### `posts.post_tags`

Tag assignments for posts.

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `post_id` | uuid | foreign key |
| `tag` | text | required |
| `created_at_utc` | timestamptz | required |

Suggested indexes:

- composite primary key or unique index on (`post_id`, `tag`)
- index on `tag`

## Social tables

### `social.comments`

Flat comment model for MVP.

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | primary key |
| `post_id` | uuid | foreign key to `posts.posts.id` |
| `author_user_id` | uuid | foreign key to `identity.users.id` |
| `content` | text | required |
| `status` | text | active, hidden, deleted |
| `created_at_utc` | timestamptz | required |
| `updated_at_utc` | timestamptz | required |
| `deleted_at_utc` | timestamptz | nullable |

Suggested indexes:

- index on `post_id, created_at_utc`
- index on `author_user_id`

### `social.post_likes`

Tracks which users liked which posts.

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `post_id` | uuid | foreign key |
| `user_id` | uuid | foreign key |
| `created_at_utc` | timestamptz | required |

Suggested indexes:

- composite primary key or unique index on (`post_id`, `user_id`)
- index on `user_id`

### Optional later MVP expansion: `social.saved_posts`

This table should be added only if saved posts are included in the first public feature set.

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `post_id` | uuid | foreign key |
| `user_id` | uuid | foreign key |
| `created_at_utc` | timestamptz | required |

## AI logging tables

### `ai.query_logs`

Optional telemetry table for AI request review and improvement.

Suggested columns:

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | primary key |
| `user_id` | uuid | nullable if anonymous AI use is allowed |
| `raw_query` | text | required |
| `applied_filters_json` | jsonb | optional |
| `response_summary` | text | optional |
| `created_at_utc` | timestamptz | required |

This table is useful for iteration, but it is not mandatory for the MVP launch if delivery speed is more important.

## Discovery read model guidance

The initial version should query directly from `posts.posts` plus related tables. A dedicated discovery table is **not required** on day 1.

If query complexity grows, a read-oriented structure can be introduced later, for example:

- `posts.discovery_cards`

Possible later columns:

- post identifiers
- primary image url
- denormalized tag array
- precomputed like/comment counts
- searchable text document
- location point

This should be treated as a future optimization, not an MVP prerequisite.

## Relationship summary

- one `identity.users` row can own one `identity.user_profiles` row
- one `identity.users` row can author many `posts.posts`
- one `posts.posts` row can reference many `posts.post_media`
- one `posts.posts` row can have many `posts.post_tags`
- one `posts.posts` row can have many `social.comments`
- one `posts.posts` row can have many `social.post_likes`
- one `identity.users` row can own many `posts.media_assets`

## Data quality and constraint notes

- `dish_name` should be required and length-limited
- `content` for comments should be required and length-limited
- latitude and longitude should stay consistent with `location_point`
- tag values should be normalized before persistence
- soft-delete states should be represented explicitly instead of physically removing records by default

## Privacy and security notes

- Object storage should not rely on permanently public raw file paths by default if signed access is preferred later.
- Refresh tokens should be stored hashed, not in plain text.
- AI logs should avoid storing sensitive data beyond what is needed for product iteration and debugging.

## Decisions intentionally deferred

- exact id generation strategy
- whether to use multiple logical schemas or one shared schema physically
- whether location is stored primarily as lat/lng columns or only as PostGIS geography plus computed projections
- whether counters are materialized in the post row or aggregated at query time
- whether comments remain flat or gain reply support later

## Sources

- `docs/planned-state/architecture/README.md`
- `docs/planned-state/architecture/containers-and-modules.md`
- `docs/planned-state/architecture/key-flows.md`
- `docs/planned-state/architecture/evolution-path.md`
- `Backend/README.md`
- `Frontend/README.md`
