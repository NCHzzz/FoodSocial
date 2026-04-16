namespace FoodSocial.UserProfile.Application;

/// <summary>
/// Handles application-owned user profile queries and updates.
/// </summary>
public sealed class UserProfileService(IUserProfileStore userProfileStore)
{
    private readonly IUserProfileStore _userProfileStore = userProfileStore ?? throw new ArgumentNullException(nameof(userProfileStore));

    /// <summary>
    /// Gets the public application-owned profile for the supplied identity identifier.
    /// </summary>
    public Task<UserProfileSnapshot?> GetProfileAsync(Guid identityId, CancellationToken cancellationToken) =>
        _userProfileStore.GetProfileAsync(identityId, cancellationToken);

    /// <summary>
    /// Updates owner-scoped profile fields for the supplied identity identifier.
    /// </summary>
    public async Task<UserProfileUpdateResult> UpdateProfileAsync(
        string authenticatedSubject,
        Guid identityId,
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticatedSubject);
        ArgumentNullException.ThrowIfNull(command);

        var ownership = await _userProfileStore.FindOwnershipByIdentityIdAsync(identityId, cancellationToken);
        if (ownership is null)
        {
            return new UserProfileUpdateResult(UserProfileOperationStatus.NotFound, null, "The requested user profile was not found.");
        }

        if (!string.Equals(ownership.Subject, authenticatedSubject, StringComparison.Ordinal))
        {
            return new UserProfileUpdateResult(UserProfileOperationStatus.Forbidden, null, "The authenticated user does not own the requested profile.");
        }

        var snapshot = await _userProfileStore.UpdateProfileAsync(identityId, command, cancellationToken);
        return snapshot is null
            ? new UserProfileUpdateResult(UserProfileOperationStatus.NotFound, null, "The requested user profile was not found.")
            : new UserProfileUpdateResult(UserProfileOperationStatus.Success, snapshot, null);
    }
}
