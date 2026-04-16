namespace FoodSocial.Contracts.UserProfiles;

/// <summary>
/// Represents the result of saving or unsaving an owned item reference.
/// </summary>
/// <param name="UserIdentityId">The owning user identity identifier.</param>
/// <param name="TargetId">The saved target identifier.</param>
/// <param name="TargetType">The saved target type.</param>
/// <param name="Saved"><c>true</c> when the target is now saved; otherwise <c>false</c>.</param>
public sealed record SaveUserItemResponse(
    Guid UserIdentityId,
    Guid TargetId,
    string TargetType,
    bool Saved);
