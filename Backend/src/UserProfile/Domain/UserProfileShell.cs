namespace FoodSocial.UserProfile.Domain;

/// <summary>
/// Represents the minimal profile shell owned by the UserProfile boundary.
/// </summary>
public sealed class UserProfileShell
{
    /// <summary>
    /// Gets the profile shell identifier.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the identity identifier that owns this shell.
    /// </summary>
    public Guid IdentityId { get; private set; }

    /// <summary>
    /// Gets the trusted external identity subject.
    /// </summary>
    public string Subject { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the shell was created.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private UserProfileShell()
    {
        Subject = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserProfileShell"/> class.
    /// </summary>
    /// <param name="identityId">The owning identity identifier.</param>
    /// <param name="subject">The trusted external identity subject.</param>
    public UserProfileShell(Guid identityId, string subject)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(identityId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        IdentityId = identityId;
        Subject = subject;
    }
}
