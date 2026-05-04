namespace FoodSocial.Contracts.Places;

/// <summary>
/// Summary response contract for Place lists and search results.
/// </summary>
public sealed record PlaceSummaryResponse(
    Guid Id,
    string Name,
    string AddressText,
    string Area,
    string City,
    string Country,
    IReadOnlyList<string> CuisineTypes,
    string? PriceRange,
    string Status);
