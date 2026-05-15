using FoodSocial.Contracts.Dishes;
using FoodSocial.Dish.Domain;

namespace FoodSocial.Dish.Application.DTOs;

/// <summary>
/// Represents Dish mutation outcomes.
/// </summary>
public enum DishOperationStatus
{
    /// <summary>The mutation created a new Dish.</summary>
    Created,

    /// <summary>The mutation found an existing duplicate-safe Dish.</summary>
    AlreadyExists,

    /// <summary>The mutation deprecated a Dish.</summary>
    Deprecated,

    /// <summary>The requested Dish was not found.</summary>
    NotFound,

    /// <summary>The request was invalid.</summary>
    Invalid
}

/// <summary>
/// Represents a Dish mutation result.
/// </summary>
public sealed record DishMutationResult(DishOperationStatus Status, DishDetailResponse? Dish, string? Error);

/// <summary>
/// Represents persistence creation outcome with duplicate safety.
/// </summary>
public sealed record DishCreationResult(DishRecord Dish, bool Created);

/// <summary>
/// Represents normalized Dish search criteria.
/// </summary>
public sealed record DishSearchCriteria(
    string? Query,
    string? NormalizedQuery,
    string? Category,
    string? CuisineType,
    bool IncludeDeprecated,
    int Limit)
{
    /// <summary>
    /// Creates normalized search criteria from a transport request.
    /// </summary>
    public static DishSearchCriteria FromRequest(DishSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = NormalizeOptional(request.Query);
        var category = NormalizeOptional(request.Category);
        var cuisineType = NormalizeOptional(request.CuisineType);
        var limit = request.Limit is null or <= 0 ? 20 : Math.Min(request.Limit.Value, 50);

        return new DishSearchCriteria(
            query,
            query is null ? null : DishRecord.NormalizeSearchValue(query),
            category,
            cuisineType,
            request.IncludeDeprecated,
            limit);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
