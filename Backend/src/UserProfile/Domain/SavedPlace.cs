namespace FoodSocial.UserProfile.Domain;

/// <summary>
/// Represents a saved place reference owned by a user profile shell.
/// </summary>
public sealed class SavedPlace
{
    private SavedPlace()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SavedPlace"/> class.
    /// </summary>
    /// <param name="userProfileShellId">The owning profile shell identifier.</param>
    /// <param name="placeId">The saved place identifier.</param>
    public SavedPlace(Guid userProfileShellId, Guid placeId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userProfileShellId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(placeId, Guid.Empty);

        UserProfileShellId = userProfileShellId;
        PlaceId = placeId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the saved place identifier.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the owning profile shell identifier.
    /// </summary>
    public Guid UserProfileShellId { get; private set; }

    /// <summary>
    /// Gets the saved place identifier.
    /// </summary>
    public Guid PlaceId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the item was saved.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
