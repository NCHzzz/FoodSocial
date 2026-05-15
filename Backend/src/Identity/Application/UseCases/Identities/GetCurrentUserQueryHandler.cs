using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Domain;

namespace FoodSocial.Identity.Application.UseCases.Identities;

/// <summary>
/// Builds the current-user response from trusted authentication claims and Identity-owned state.
/// </summary>
public sealed class GetCurrentUserQueryHandler(IIdentityUserStore identityUserStore)
{
    private readonly IIdentityUserStore _identityUserStore = identityUserStore ?? throw new ArgumentNullException(nameof(identityUserStore));

    /// <summary>
    /// Resolves the current-user response for an authenticated subject.
    /// </summary>
    /// <param name="query">The trusted current-user query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current-user response.</returns>
    public async Task<CurrentUserResponse> HandleAsync(CurrentUserQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Subject);

        var roles = await _identityUserStore.GetRoleNamesBySubjectAsync(query.Subject, cancellationToken);
        var normalizedRoles = roles
            .Select(IdentityAppRoles.Normalize)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();

        var identityUser = await _identityUserStore.FindBySubjectAsync(query.Subject, cancellationToken);
        var lifecyclePolicy = identityUser is null
            ? null
            : IdentityLifecyclePolicy.Describe(identityUser.State);

        return new CurrentUserResponse(
            new AuthenticatedUserDto(
                query.Subject,
                identityUser?.PreferredUsername ?? query.PreferredUsername,
                identityUser?.Email ?? query.Email,
                normalizedRoles),
            new SessionIdentityDto(
                IdentityId: identityUser?.Id ?? Guid.Empty,
                State: lifecyclePolicy?.CurrentState.ToString() ?? "Unknown",
                BootstrapPending: lifecyclePolicy?.DownstreamBootstrapPending ?? false,
                DownstreamBootstrapPending: lifecyclePolicy?.DownstreamBootstrapPending ?? false),
            DateTimeOffset.UtcNow);
    }
}
