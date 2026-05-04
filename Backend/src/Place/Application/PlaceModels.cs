using FoodSocial.Contracts.Places;
using FoodSocial.Place.Domain;

namespace FoodSocial.Place.Application;

/// <summary>
/// Represents Place mutation outcomes.
/// </summary>
public enum PlaceOperationStatus
{
    /// <summary>The mutation created a new Place.</summary>
    Created,

    /// <summary>The mutation found an existing duplicate-safe Place.</summary>
    AlreadyExists,

    /// <summary>The mutation updated a Place lifecycle status.</summary>
    StatusUpdated,

    /// <summary>The requested Place was not found.</summary>
    NotFound,

    /// <summary>The request was invalid.</summary>
    Invalid
}

/// <summary>
/// Represents a Place mutation result.
/// </summary>
public sealed record PlaceMutationResult(PlaceOperationStatus Status, PlaceDetailResponse? Place, string? Error);

/// <summary>
/// Represents persistence creation outcome with duplicate safety.
/// </summary>
public sealed record PlaceCreationResult(PlaceRecord Place, bool Created);

/// <summary>
/// Represents normalized Place search criteria.
/// </summary>
public sealed record PlaceSearchCriteria(
    string? Query,
    string? NormalizedQuery,
    string? NormalizedCity,
    string? NormalizedArea,
    string? CuisineType,
    string? PriceRange,
    bool IncludeInactive,
    int Limit)
{
    /// <summary>
    /// Creates normalized search criteria from a transport request.
    /// </summary>
    public static PlaceSearchCriteria FromRequest(PlaceSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = NormalizeOptional(request.Query);
        var city = NormalizeOptional(request.City);
        var area = NormalizeOptional(request.Area);
        var cuisineType = NormalizeOptional(request.CuisineType);
        var priceRange = NormalizeOptional(request.PriceRange);
        var limit = request.Limit is null or <= 0 ? 20 : Math.Min(request.Limit.Value, 50);

        return new PlaceSearchCriteria(
            query,
            query is null ? null : PlaceRecord.NormalizeSearchValue(query),
            city is null ? null : PlaceRecord.NormalizeSearchValue(city),
            area is null ? null : PlaceRecord.NormalizeSearchValue(area),
            cuisineType is null ? null : PlaceRecord.NormalizeSearchValue(cuisineType),
            priceRange,
            request.IncludeInactive,
            limit);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
