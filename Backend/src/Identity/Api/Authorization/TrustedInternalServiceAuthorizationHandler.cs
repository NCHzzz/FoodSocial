using FoodSocial.Contracts.Identity;
using Microsoft.AspNetCore.Authorization;

namespace FoodSocial.Identity.Api.Authorization;

/// <summary>
/// Evaluates service-to-service Keycloak client-credentials JWT callers.
/// </summary>
public sealed class TrustedInternalServiceAuthorizationHandler(IConfiguration configuration)
    : AuthorizationHandler<TrustedInternalServiceRequirement>
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TrustedInternalServiceRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var allowedClientId = _configuration["Identity:TrustedServiceClients:BffClientId"]
            ?? throw new InvalidOperationException("Identity trusted BFF service client id is not configured.");
        var authorizedParty = IdentityClaimReader.GetAuthorizedParty(context.User);
        if (string.Equals(authorizedParty, allowedClientId, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
