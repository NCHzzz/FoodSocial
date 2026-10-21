using FoodSocial.Contracts.Common;
using FoodSocial.Contracts.Dishes;

using FoodSocial.Dish.Application;
using FoodSocial.Dish.Domain;
using FoodSocial.Messaging;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodSocial.Dish.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core implementation of the canonical Dish store.
/// </summary>
public sealed class DishStore(DishDbContext dbContext) : IDishStore
{
    private readonly DishDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task<DishCreationResult> CreateDishAsync(DishRecord dish, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dish);

        var existing = await FindByNormalizedNameAsync(dish.NormalizedName, cancellationToken);
        if (existing is not null)
        {
            return new DishCreationResult(existing, Created: false);
        }

        _dbContext.Dishes.Add(dish);
        _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(dish, "Created"));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();
            var duplicate = await FindByNormalizedNameAsync(dish.NormalizedName, cancellationToken);
            if (duplicate is null)
            {
                throw;
            }

            return new DishCreationResult(duplicate, Created: false);
        }

        _dbContext.ChangeTracker.Clear();
        var created = await GetDishAsync(dish.Id, cancellationToken)
            ?? throw new InvalidOperationException("The created dish could not be reloaded.");
        return new DishCreationResult(created, Created: true);
    }

    /// <inheritdoc />
    public Task<DishRecord?> GetDishAsync(Guid dishId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(dishId, Guid.Empty);

        return CurrentTenantDishes()
            .AsNoTracking()
            .SingleOrDefaultAsync(dish => dish.Id == dishId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DishRecord>> SearchDishesAsync(DishSearchCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = ApplyStructuredFilters(CurrentTenantDishes().AsNoTracking(), criteria);

        var normalizedQuery = criteria.NormalizedQuery;
        if (normalizedQuery is not null)
        {
            query = IsNpgsqlProvider()
                ? ApplyPostgresTextSearch(query, normalizedQuery)
                : ApplySqliteTextSearch(query, normalizedQuery);
        }

        return await query
            .OrderBy(dish => dish.Name)
            .ThenBy(dish => dish.Id)
            .Take(criteria.Limit)
            .ToArrayAsync(cancellationToken);
    }

    private static IQueryable<DishRecord> ApplyStructuredFilters(IQueryable<DishRecord> query, DishSearchCriteria criteria)
    {
        if (!criteria.IncludeDeprecated)
        {
            query = query.Where(dish => dish.Status == DishLifecycleStatus.Active);
        }

        if (criteria.Category is not null)
        {
            query = query.Where(dish => dish.Category != null && dish.Category.ToLower() == criteria.Category.ToLower());
        }

        if (criteria.CuisineType is not null)
        {
            query = query.Where(dish => dish.CuisineType != null && dish.CuisineType.ToLower() == criteria.CuisineType.ToLower());
        }

        return query;
    }

    /// <inheritdoc />
    public async Task<DishRecord?> DeprecateDishAsync(Guid dishId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(dishId, Guid.Empty);

        var dish = await CurrentTenantDishes().SingleOrDefaultAsync(candidate => candidate.Id == dishId, cancellationToken);
        if (dish is null)
        {
            return null;
        }

        var previousStatus = dish.Status;
        dish.Deprecate();
        if (previousStatus != dish.Status)
        {
            _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(dish, "Deprecated"));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.ChangeTracker.Clear();

        return await GetDishAsync(dishId, cancellationToken);
    }

    private Task<DishRecord?> FindByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken) =>
        CurrentTenantDishes()
            .AsNoTracking()
            .SingleOrDefaultAsync(dish => dish.NormalizedName == normalizedName, cancellationToken);

    private IQueryable<DishRecord> CurrentTenantDishes() =>
        _dbContext.Dishes.Where(dish => dish.TenantId == TenantDefaults.Id);

    private static IntegrationOutboxMessage CreateOutboxMessage(DishRecord dish, string changeType)
    {
        var integrationEvent = DishCatalogChangedIntegrationEvent.Create(
            dish.Id,
            dish.TenantId,
            changeType,
            dish.Name,
            dish.Status.ToString());

        return IntegrationOutboxMessage.Create(
            integrationEvent,
            integrationEvent.EventId,
            integrationEvent.EventVersion,
            integrationEvent.OccurredAtUtc,
            integrationEvent.CorrelationId,
            "dish.events",
            $"dish.catalog.{changeType.ToLowerInvariant()}.v1",
            dish.Id,
            "Dish");
    }

    private static IQueryable<DishRecord> ApplySqliteTextSearch(IQueryable<DishRecord> query, string normalizedQuery)
    {
        var aliasToken = $"%|{normalizedQuery}%";
        return query.Where(dish =>
            dish.NormalizedName.Contains(normalizedQuery)
            || EF.Functions.Like(dish.AliasesSearchText, aliasToken));
    }

    private static IQueryable<DishRecord> ApplyPostgresTextSearch(IQueryable<DishRecord> query, string normalizedQuery)
    {
        var escaped = EscapeLikePattern(normalizedQuery);
        var namePattern = $"%{escaped}%";
        var aliasPattern = $"%|{escaped}%";
        return query.Where(dish =>
            EF.Functions.ILike(dish.NormalizedName, namePattern, "\\")
            || EF.Functions.ILike(dish.AliasesSearchText, aliasPattern, "\\")
            || EF.Functions.TrigramsAreSimilar(dish.NormalizedName, normalizedQuery)
            || EF.Functions.TrigramsAreSimilar(dish.AliasesSearchText, normalizedQuery));
    }

    private bool IsNpgsqlProvider() =>
        string.Equals(_dbContext.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal);

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        if (exception.InnerException is PostgresException postgresException)
        {
            return postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
        }

        if (exception.InnerException is null
            || !string.Equals(exception.InnerException.GetType().FullName, "Microsoft.Data.Sqlite.SqliteException", StringComparison.Ordinal))
        {
            return false;
        }

        var sqliteErrorCodeProperty = exception.InnerException.GetType().GetProperty("SqliteErrorCode");
        var sqliteErrorCode = sqliteErrorCodeProperty?.GetValue(exception.InnerException);
        return sqliteErrorCode is int code && code == 19;
    }
}
