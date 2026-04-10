namespace FoodSocial.UserProfile.Application;

/// <summary>
/// Represents a duplicate profile-shell identity violation.
/// </summary>
public sealed class UserProfileBootstrapDuplicateIdentityException(Guid identityId, Exception innerException)
    : InvalidOperationException($"A profile shell for identity '{identityId}' already exists.", innerException)
{
    /// <summary>
    /// Gets the duplicate identity identifier.
    /// </summary>
    public Guid IdentityId { get; } = identityId;
}
