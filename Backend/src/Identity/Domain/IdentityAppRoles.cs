namespace FoodSocial.Identity.Domain;

/// <summary>
/// Defines the canonical account-level roles owned by the Identity service.
/// </summary>
public static class IdentityAppRoles
{
    /// <summary>
    /// Standard end-user role.
    /// </summary>
    public const string User = "user";

    /// <summary>
    /// Administrative role.
    /// </summary>
    public const string Admin = "admin";

    /// <summary>
    /// Moderation role.
    /// </summary>
    public const string Moderator = "moderator";

    /// <summary>
    /// Gets the explicit canonical role set.
    /// </summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        User,
        Admin,
        Moderator
    };

    /// <summary>
    /// Normalizes and validates a role name.
    /// </summary>
    /// <param name="role">The input role value.</param>
    /// <returns>The normalized role.</returns>
    public static string Normalize(string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        var normalizedRole = role.Trim().ToLowerInvariant();

        if (!All.Contains(normalizedRole))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported account role.");
        }

        return normalizedRole;
    }
}
