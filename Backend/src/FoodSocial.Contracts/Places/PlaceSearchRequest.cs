namespace FoodSocial.Contracts.Places;

/// <summary>
/// Query contract for bounded Place search.
/// </summary>
public sealed record PlaceSearchRequest(
    string? Query,
    string? City,
    string? Area,
    string? CuisineType,
    string? PriceRange,
    bool IncludeInactive,
    int? Limit);
