# 07_domain_model.md

## Status

- Working draft

## Main Entities

- Tenant
- User
- Profile
- Post
- Food Post (primary content type in MVP, exact modeling TBD)
- Restaurant / Place Post (secondary content type, exact modeling TBD)
- Recipe Post (secondary content type, exact modeling TBD)
- Comment
- Like
- Save
- Share
- Repost / Reshare (Need confirmation on final modeling shape)
- Follow
- Report
- Recommendation Signal (Assumption)
- Place (first-class MVP entity)
- Dish (first-class MVP entity)

## Recommended MVP modeling direction

Use a shared `Post` entity with a `contentType` field, plus content-type-specific detail records or optional metadata sections.

Approved MVP direction for places:

- `Place` is a first-class entity from day one
- food posts may reference a canonical `Place`
- place-centric discovery should be built on top of this entity

Approved MVP direction for dishes:

- `Dish` is a first-class entity from day one
- food posts should reference a canonical `Dish` when possible
- dish-centric discovery and recommendation should be built on top of this entity

For a food-first post, recommended MVP fields are:

- `primaryMediaUrl` (required)
- `dishId` (preferred when the dish is known)
- `dishName` (fallback text when a canonical dish is unavailable)
- `caption` (required)
- `placeId` (preferred when the place is known)
- `restaurantName` (fallback text only when a canonical place is unavailable)
- `locationText` or `area` (optional supplemental data)
- `rating` (optional)
- `priceRange` (optional)
- `tags` (optional)

This approach is recommended because it is simple enough for a solo MVP while keeping room for structured discovery.

### Place entity direction

Recommended initial `Place` fields:

- `id`
- `name`
- `normalizedName`
- `addressText`
- `area`
- `city`
- `latitude` (Need confirmation)
- `longitude` (Need confirmation)
- `status`
- `createdAt`
- `updatedAt`

Initial geographic modeling should support Vietnam-first location and place data.

### Dish entity direction

Recommended initial `Dish` fields:

- `id`
- `name`
- `normalizedName`
- `aliases` (Need confirmation)
- `cuisineType` (Need confirmation)
- `category`
- `status`
- `createdAt`
- `updatedAt`

## Relationships

- Tenant 1 -> many Users
- Tenant 1 -> many Profiles
- Tenant 1 -> many Posts
- Tenant 1 -> many Dishes
- Tenant 1 -> many Places
- User 1 -> many Posts
- User 1 -> many Comments
- User many-to-many User through Follow
- Post 1 -> many Comments
- Post 1 -> many Likes
- Post 1 -> many Saves
- Save can target Post, Dish, or Place
- Post many-to-1 Dish (dish link preferred for food posts)
- Post many-to-1 Place (place link preferred for food/place posts)
- Report targets Post, User, or Comment

## Domain Rules

### DR-01 Place is canonical in MVP

If a restaurant or place is known, the system should prefer linking content to a canonical `Place` entity instead of storing only free text.

### DR-02 Unresolved place fallback may still be needed

The system may allow fallback text when a place cannot be matched immediately, but canonical place linking remains the preferred direction.

### DR-03 Place discovery is a product capability

Because `Place` is a first-class entity, the platform can support place-centric search, aggregation, and future recommendation logic.

### DR-04 Dish is canonical in MVP

If a dish is known, the system should prefer linking content to a canonical `Dish` entity instead of storing only free text.

### DR-05 Dish discovery is a primary product capability

Because the product is Food-first and `Dish` is first-class, the platform should support dish-centric search, aggregation, and recommendation.

### DR-06 Tenant isolation is a first-class architectural concern

Because the platform is multi-tenant, domain data and access patterns must respect tenant boundaries across identity, content, discovery, moderation, and analytics.

## Suggested Aggregate Boundaries

### Recommended initial aggregate direction

- `User/Profile` aggregate
  - user identity mapping
  - profile data
  - taste preferences

- `Post` aggregate
  - post core data
  - media references
  - caption/review content
  - engagement counters or summaries (implementation-dependent)

- `Dish` aggregate
  - canonical dish identity
  - naming and normalization metadata

- `Place` aggregate
  - canonical place identity
  - location metadata

- `Moderation` aggregate
  - report record
  - target reference
  - moderation outcome

These boundaries should remain aligned to service ownership and should avoid cross-service transactional coupling.

## Need Confirmation

## Publish-Time Canonicalization Direction

- Dish should be treated more strictly at publish time
- Place may still use fallback text when canonical matching is unavailable

## Need Confirmation

- Whether food-first posting requires mandatory dish tagging in MVP

## Recipe Modeling Direction

- Recipe should exist in MVP as a structured post type
- Basic recipe structure should include ingredients, steps, and media
- Recipe remains secondary to the Food-first discovery model and is not a deep standalone module yet
