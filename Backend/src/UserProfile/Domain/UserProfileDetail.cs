namespace FoodSocial.UserProfile.Domain;

/// <summary>
/// Represents the application-owned profile details anchored to a profile shell.
/// </summary>
public sealed class UserProfileDetail
{
    private readonly List<string> _dietaryRestrictions = [];
    private readonly List<string> _dislikedIngredients = [];
    private readonly List<string> _preferredCuisines = [];

    private UserProfileDetail()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserProfileDetail"/> class.
    /// </summary>
    /// <param name="userProfileShellId">The owning profile shell identifier.</param>
    /// <param name="displayName">The initial display name.</param>
    public UserProfileDetail(Guid userProfileShellId, string? displayName)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userProfileShellId, Guid.Empty);

        UserProfileShellId = userProfileShellId;
        DisplayName = NormalizeOptional(displayName);
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    /// <summary>
    /// Gets the profile detail identifier.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the owning profile shell identifier.
    /// </summary>
    public Guid UserProfileShellId { get; private set; }

    /// <summary>
    /// Gets the display name presented by the application profile.
    /// </summary>
    public string? DisplayName { get; private set; }

    /// <summary>
    /// Gets the optional avatar URL.
    /// </summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// Gets the optional profile biography.
    /// </summary>
    public string? Bio { get; private set; }

    /// <summary>
    /// Gets the preferred cuisine tags selected by the user.
    /// </summary>
    public IReadOnlyList<string> PreferredCuisines => _preferredCuisines;

    /// <summary>
    /// Gets the disliked ingredients selected by the user.
    /// </summary>
    public IReadOnlyList<string> DislikedIngredients => _dislikedIngredients;

    /// <summary>
    /// Gets the dietary restrictions selected by the user.
    /// </summary>
    public IReadOnlyList<string> DietaryRestrictions => _dietaryRestrictions;

    /// <summary>
    /// Gets the UTC timestamp when the detail row was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the detail row was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Updates the mutable MVP profile fields.
    /// </summary>
    /// <param name="displayName">The new display name.</param>
    /// <param name="avatarUrl">The new avatar URL.</param>
    /// <param name="bio">The new biography.</param>
    /// <param name="preferredCuisines">The preferred cuisine values.</param>
    /// <param name="dislikedIngredients">The disliked ingredient values.</param>
    /// <param name="dietaryRestrictions">The dietary restriction values.</param>
    public void Update(
        string? displayName,
        string? avatarUrl,
        string? bio,
        IEnumerable<string>? preferredCuisines,
        IEnumerable<string>? dislikedIngredients,
        IEnumerable<string>? dietaryRestrictions)
    {
        DisplayName = NormalizeOptional(displayName);
        AvatarUrl = NormalizeOptional(avatarUrl);
        Bio = NormalizeOptional(bio);

        ReplaceValues(_preferredCuisines, preferredCuisines);
        ReplaceValues(_dislikedIngredients, dislikedIngredients);
        ReplaceValues(_dietaryRestrictions, dietaryRestrictions);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ReplaceValues(List<string> target, IEnumerable<string>? source)
    {
        target.Clear();

        if (source is null)
        {
            return;
        }

        foreach (var value in source)
        {
            var normalized = NormalizeOptional(value);
            if (normalized is null || target.Contains(normalized, StringComparer.Ordinal))
            {
                continue;
            }

            target.Add(normalized);
        }
    }
}
