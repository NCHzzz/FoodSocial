# 02_personas_and_roles.md

## Status

- Working draft

## User Personas

This file keeps persona and permission framing.

Canonical profile, taste-profile, and auth behavior live in `04_functional_requirements.md` and `11_frontend_structure.md`.

### 1. Food Sharer / Reviewer

Primary early-content persona.

#### Goals

- Share dishes, restaurants, and food experiences
- Build visibility for useful food content
- Help others discover good food

#### Needs

- Easy post creation
- Support for food photos, descriptions, and location tagging
- Social feedback through likes, comments, saves, and shares

### 2. Food Explorer

Primary consumption and discovery persona.

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

Secondary launch persona.

#### Goals

- Share recipes
- Discover cooking ideas from the community

#### Needs

- Recipe posting support
- Structured recipe content with ingredients, steps, and media

## Role Priority for Launch

1. Food sharers / reviewers
2. Food explorers
3. Recipe-oriented users / home cooks

## Roles

This section defines role meaning.

Use `## Permissions Overview` as the canonical permissions view.

### Guest (Assumption)

- Browse public content such as basic feed and post views
- Restricted from deeper authenticated actions

### Registered User

- Create posts
- Like, comment, save, share
- Search
- Follow users
- Block or mute users
- Manage profile
- Report content

Profile and identity direction are canonical in `04_functional_requirements.md` and `11_frontend_structure.md`.

### Admin / Moderator

- Review reports
- Moderate users and content
- Handle abuse / seeding / spam

## Permissions Overview

This is the canonical permissions section for launch role behavior.

### Guest

- View basic public feed content
- View basic post details
- Browse public profiles at a limited level (Assumption)
- Cannot post, like, comment, save, follow, report, or manage profile data

### Launch visibility direction

Public/private visibility direction is canonical in `04_functional_requirements.md`.

### Registered User

- Create and manage posts
- Like, comment, save, share, and repost content
- Follow users
- View and manage saved items
- Edit basic profile and taste preferences
- Report posts, users, and comments
- View notification center
- Control basic profile/content visibility where supported

### Admin / Moderator

- Review moderation queue
- Dismiss reports
- Hide or delete reported content
- Warn users
- Suspend or restrict users
- Review moderation history and audit data
- Monitor moderation backlog and abuse signals

## Key Needs by Role

This section is intentionally brief and only keeps needs not already obvious from persona and permission sections.

### Registered User

- food-centric social posting
- fast discovery
- trusted recommendations

### Admin / Moderator

- content reporting workflow
- moderation queue
- policy enforcement tools

## Need Confirmation

- Whether restaurant owners or business accounts exist at launch
- Whether creators have special profile features at launch
