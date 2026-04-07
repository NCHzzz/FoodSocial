# Local Keycloak bootstrap

This folder contains local-development Keycloak assets for the Aspire AppHost.

## Realm import directory

Place exported realm JSON files in `realms/`.

Recommended first file name:

- `foodsocial-realm.json`

The AppHost uses `WithRealmImport("./keycloak/realms")`, so any valid realm export JSON placed there will be imported when the local Keycloak container starts.

## Local development expectations

- Keycloak is treated as import-driven local state for development
- the realm JSON in `realms/` is the source of truth for local bootstrap
- if you change local clients, scopes, or demo users, restart AppHost so Keycloak comes up from the updated realm import

## Why no data volume is used for local Keycloak

For this repository, deterministic local bootstrap matters more than preserving ad-hoc Keycloak admin-console changes.

Using a Keycloak data volume caused the existing realm to persist and later imports to be skipped, which made client/bootstrap changes appear not to work.

For local development, this repo now prefers:

- fresh Keycloak state from the realm import
- explicit realm JSON under source control
- repeatable auth bootstrap on restart
