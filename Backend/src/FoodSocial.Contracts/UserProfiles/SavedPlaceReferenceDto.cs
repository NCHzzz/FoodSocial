namespace FoodSocial.Contracts.UserProfiles;

/// <summary>
/// Represents a saved place reference.
/// </summary>
/// <param name="PlaceId">The saved place identifier.</param>
/// <param name="SavedAtUtc">The UTC timestamp when the place was saved.</param>
public sealed record SavedPlaceReferenceDto(
    Guid PlaceId,
    DateTimeOffset SavedAtUtc);
