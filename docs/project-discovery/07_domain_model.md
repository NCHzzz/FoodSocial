# Data Model & Domain Overview

## Status

- Working draft
- Last updated: 2026-04-02

## Overview

This document defines the core entities, their attributes, relationships, and the MVP modeling direction for FoodSocial.

---

### Core Entities (Detailed)

#### User
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

#### Post (base)
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

#### FoodPost (extends Post)
```json
{
  "id": "uuid", // References Post.id
  "dishName": "string",
  "dishId": "uuid", // References Dish.id
  "placeId": "uuid", // References Place.id
  "rating": "integer (1-5)",
  "priceRange": "enum: $ | $$ | $$$ | $$$$",
  "tasteNotes": ["string"]
}
```

#### Place
```json
{
  "id": "uuid",
  "name": "string",
  "description": "string",
  "address": "string",
  "location": {"latitude": "float", "longitude": "float"},
  "cuisineTypes": ["string"],
  "priceRange": "enum: $ | $$ | $$$ | $$$$",
  "operatingHours": {"monday": {"open": "time", "close": "time"}, "tuesday": {"open": "time", "close": "time"}},
  "contact": {"phone": "string", "website": "string", "email": "string"},
  "rating": {"average": "float", "count": "integer"},
  "status": "enum: active | closed | suspended",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

#### Dish
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

#### Recipe (extends Post)
```json
{
  "id": "uuid", // References Post.id
  "servings": "integer",
  "prepTime": "integer (minutes)",
  "cookTime": "integer (minutes)",
  "ingredients": [{"item": "string", "quantity": "string", "unit": "string", "notes": "string"}],
  "instructions": [{"step": "integer", "instruction": "string", "mediaUrl": "string"}],
  "difficulty": "enum: easy | medium | hard",
  "tags": ["string"]
}
```

#### Comment
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

#### Report
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

#### Notification
```json
{
  "id": "uuid",
  "userId": "uuid",
  "type": "enum: like | comment | follow | share | save | system",
  "title": "string",
  "message": "string",
  "data": "json",
  "isRead": "boolean",
  "createdAt": "datetime"
}
```

---

### High‑Level Domain Model (MVP Direction)

- **Dish** and **Place** are first‑class entities from day 1.
- Food posts must reference a canonical `Dish`, and may reference a canonical `Place` when known.
- `Post` is the central content aggregate.
- `User` owns posts, likes, comments, follows, and saved items.
- Tenant isolation is enforced at the service layer rather than as a separate MVP domain entity.
- Aggregates:
  - `User/Profile`
  - `Post` (including `FoodPost`, `RecipePost`)
  - `Dish`
  - `Place`
  - `Moderation` (reports, actions)

---

### Entity Relationships (Mermaid ER Diagram)

```mermaid
erDiagram
    User ||--o{ Post : creates
    User ||--o{ Comment : writes
    User ||--o{ Report : submits
    User ||--o{ Notification : receives
    Post ||--o{ FoodPost : extends
    Post ||--o{ Recipe : extends
    FoodPost }o--|| Dish : references
    FoodPost }o--|| Place : references
    Comment ||--o{ Comment : nested
    Place ||--o{ FoodPost : location
```

---

### Indexing Strategy (summary)

Canonical schema/index detail lives in `08_database_design.md`.

Only the highest-value summary remains here:

- primary keys use UUIDs
- `User.username` and `User.email` are unique
- search indexes matter on `Post.caption`, `Place.name`, and `Dish.name`

---

This is the canonical domain-model document for current product and API-facing entity discussion.
