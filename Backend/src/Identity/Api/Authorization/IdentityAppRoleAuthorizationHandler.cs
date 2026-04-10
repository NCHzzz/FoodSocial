using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Application;
using FoodSocial.Identity.Domain;
using Microsoft.AspNetCore.Authorization;

namespace FoodSocial.Identity.Api.Authorization;

/// <summary>
/// Evaluates account-level role requirements against Identity-owned role assignments.
/// </summary>
public sealed class IdentityAppRoleAuthorizationHandler(IIdentityUserStore identityUserStore)
    : AuthorizationHandler<IdentityAppRoleRequirement>
{
    private readonly IIdentityUserStore _identityUserStore = identityUserStore ?? throw new ArgumentNullException(nameof(identityUserStore));

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IdentityAppRoleRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var subject = IdentityClaimReader.GetSubject(context.User);

        if (string.IsNullOrWhiteSpace(subject))
        {
            return;
        }

        var roleNames = await _identityUserStore.GetRoleNamesBySubjectAsync(subject, CancellationToken.None);

        if (roleNames.Select(IdentityAppRoles.Normalize).Any(requirement.AllowedRoles.Contains))
        {
            context.Succeed(requirement);
        }
    }
}
