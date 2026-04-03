# 02_personas_and_roles.md

## Status

- Working draft

## User Personas

### 1. Food Sharer / Reviewer

The main early-content persona.

#### Goals

- Share dishes, restaurants, and food experiences
- Build visibility for useful food content
- Help others discover good food

#### Needs

- Easy post creation
- Support for food photos, descriptions, and location tagging
- Social feedback through likes, comments, saves, and shares

### 2. Food Explorer

The main consumption and discovery persona.

#### Goals

- Find what to eat
- Discover good restaurants and dishes nearby or in a target location
- Browse trusted reviews and recommendations

#### Needs

- Discovery feed
- Search by dish, restaurant, and location
- Save content for later
- Follow creators with similar taste

### 3. Recipe-Oriented User / Home Cook

Currently a secondary persona in MVP.

#### Goals

- Share recipes
- Discover cooking ideas from the community

#### Needs

- Recipe posting support
- Structured recipe content with ingredients, steps, and media

## Role Priority for MVP

1. Food sharers / reviewers
2. Food explorers
3. Recipe-oriented users / home cooks

## Roles

### Guest (Assumption)

- Browse public content such as basic feed and post views
- Restricted from deeper authenticated actions

### Registered User

- Create posts
- Like, comment, save, share
- Search
- Follow users
- Manage profile
- Report content

#### Profile direction

- MVP profile should show posts, saved items, follow counts, and a basic bio or taste profile
- Profile design should preserve a clear path toward richer activity and recommendation-signal views later
- MVP may allow users to edit a basic taste profile manually
- Later profile evolution may combine manual taste preferences with AI-inferred taste understanding
- MVP manual taste profile should include cuisine or tag preferences plus dislike/avoid preferences
- Later evolution may add richer taste attributes such as spice level or flavor preferences

#### Identity direction

- authenticated through an external identity provider
- application profile data remains owned by the platform
- identity mapping and profile ownership should not be treated as the same backend concern

### Admin / Moderator

- Review reports
- Moderate users and content
- Handle abuse / seeding / spam

## Permissions Overview

### Guest

- View basic public feed content
- View basic post details
- Browse public profiles at a limited level (Assumption)
- Cannot post, like, comment, save, follow, report, or manage profile data

### MVP visibility direction

- basic feed, post, and limited profile content should be public in MVP
- private accounts or private posts are not part of the current MVP direction

### Registered User

- Create and manage posts
- Like, comment, save, share, and repost content
- Follow users
- View and manage saved items
- Edit basic profile and taste preferences
- Report posts, users, and comments
- View notification center

### Admin / Moderator

- Review moderation queue
- Dismiss reports
- Hide or delete reported content
- Warn users
- Suspend or restrict users
- Review moderation history and audit data

## Key Needs by Role

### Registered User

- food-centric social posting
- fast discovery
- trusted recommendations

### Admin / Moderator

- content reporting workflow
- moderation queue
- policy enforcement tools

## Need Confirmation

- Whether restaurant owners or business accounts exist in MVP
- Whether creators have special profile features in MVP
