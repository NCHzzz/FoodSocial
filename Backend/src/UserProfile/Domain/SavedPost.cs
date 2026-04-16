namespace FoodSocial.UserProfile.Domain;

/// <summary>
/// Represents a saved post reference owned by a user profile shell.
/// </summary>
public sealed class SavedPost
{
    private SavedPost()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SavedPost"/> class.
    /// </summary>
    /// <param name="userProfileShellId">The owning profile shell identifier.</param>
    /// <param name="postId">The saved post identifier.</param>
    public SavedPost(Guid userProfileShellId, Guid postId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userProfileShellId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(postId, Guid.Empty);

        UserProfileShellId = userProfileShellId;
        PostId = postId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the saved post identifier.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the owning profile shell identifier.
    /// </summary>
    public Guid UserProfileShellId { get; private set; }

    /// <summary>
    /// Gets the saved post identifier.
    /// </summary>
    public Guid PostId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the item was saved.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
