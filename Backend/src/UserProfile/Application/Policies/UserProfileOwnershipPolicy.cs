using FoodSocial.UserProfile.Application.DTOs;

namespace FoodSocial.UserProfile.Application.Policies;

/// <summary>
/// Centralizes owner lookup and subject comparison for authorization-sensitive profile operations.
/// </summary>
public sealed class UserProfileOwnershipPolicy(IUserProfileStore userProfileStore)
{
    private readonly IUserProfileStore _userProfileStore = userProfileStore ?? throw new ArgumentNullException(nameof(userProfileStore));

    /// <summary>
    /// Authorizes access to an owner-scoped profile by identity identifier.
    /// </summary>
    public async Task<UserProfileOwnershipAuthorizationResult> AuthorizeOwnerAsync(
        string authenticatedSubject,
        Guid identityId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticatedSubject);

        var ownership = await _userProfileStore.FindOwnershipByIdentityIdAsync(identityId, cancellationToken);
        if (ownership is null)
        {
            return UserProfileOwnershipAuthorizationResult.NotFound(
                "The requested user profile was not found.");
        }

        return string.Equals(ownership.Subject, authenticatedSubject, StringComparison.Ordinal)
            ? UserProfileOwnershipAuthorizationResult.Success(ownership)
            : UserProfileOwnershipAuthorizationResult.Forbidden(
                "The authenticated user does not own the requested profile.");
    }

    /// <summary>
    /// Finds the authenticated user's owner profile for actor-scoped operations.
    /// </summary>
    public async Task<UserProfileOwnershipAuthorizationResult> RequireAuthenticatedOwnerAsync(
        string authenticatedSubject,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticatedSubject);

        var ownership = await _userProfileStore.FindOwnershipBySubjectAsync(authenticatedSubject, cancellationToken);
        return ownership is null
            ? UserProfileOwnershipAuthorizationResult.NotFound("The authenticated user profile was not found.")
            : UserProfileOwnershipAuthorizationResult.Success(ownership);
    }
}

/// <summary>
/// Represents ownership authorization outcome for profile application services.
/// </summary>
public sealed record UserProfileOwnershipAuthorizationResult(
    UserProfileOperationStatus Status,
    UserProfileOwnership? Ownership,
    string? Error)
{
    public static UserProfileOwnershipAuthorizationResult Success(UserProfileOwnership ownership) =>
        new(UserProfileOperationStatus.Success, ownership, null);

    public static UserProfileOwnershipAuthorizationResult NotFound(string error) =>
        new(UserProfileOperationStatus.NotFound, null, error);

    public static UserProfileOwnershipAuthorizationResult Forbidden(string error) =>
        new(UserProfileOperationStatus.Forbidden, null, error);
}
