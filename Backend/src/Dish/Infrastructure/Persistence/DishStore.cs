using FoodSocial.Dish.Application;
using FoodSocial.Dish.Domain;
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

        return _dbContext.Dishes
            .AsNoTracking()
            .SingleOrDefaultAsync(dish => dish.Id == dishId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DishRecord>> SearchDishesAsync(DishSearchCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = ApplyStructuredFilters(_dbContext.Dishes.AsNoTracking(), criteria);

        var normalizedQuery = criteria.NormalizedQuery;
        if (normalizedQuery is not null)
        {
            var aliasToken = $"%|{normalizedQuery}%";
            query = query.Where(dish =>
                dish.NormalizedName.Contains(normalizedQuery)
                || EF.Functions.Like(dish.AliasesSearchText, aliasToken));
        }

        if (criteria.Category is not null)
        {
            query = query.Where(dish => dish.Category != null && dish.Category.ToLower() == criteria.Category.ToLower());
        }

        if (criteria.CuisineType is not null)
        {
            query = query.Where(dish => dish.CuisineType != null && dish.CuisineType.ToLower() == criteria.CuisineType.ToLower());
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

        var dish = await _dbContext.Dishes.SingleOrDefaultAsync(candidate => candidate.Id == dishId, cancellationToken);
        if (dish is null)
        {
            return null;
        }

        dish.Deprecate();
        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.ChangeTracker.Clear();

        return await GetDishAsync(dishId, cancellationToken);
    }

    private Task<DishRecord?> FindByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken) =>
        _dbContext.Dishes
            .AsNoTracking()
            .SingleOrDefaultAsync(dish => dish.NormalizedName == normalizedName, cancellationToken);

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
