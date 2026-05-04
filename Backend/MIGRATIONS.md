# Backend database migrations

FoodSocial uses **service-owned EF Core migrations**. Each service owns its own PostgreSQL schema, `DbContext`, design-time factory, migration files, model snapshot, and migration history table.

## Service map

| Service | DbContext | Migration folder | History table |
| --- | --- | --- | --- |
| Identity | `IdentityDbContext` | `src/Identity/Infrastructure/Persistence/Migrations/` | `__IdentityMigrationsHistory` |
| UserProfile | `UserProfileDbContext` | `src/UserProfile/Infrastructure/Persistence/Migrations/` | `__UserProfileMigrationsHistory` |
| Dish | `DishDbContext` | `src/Dish/Infrastructure/Persistence/Migrations/` | `__DishMigrationsHistory` |
| Place | `PlaceDbContext` | `src/Place/Infrastructure/Persistence/Migrations/` | `__PlaceMigrationsHistory` |
| Post | `PostDbContext` | `src/Post/Infrastructure/Persistence/Migrations/` | `__PostMigrationsHistory` |

## Rules

- Commit migration files and the matching `*DbContextModelSnapshot.cs` with model changes.
- Generate migrations from the owning service boundary only.
- Do not create shared migrations or cross-service database foreign keys.
- Keep external references such as `DishId`, `PlaceId`, `AuthorId`, and `ReporterId` as scalar IDs.
- Keep each service's design-time factory available so EF tooling does not depend on Aspire AppHost startup.

## Commands

Restore the repo-local EF tool from the repository root:

```bash
dotnet tool restore
```

Generate a migration:

```bash
dotnet tool run dotnet-ef migrations add <MigrationName> \
  --project "Backend/src/<Service>/Infrastructure/FoodSocial.<Service>.Infrastructure.csproj" \
  --startup-project "Backend/src/<Service>/Api/FoodSocial.<Service>.Api.csproj" \
  --context FoodSocial.<Service>.Infrastructure.Persistence.<Service>DbContext \
  --output-dir Persistence/Migrations
```

Apply a migration locally, for example Post:

```bash
dotnet tool run dotnet-ef database update \
  --project "Backend/src/Post/Infrastructure/FoodSocial.Post.Infrastructure.csproj" \
  --startup-project "Backend/src/Post/Api/FoodSocial.Post.Api.csproj" \
  --context FoodSocial.Post.Infrastructure.Persistence.PostDbContext
```

Staging and production should apply migrations through the deployment pipeline or another controlled release step.

## WSL fallback package folder fix

If EF tooling in WSL reports a missing Visual Studio fallback package folder such as `C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages`, restore the affected projects with fallback folders cleared, then rerun the EF command:

```bash
dotnet restore "Backend/src/<Service>/Infrastructure/FoodSocial.<Service>.Infrastructure.csproj" /p:RestoreFallbackFolders=
dotnet restore "Backend/src/<Service>/Api/FoodSocial.<Service>.Api.csproj" /p:RestoreFallbackFolders=
```
