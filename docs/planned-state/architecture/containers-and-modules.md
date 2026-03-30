---
title: Planned Containers and Modules
status: planned-state
---

# Planned Containers and Modules

This file describes the **target MVP solution topology** for FoodSocial. It does **not** describe implemented runtime components yet.

## System boundary

The MVP should be built as a web-based system with a clear separation between:

- frontend presentation and interaction logic
- backend application and domain logic
- relational data storage
- binary media storage

## Primary containers

### 1. `frontend-web`

**Technology**

- Angular latest
- TailwindCSS

**Responsibility**

- application shell and navigation
- authentication state on the client
- location permission capture
- create-post flow
- nearby feed and search experience
- post detail rendering
- AI assist input and result presentation

**Notes**

- This container should remain thin in business logic.
- Backend remains the source of truth for authorization, filtering, ranking, and AI-grounded results.

### 2. `backend-api`

**Technology**

- .NET 10
- ASP.NET Core

**Responsibility**

- expose API endpoints used by the frontend
- own business logic, validation, authorization, and persistence orchestration
- organize the MVP around domain modules that can later become services

**Deployment recommendation**

- one backend deployable for the MVP
- internal modular boundaries instead of multiple independently deployed services

### 3. `postgres`

**Technology**

- PostgreSQL
- PostGIS extension

**Responsibility**

- system of record for transactional data
- geospatial filtering for nearby discovery
- lightweight text search and feed-query support

### 4. `object-storage`

**Technology options**

- MinIO for development
- S3-compatible or Azure Blob storage for production

**Responsibility**

- store uploaded image files
- separate media binaries from transactional records

## MVP exclusions from the initial topology

The following should remain out of scope for the first architecture slice unless clear pressure appears:

- separate microservice deployables
- message bus as a mandatory dependency
- Elasticsearch
- vector database
- dedicated search cluster
- dedicated AI service runtime

## Backend module shape

The backend should be implemented as a **modular monolith** whose internal boundaries mirror future service seams.

### Identity module

**Purpose**

- registration and login
- token issuance and session lifecycle
- user profile basics

**Owns data such as**

- `users`
- `refresh_tokens`
- `user_profiles`

### Posts module

**Purpose**

- create, edit, delete, and view food posts
- store dish details, captions, location metadata, and tags
- associate uploaded images with posts

**Owns data such as**

- `posts`
- `post_images`
- `post_tags`

### Social module

**Purpose**

- comments
- likes
- saved posts, if introduced during MVP expansion

**Owns data such as**

- `comments`
- `post_likes`
- `saved_posts` if needed later

### Discovery module

**Purpose**

- nearby feed queries
- keyword and tag filtering
- simple ranking based on distance, freshness, and engagement

**Owns logic for**

- search and feed composition
- query scoring rules
- delivery of discovery-oriented read models if needed later

**MVP note**

- Discovery may read from post data directly at first.
- It does not need a separate search database in the initial version.

### Media module

**Purpose**

- upload intent creation
- file validation
- object-storage association
- media metadata lifecycle

**Owns data such as**

- media keys and storage references
- media status
- file metadata and ownership

### AI Assist module

**Purpose**

- interpret natural-language food discovery requests
- convert user intent into filters or structured discovery queries
- generate grounded suggestion responses based on FoodSocial post data

**MVP note**

- AI Assist is an orchestration layer, not an autonomous recommendation platform.

## Frontend module shape

The frontend should follow the Angular scaffold already described in `Frontend/README.md`.

### `src/app/core/`

Planned responsibilities:

- auth session state
- route guards
- HTTP interceptors and API clients
- environment configuration
- app bootstrap concerns
- location permission and device context helpers

### `src/app/shared/`

Planned responsibilities:

- reusable post cards
- avatar and profile UI pieces
- tag chips
- image upload widgets
- form helpers
- loading and empty states

### `src/app/features/`

Planned feature areas:

- `auth`
- `feed-nearby`
- `post-create`
- `post-detail`
- `profile`
- `ai-assist`

## Data ownership rules

To stay microservice-ready without deploying many services on day 1, the MVP should follow these rules:

1. Each backend module owns its own write logic.
2. Modules should not depend on direct internal table manipulation by other modules.
3. Cross-module collaboration should happen through interfaces, application services, or domain events.
4. Discovery may use read-oriented projections later, but the transactional source remains owned by the originating module.

## Container responsibility summary

| Container | Primary role | Owns truth for |
|---|---|---|
| `frontend-web` | UI and client interaction | presentation state only |
| `backend-api` | business logic and APIs | domain behavior and authorization |
| `postgres` | relational persistence | transactional records |
| `object-storage` | binary file storage | image binaries |

## Risks and deferred decisions

- Whether image processing stays inline or moves to a worker later.
- Whether comments remain flat or gain nested replies later.
- Whether saved posts are included in the first release or follow shortly after.
- Whether AI assist launches as a dedicated screen or as a smart query surface inside discovery.

## Sources

- `Backend/README.md`
- `Frontend/README.md`
