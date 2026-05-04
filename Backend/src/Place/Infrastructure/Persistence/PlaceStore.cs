using FoodSocial.Place.Application;
using FoodSocial.Place.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodSocial.Place.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core implementation of the canonical Place store.
/// </summary>
public sealed class PlaceStore(PlaceDbContext dbContext) : IPlaceStore
{
    private readonly PlaceDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task<PlaceCreationResult> CreatePlaceAsync(PlaceRecord place, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(place);

        var existing = await FindByDuplicateScopeAsync(place.NormalizedName, place.NormalizedCity, place.NormalizedArea, cancellationToken);
        if (existing is not null)
        {
            return new PlaceCreationResult(existing, Created: false);
        }

        _dbContext.Places.Add(place);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();
            var duplicate = await FindByDuplicateScopeAsync(place.NormalizedName, place.NormalizedCity, place.NormalizedArea, cancellationToken);
            if (duplicate is null)
            {
                throw;
            }

            return new PlaceCreationResult(duplicate, Created: false);
        }

        _dbContext.ChangeTracker.Clear();
        var created = await GetPlaceAsync(place.Id, cancellationToken)
            ?? throw new InvalidOperationException("The created place could not be reloaded.");
        return new PlaceCreationResult(created, Created: true);
    }

    /// <inheritdoc />
    public Task<PlaceRecord?> GetPlaceAsync(Guid placeId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(placeId, Guid.Empty);

        return _dbContext.Places
            .AsNoTracking()
            .SingleOrDefaultAsync(place => place.Id == placeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlaceRecord>> SearchPlacesAsync(PlaceSearchCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = ApplyStructuredFilters(_dbContext.Places.AsNoTracking(), criteria);

        var normalizedQuery = criteria.NormalizedQuery;
        if (normalizedQuery is not null)
        {
            query = query.Where(place =>
                place.NormalizedName.Contains(normalizedQuery)
                || place.NormalizedAddressText.Contains(normalizedQuery)
                || place.NormalizedCity.Contains(normalizedQuery)
                || place.NormalizedArea.Contains(normalizedQuery)
                || place.NormalizedCountry.Contains(normalizedQuery));
        }

        return await query
            .OrderBy(place => place.Name)
            .ThenBy(place => place.Id)
            .Take(criteria.Limit)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PlaceRecord?> UpdatePlaceStatusAsync(Guid placeId, PlaceLifecycleStatus status, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(placeId, Guid.Empty);

        var place = await _dbContext.Places.SingleOrDefaultAsync(candidate => candidate.Id == placeId, cancellationToken);
        if (place is null)
        {
            return null;
        }

        place.ChangeStatus(status);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.ChangeTracker.Clear();

        return await GetPlaceAsync(placeId, cancellationToken);
    }

    private static IQueryable<PlaceRecord> ApplyStructuredFilters(IQueryable<PlaceRecord> query, PlaceSearchCriteria criteria)
    {
        if (!criteria.IncludeInactive)
        {
            query = query.Where(place => place.Status == PlaceLifecycleStatus.Active);
        }

        if (criteria.NormalizedCity is not null)
        {
            query = query.Where(place => place.NormalizedCity == criteria.NormalizedCity);
        }

        if (criteria.NormalizedArea is not null)
        {
            query = query.Where(place => place.NormalizedArea == criteria.NormalizedArea);
        }

        if (criteria.PriceRange is not null)
        {
            query = query.Where(place => place.PriceRange != null && place.PriceRange.ToLower() == criteria.PriceRange.ToLower());
        }

        if (criteria.CuisineType is not null)
        {
            var cuisineToken = $"%|{criteria.CuisineType}|%";
            query = query.Where(place => EF.Functions.Like(place.CuisineTypesSearchText, cuisineToken));
        }

        return query;
    }

    private Task<PlaceRecord?> FindByDuplicateScopeAsync(string normalizedName, string normalizedCity, string normalizedArea, CancellationToken cancellationToken) =>
        _dbContext.Places
            .AsNoTracking()
            .SingleOrDefaultAsync(
                place => place.NormalizedName == normalizedName
                    && place.NormalizedCity == normalizedCity
                    && place.NormalizedArea == normalizedArea,
                cancellationToken);

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
