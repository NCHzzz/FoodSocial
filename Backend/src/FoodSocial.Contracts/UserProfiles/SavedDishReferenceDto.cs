namespace FoodSocial.Contracts.UserProfiles;

/// <summary>
/// Represents a saved dish reference.
/// </summary>
/// <param name="DishId">The saved dish identifier.</param>
/// <param name="SavedAtUtc">The UTC timestamp when the dish was saved.</param>
public sealed record SavedDishReferenceDto(
    Guid DishId,
    DateTimeOffset SavedAtUtc);
