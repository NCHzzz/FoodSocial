namespace FoodSocial.UserProfile.Application;

/// <summary>
/// Handles application-owned follow-graph operations.
/// </summary>
public sealed class UserFollowService(IUserProfileStore userProfileStore)
{
    private const int DefaultListLimit = 50;
    private readonly IUserProfileStore _userProfileStore = userProfileStore ?? throw new ArgumentNullException(nameof(userProfileStore));

    /// <summary>
    /// Gets the followers for the supplied identity identifier.
    /// </summary>
    public Task<UserProfileConnectionsSnapshot?> GetFollowersAsync(Guid identityId, CancellationToken cancellationToken) =>
        _userProfileStore.GetFollowersAsync(identityId, DefaultListLimit, cancellationToken);

    /// <summary>
    /// Gets the following list for the supplied identity identifier.
    /// </summary>
    public Task<UserProfileConnectionsSnapshot?> GetFollowingAsync(Guid identityId, CancellationToken cancellationToken) =>
        _userProfileStore.GetFollowingAsync(identityId, DefaultListLimit, cancellationToken);

    /// <summary>
    /// Follows the supplied target profile idempotently for the authenticated user.
    /// </summary>
    public async Task<UserProfileMutationResult> FollowAsync(
        string authenticatedSubject,
        Guid targetIdentityId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticatedSubject);

        var owner = await _userProfileStore.FindOwnershipBySubjectAsync(authenticatedSubject, cancellationToken);
        if (owner is null)
        {
            return new UserProfileMutationResult(UserProfileOperationStatus.NotFound, targetIdentityId, false, "The authenticated user profile was not found.");
        }

        if (owner.IdentityId == targetIdentityId)
        {
            return new UserProfileMutationResult(UserProfileOperationStatus.Invalid, targetIdentityId, false, "A user cannot follow their own profile.");
        }

        return await _userProfileStore.FollowAsync(owner.IdentityId, targetIdentityId, cancellationToken);
    }

    /// <summary>
    /// Unfollows the supplied target profile idempotently for the authenticated user.
    /// </summary>
    public async Task<UserProfileMutationResult> UnfollowAsync(
        string authenticatedSubject,
        Guid targetIdentityId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticatedSubject);

        var owner = await _userProfileStore.FindOwnershipBySubjectAsync(authenticatedSubject, cancellationToken);
        if (owner is null)
        {
            return new UserProfileMutationResult(UserProfileOperationStatus.NotFound, targetIdentityId, false, "The authenticated user profile was not found.");
        }

        if (owner.IdentityId == targetIdentityId)
        {
            return new UserProfileMutationResult(UserProfileOperationStatus.Invalid, targetIdentityId, false, "A user cannot unfollow their own profile.");
        }

        return await _userProfileStore.UnfollowAsync(owner.IdentityId, targetIdentityId, cancellationToken);
    }
}
