namespace FoodSocial.Contracts.UserProfiles;

/// <summary>
/// Represents the result of following or unfollowing another user.
/// </summary>
/// <param name="TargetIdentityId">The followed or unfollowed user identity identifier.</param>
/// <param name="Following"><c>true</c> when the caller now follows the target; otherwise <c>false</c>.</param>
public sealed record FollowUserResponse(
    Guid TargetIdentityId,
    bool Following);
