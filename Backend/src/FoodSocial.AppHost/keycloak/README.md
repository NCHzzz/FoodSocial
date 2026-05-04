# Local Keycloak bootstrap

This folder contains local-development Keycloak assets for the Aspire AppHost.

## Realm import directory

Place exported realm JSON files in `realms/`.

Recommended first file name:

- `foodsocial-realm.json`

The AppHost uses `WithRealmImport("./keycloak/realms")`, so any valid realm export JSON placed there will be imported when the local Keycloak container starts.

The AppHost does not pin Keycloak to host port `8080`. Use the Aspire Dashboard endpoint shown for the `keycloak` resource when opening the Keycloak UI locally; this avoids collisions with other local services that commonly use port 8080.

## Local development expectations

- Keycloak is treated as import-driven local state for development
- the realm JSON in `realms/` is the source of truth for local bootstrap
- if you change local clients, scopes, or demo users, restart AppHost so Keycloak comes up from the updated realm import

## Keycloak container persistence and reset behavior

For this repository, deterministic local bootstrap matters more than preserving ad-hoc Keycloak admin-console changes.

Using a Keycloak data volume caused the existing realm to persist and later imports to be skipped, which made client/bootstrap changes appear not to work.

For local development, this repo prefers:

- a persistent Aspire-managed Keycloak container with the stable Docker name `foodsocial-keycloak` for faster AppHost restarts
- no Keycloak data volume, so removing the persistent container returns Keycloak to fresh realm-import state
- explicit realm JSON under source control
- repeatable auth bootstrap on restart

If the checked-in realm import changes and the local Keycloak container still reflects old state, remove the `foodsocial-keycloak` container from Docker and start the AppHost again.

PostgreSQL, RabbitMQ, and Redis use stable Aspire container names plus named data volumes because their development state is not import-driven in the same way:

- `foodsocial-postgres` with `foodsocial-postgres-data`
- `foodsocial-rabbitmq` with `foodsocial-rabbitmq-data`
- `foodsocial-redis` with `foodsocial-redis-data`
