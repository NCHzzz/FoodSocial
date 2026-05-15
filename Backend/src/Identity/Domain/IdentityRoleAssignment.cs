namespace FoodSocial.Identity.Domain;

/// <summary>
/// Represents an Identity-owned account-level role assignment for a subject.
/// </summary>
public sealed class IdentityRoleAssignment
{
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Gets the assignment identifier.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the tenant isolation identifier.
    /// </summary>
    public Guid TenantId { get; private set; } = DefaultTenantId;

    /// <summary>
    /// Gets the subject that owns this role assignment.
    /// </summary>
    public string Subject { get; private set; }

    /// <summary>
    /// Gets the assigned role.
    /// </summary>
    public string Role { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the assignment was created.
    /// </summary>
    public DateTimeOffset AssignedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private IdentityRoleAssignment()
    {
        Subject = string.Empty;
        Role = string.Empty;
    }

    /// <summary>
    /// Initializes a new role assignment.
    /// </summary>
    /// <param name="subject">The trusted subject receiving the role.</param>
    /// <param name="role">The canonical role name.</param>
    public IdentityRoleAssignment(string subject, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        Subject = subject;
        Role = IdentityAppRoles.Normalize(role);
    }
}
