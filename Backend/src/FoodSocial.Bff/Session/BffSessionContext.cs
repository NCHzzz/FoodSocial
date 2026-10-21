using FoodSocial.Contracts.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Bff;

/// <summary>
/// Default <see cref="IBffSessionContext"/> implementation. Resolves the authenticated
/// principal, ensures an identity mapping through <see cref="IdentityOrchestrationHttpClient"/>,
/// and returns a session envelope or a short-circuiting problem result.
/// </summary>
internal sealed class BffSessionContext(
    IdentityOrchestrationHttpClient identityClient,
    ILogger<BffSessionContext> logger) : IBffSessionContext
{
    private readonly IdentityOrchestrationHttpClient _identityClient = identityClient ?? throw new ArgumentNullException(nameof(identityClient));
    private readonly ILogger<BffSessionContext> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<ResolvedBffSession?> ResolveAllowedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var authenticateResult = await httpContext.AuthenticateAsync(BffAuthenticationDefaults.Scheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal?.Identity?.IsAuthenticated != true)
        {
            return ResolvedBffSession.Unauthorized();
        }

        var accessToken = await httpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            accessToken = BffRequestConventions.ResolveBearerTokenOrNull(httpContext.Request);
        }
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return ResolvedBffSession.Unauthorized();
        }

        try
        {
            var ensuredIdentity = await _identityClient.EnsureIdentityAsync(
                authenticateResult.Principal,
                accessToken,
                useTrustedInternalEnsure: !BffRequestConventions.HasBearerAuthorizationHeader(httpContext.Request),
                cancellationToken);

            if (string.Equals(ensuredIdentity.State, IdentityWireConstants.LifecycleStateSuspended, StringComparison.Ordinal)
                || string.Equals(ensuredIdentity.State, IdentityWireConstants.LifecycleStateDeleted, StringComparison.Ordinal))
            {
                return ResolvedBffSession.Forbidden(httpContext);
            }

            return ResolvedBffSession.Success(ensuredIdentity.IdentityId, ensuredIdentity.Roles);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "The BFF failed to complete identity orchestration before forwarding a protected request. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
            return ResolvedBffSession.OrchestrationFailed(httpContext);
        }
    }

    public async Task<ResolvedBffSession?> ResolveModeratorAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var session = await ResolveAllowedAsync(httpContext, cancellationToken);
        if (session is null || session.Result is not null)
        {
            return session;
        }

        return HasModeratorRole(session.Roles)
            ? session
            : ResolvedBffSession.ModerationForbidden(httpContext);
    }

    private static bool HasModeratorRole(IReadOnlyList<string> roles) =>
        roles.Any(role => string.Equals(role, IdentityWireConstants.RoleModerator, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, IdentityWireConstants.RoleAdmin, StringComparison.OrdinalIgnoreCase));
}
