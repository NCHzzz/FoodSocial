namespace FoodSocial.Contracts.UserProfiles;

/// <summary>
/// Represents the public application-owned user profile response.
/// </summary>
/// <param name="IdentityId">The identity identifier that owns the profile.</param>
/// <param name="DisplayName">The display name shown on the profile.</param>
/// <param name="AvatarUrl">The optional avatar URL.</param>
/// <param name="Bio">The optional profile biography.</param>
/// <param name="PreferredCuisines">The preferred cuisine values.</param>
/// <param name="DislikedIngredients">The disliked ingredient values.</param>
/// <param name="DietaryRestrictions">The dietary restriction values.</param>
/// <param name="FollowerCount">The number of followers for the profile.</param>
/// <param name="FollowingCount">The number of other users the profile follows.</param>
public sealed record UserProfileResponse(
    Guid IdentityId,
    string? DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyList<string> PreferredCuisines,
    IReadOnlyList<string> DislikedIngredients,
    IReadOnlyList<string> DietaryRestrictions,
    long FollowerCount,
    long FollowingCount);
