namespace FoodSocial.UserProfile.Application;

/// <summary>
/// Defines persistence operations for the application-owned user profile graph.
/// </summary>
public interface IUserProfileStore
{
    /// <summary>
    /// Finds profile ownership details by identity identifier.
    /// </summary>
    Task<UserProfileOwnership?> FindOwnershipByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken);

    /// <summary>
    /// Finds profile ownership details by subject.
    /// </summary>
    Task<UserProfileOwnership?> FindOwnershipBySubjectAsync(string subject, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a public profile snapshot by identity identifier.
    /// </summary>
    Task<UserProfileSnapshot?> GetProfileAsync(Guid identityId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates profile fields for the supplied identity identifier.
    /// </summary>
    Task<UserProfileSnapshot?> UpdateProfileAsync(Guid identityId, UpdateUserProfileCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a bounded follower list for the supplied identity identifier.
    /// </summary>
    Task<UserProfileConnectionsSnapshot?> GetFollowersAsync(Guid identityId, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a bounded following list for the supplied identity identifier.
    /// </summary>
    Task<UserProfileConnectionsSnapshot?> GetFollowingAsync(Guid identityId, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a follow relationship idempotently.
    /// </summary>
    Task<UserProfileMutationResult> FollowAsync(Guid followerIdentityId, Guid followedIdentityId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a follow relationship idempotently.
    /// </summary>
    Task<UserProfileMutationResult> UnfollowAsync(Guid followerIdentityId, Guid followedIdentityId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the owner-scoped saved items for the supplied identity identifier.
    /// </summary>
    Task<UserSavedItemsSnapshot?> GetSavedItemsAsync(Guid identityId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a post idempotently for the supplied owner.
    /// </summary>
    Task<UserSavedItemMutationResult> SavePostAsync(Guid identityId, Guid postId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a saved post idempotently for the supplied owner.
    /// </summary>
    Task<UserSavedItemMutationResult> UnsavePostAsync(Guid identityId, Guid postId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a dish idempotently for the supplied owner.
    /// </summary>
    Task<UserSavedItemMutationResult> SaveDishAsync(Guid identityId, Guid dishId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a saved dish idempotently for the supplied owner.
    /// </summary>
    Task<UserSavedItemMutationResult> UnsaveDishAsync(Guid identityId, Guid dishId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a place idempotently for the supplied owner.
    /// </summary>
    Task<UserSavedItemMutationResult> SavePlaceAsync(Guid identityId, Guid placeId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a saved place idempotently for the supplied owner.
    /// </summary>
    Task<UserSavedItemMutationResult> UnsavePlaceAsync(Guid identityId, Guid placeId, CancellationToken cancellationToken);
}
