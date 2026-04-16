namespace FoodSocial.UserProfile.Domain;

/// <summary>
/// Represents a follow relationship between two user profile shells.
/// </summary>
public sealed class UserFollow
{
    private UserFollow()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserFollow"/> class.
    /// </summary>
    /// <param name="followerUserProfileShellId">The follower profile shell identifier.</param>
    /// <param name="followedUserProfileShellId">The followed profile shell identifier.</param>
    public UserFollow(Guid followerUserProfileShellId, Guid followedUserProfileShellId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(followerUserProfileShellId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(followedUserProfileShellId, Guid.Empty);

        if (followerUserProfileShellId == followedUserProfileShellId)
        {
            throw new ArgumentException("A user cannot follow the same profile shell as both follower and followed.", nameof(followedUserProfileShellId));
        }

        FollowerUserProfileShellId = followerUserProfileShellId;
        FollowedUserProfileShellId = followedUserProfileShellId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the follow relationship identifier.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the follower profile shell identifier.
    /// </summary>
    public Guid FollowerUserProfileShellId { get; private set; }

    /// <summary>
    /// Gets the followed profile shell identifier.
    /// </summary>
    public Guid FollowedUserProfileShellId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the follow was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
