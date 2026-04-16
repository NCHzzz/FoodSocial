namespace FoodSocial.UserProfile.Domain;

/// <summary>
/// Represents a saved dish reference owned by a user profile shell.
/// </summary>
public sealed class SavedDish
{
    private SavedDish()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SavedDish"/> class.
    /// </summary>
    /// <param name="userProfileShellId">The owning profile shell identifier.</param>
    /// <param name="dishId">The saved dish identifier.</param>
    public SavedDish(Guid userProfileShellId, Guid dishId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userProfileShellId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(dishId, Guid.Empty);

        UserProfileShellId = userProfileShellId;
        DishId = dishId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the saved dish identifier.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the owning profile shell identifier.
    /// </summary>
    public Guid UserProfileShellId { get; private set; }

    /// <summary>
    /// Gets the saved dish identifier.
    /// </summary>
    public Guid DishId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the item was saved.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
