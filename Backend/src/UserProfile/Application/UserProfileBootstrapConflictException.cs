namespace FoodSocial.UserProfile.Application;

/// <summary>
/// Represents a persistence conflict while bootstrapping a profile shell.
/// </summary>
public sealed class UserProfileBootstrapConflictException(Guid identityId, Exception innerException)
    : InvalidOperationException($"A profile shell bootstrap conflict occurred for identity '{identityId}'.", innerException)
{
    /// <summary>
    /// Gets the identity identifier involved in the conflicting bootstrap attempt.
    /// </summary>
    public Guid IdentityId { get; } = identityId;
}
