# Local Auth and Identity Bootstrap Runbook

> Status: active
> Date: 2026-04-07
> Scope: local development validation for Keycloak, Identity API auth, and Identity persistence bootstrap

## Goal

Provide the exact local-development steps needed to verify the Keycloak-driven auth bootstrap and Identity persistence setup before business logic work begins.

## Preconditions

- .NET 10 SDK installed
- Docker Desktop or equivalent local container runtime running
- PostgreSQL, RabbitMQ, Redis, and Keycloak managed through Aspire AppHost

## 1. Start the AppHost

Run:

```bash
dotnet run --project Backend/src/FoodSocial.AppHost/FoodSocial.AppHost.csproj
```

Expected outcome:

- Aspire dashboard starts
- Keycloak starts as an Aspire-managed container
- Identity API and BFF start successfully

## 2. Find the current Keycloak and PostgreSQL host ports

Aspire-managed containers may expose dynamic host ports for manual local access.

Service-side auth uses the Aspire-injected Keycloak service reference by default. Manual curl and browser inspection use the current host-mapped ports below.

Run:

```bash
docker ps --format "table {{.Names}}\t{{.Ports}}"
```

Identify:

- the Keycloak container host port mapped to container port `8080`
- the PostgreSQL container host port mapped to container port `5432`

Use those mapped host ports in the manual commands below.

## 3. Verify Keycloak realm import

The local realm import file lives at:

- `Backend/src/FoodSocial.AppHost/keycloak/realms/foodsocial-realm.json`

Verify in the Keycloak admin UI:

- realm `foodsocial` exists
- client `foodsocial-bff` exists
- client `foodsocial-identity-api` exists
- client `foodsocial-cli` exists
- user `demo-user` exists

## 4. Browser login flow through the BFF

The FoodSocial architecture keeps browser-facing auth at the BFF edge.

Start the login flow in a browser:

```text
http://localhost:5276/api/auth/login
```

Expected outcome:

- Keycloak login page appears
- after successful login, the browser is redirected back to the BFF
- a BFF auth cookie is created

## 5. Verify the BFF session endpoint

Call:

```bash
curl "http://localhost:5276/api/auth/session" \
  --cookie-jar cookies.txt \
  --cookie cookies.txt
```

Expected outcome:

- authenticated session returns the current user payload

## 6. Get a test access token for API-only validation

Replace `<keycloak-port>` with the mapped host port from the previous step, then run:

```bash
curl -X POST "http://localhost:<keycloak-port>/realms/foodsocial/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=foodsocial-cli" \
  -d "grant_type=password" \
  -d "username=demo-user" \
  -d "password=Pass@word1"
```

Expected outcome:

- response contains `access_token`

## 7. Validate the Identity auth boundary

Call the bootstrap endpoint with the token:

```bash
curl "http://localhost:5147/identity/current-user" \
  -H "Authorization: Bearer <access_token>"
```

Expected outcome:

- response is HTTP 200
- response contains the Keycloak subject, preferred username, and email

## 8. Create the first Identity migration

If `dotnet-ef` is not installed globally, install it first:

```bash
dotnet tool install --global dotnet-ef
```

Then create the migration:

```bash
dotnet ef migrations add InitialIdentityBootstrap \
  --project Backend/src/Identity/Infrastructure/FoodSocial.Identity.Infrastructure.csproj \
  --startup-project Backend/src/Identity/Api/FoodSocial.Identity.Api.csproj \
  --output-dir Persistence/Migrations
```

Expected outcome:

- migration files appear under `Identity/Infrastructure/Persistence/Migrations`

## 9. Apply the migration

Replace `<postgres-port>` and `<postgres-password>` with the mapped host port and password for the running Aspire PostgreSQL container, then export the connection string first:

```bash
ConnectionStrings__identitydb='Host=localhost;Port=<postgres-port>;Database=identitydb;Username=postgres;Password=<postgres-password>' \
dotnet ef database update \
  --project Backend/src/Identity/Infrastructure/FoodSocial.Identity.Infrastructure.csproj \
  --startup-project Backend/src/Identity/Api/FoodSocial.Identity.Api.csproj
```

Expected outcome:

- `identitydb` contains the first `identity_users` table

## 10. Rebuild the affected projects

Run:

```bash
dotnet build Backend/src/FoodSocial.Bff/FoodSocial.Bff.csproj
dotnet build Backend/src/Identity/Api/FoodSocial.Identity.Api.csproj
dotnet build Backend/src/FoodSocial.AppHost/FoodSocial.AppHost.csproj
```

Expected outcome:

- all three builds succeed

## 11. Update current-state docs after verification

After local validation is complete:

- update `docs/context/current_build_status.md`
- append a milestone entry to `docs/context/progress_log.md`

## What is still intentionally out of scope

This runbook does not yet cover:

- full identity provisioning logic
- BFF token forwarding or orchestration endpoints
- messaging consumers or outbox patterns
- business-domain behavior for profile creation
