using FoodSocial.Identity.Domain;

namespace FoodSocial.Identity.Application.UseCases.Identities;

/// <summary>
/// Assigns application-owned Identity roles inside the application boundary.
/// </summary>
public sealed class AssignIdentityRoleCommandHandler(IIdentityUserStore identityUserStore)
{
    private readonly IIdentityUserStore _identityUserStore = identityUserStore ?? throw new ArgumentNullException(nameof(identityUserStore));

    /// <summary>
    /// Assigns the requested role to the target subject.
    /// </summary>
    /// <param name="command">The role assignment command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The role assignment result.</returns>
    public async Task<AssignIdentityRoleResult> HandleAsync(AssignIdentityRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Role);

        var normalizedRole = IdentityAppRoles.Normalize(command.Role);
        var created = await _identityUserStore.EnsureRoleAssignedAsync(command.Subject, normalizedRole, cancellationToken);
        await _identityUserStore.SaveChangesAsync(cancellationToken);

        return new AssignIdentityRoleResult(command.Subject, normalizedRole, created);
    }
}
