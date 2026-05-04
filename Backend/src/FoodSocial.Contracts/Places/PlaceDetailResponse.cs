namespace FoodSocial.Contracts.Places;

/// <summary>
/// Detailed response contract for a canonical Place record.
/// </summary>
public sealed record PlaceDetailResponse(
    Guid Id,
    string Name,
    string NormalizedName,
    string? Description,
    string AddressText,
    string Area,
    string City,
    string Country,
    decimal? Latitude,
    decimal? Longitude,
    IReadOnlyList<string> CuisineTypes,
    string? PriceRange,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
