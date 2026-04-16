namespace FoodSocial.Contracts.UserProfiles;

/// <summary>
/// Represents a compact user profile summary for follow-graph lists.
/// </summary>
/// <param name="IdentityId">The user identity identifier.</param>
/// <param name="DisplayName">The display name shown for the user.</param>
/// <param name="AvatarUrl">The optional avatar URL.</param>
public sealed record UserProfileSummaryDto(
    Guid IdentityId,
    string? DisplayName,
    string? AvatarUrl);
