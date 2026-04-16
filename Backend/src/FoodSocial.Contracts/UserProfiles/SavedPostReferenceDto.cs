namespace FoodSocial.Contracts.UserProfiles;

/// <summary>
/// Represents a saved post reference.
/// </summary>
/// <param name="PostId">The saved post identifier.</param>
/// <param name="SavedAtUtc">The UTC timestamp when the post was saved.</param>
public sealed record SavedPostReferenceDto(
    Guid PostId,
    DateTimeOffset SavedAtUtc);
