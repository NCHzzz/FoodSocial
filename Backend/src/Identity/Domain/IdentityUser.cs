namespace FoodSocial.Identity.Domain;

/// <summary>
/// Represents the local identity record that maps an external subject into the FoodSocial Identity boundary.
/// </summary>
public sealed class IdentityUser
{
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the tenant isolation identifier.
    /// </summary>
    public Guid TenantId { get; private set; } = DefaultTenantId;

    /// <summary>
    /// Gets the external identity subject.
    /// </summary>
    public string Subject { get; private set; }

    /// <summary>
    /// Gets the last known preferred username supplied by the external identity provider.
    /// </summary>
    public string? PreferredUsername { get; private set; }

    /// <summary>
    /// Gets the last known email supplied by the external identity provider.
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Gets the current lifecycle state for the identity.
    /// </summary>
    public IdentityLifecycleState State { get; private set; } = IdentityLifecycleState.PendingBootstrap;

    /// <summary>
    /// Gets the UTC timestamp when the identity was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private IdentityUser()
    {
        Subject = string.Empty;
    }

    public IdentityUser(string subject, string? preferredUsername, string? email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        Subject = subject;
        PreferredUsername = preferredUsername;
        Email = email;
    }

    /// <summary>
    /// Reconciles the cached external profile fields for this identity.
    /// </summary>
    /// <param name="preferredUsername">The latest preferred username from trusted claims.</param>
    /// <param name="email">The latest email address from trusted claims.</param>
    public void ReconcileProfile(string? preferredUsername, string? email)
    {
        if (!string.IsNullOrWhiteSpace(preferredUsername)
            && !string.Equals(PreferredUsername, preferredUsername, StringComparison.Ordinal))
        {
            PreferredUsername = preferredUsername;
        }

        if (!string.IsNullOrWhiteSpace(email)
            && !string.Equals(Email, email, StringComparison.Ordinal))
        {
            Email = email;
        }
    }

    /// <summary>
    /// Applies a canonical lifecycle transition to this identity.
    /// </summary>
    /// <param name="transition">The requested lifecycle transition.</param>
    public void TransitionTo(IdentityLifecycleTransition transition)
    {
        State = IdentityLifecycleRules.Apply(State, transition);
    }
}
