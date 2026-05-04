namespace FoodSocial.Contracts.Places;

/// <summary>
/// Request contract for creating a canonical Place record.
/// </summary>
public sealed record CreatePlaceRequest(
    string? Name,
    string? Description,
    string? AddressText,
    string? Area,
    string? City,
    string? Country,
    decimal? Latitude,
    decimal? Longitude,
    IReadOnlyList<string>? CuisineTypes,
    string? PriceRange);
