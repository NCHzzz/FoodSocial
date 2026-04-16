namespace FoodSocial.Contracts.UserProfiles;

/// <summary>
/// Represents an owner-scoped request to update application-owned user profile fields.
/// </summary>
/// <param name="DisplayName">The display name shown on the profile.</param>
/// <param name="AvatarUrl">The optional avatar URL.</param>
/// <param name="Bio">The optional profile biography.</param>
/// <param name="PreferredCuisines">The preferred cuisine values for the manual taste profile.</param>
/// <param name="DislikedIngredients">The disliked ingredient values for the manual taste profile.</param>
/// <param name="DietaryRestrictions">The dietary restriction values for the manual taste profile.</param>
public sealed record UpdateUserProfileRequest(
    string? DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyList<string>? PreferredCuisines,
    IReadOnlyList<string>? DislikedIngredients,
    IReadOnlyList<string>? DietaryRestrictions);
