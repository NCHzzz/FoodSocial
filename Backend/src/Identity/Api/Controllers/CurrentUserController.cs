using System.Security.Claims;
using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Application;
using FoodSocial.Identity.Api.Authorization;
using FoodSocial.Identity.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Identity.Api.Controllers;

[ApiController]
[Route("identity/current-user")]
public sealed class CurrentUserController(
    EnsureIdentityCommandHandler ensureIdentityCommandHandler,
    IIdentityUserStore identityUserStore)
    : ControllerBase
{
    private readonly EnsureIdentityCommandHandler _ensureIdentityCommandHandler = ensureIdentityCommandHandler ?? throw new ArgumentNullException(nameof(ensureIdentityCommandHandler));
    private readonly IIdentityUserStore _identityUserStore = identityUserStore ?? throw new ArgumentNullException(nameof(identityUserStore));

    [HttpGet]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedIdentityClaims(User, out var claims, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var roles = await _identityUserStore.GetRoleNamesBySubjectAsync(claims.Subject, cancellationToken);
        var normalizedRoles = roles
            .Select(IdentityAppRoles.Normalize)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();

        var identityUser = await _identityUserStore.FindBySubjectAsync(claims.Subject, cancellationToken);
        var lifecyclePolicy = identityUser is null
            ? null
            : IdentityLifecyclePolicy.Describe(identityUser.State);

        var response = new CurrentUserResponse(
            new AuthenticatedUserDto(
                claims.Subject,
                identityUser?.PreferredUsername ?? claims.PreferredUsername,
                identityUser?.Email ?? claims.Email,
                normalizedRoles),
            new SessionIdentityDto(
                IdentityId: identityUser?.Id ?? Guid.Empty,
                State: lifecyclePolicy?.CurrentState.ToString() ?? "Unknown",
                BootstrapPending: lifecyclePolicy?.DownstreamBootstrapPending ?? false,
                DownstreamBootstrapPending: lifecyclePolicy?.DownstreamBootstrapPending ?? false),
            DateTimeOffset.UtcNow);

        return Ok(response);
    }

    [HttpPost("ensure")]
    [Authorize]
    [ProducesResponseType<EnsureIdentityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EnsureIdentityResponse>> EnsureIdentity(
        [FromBody] EnsureIdentityRequest? request,
        CancellationToken cancellationToken)
    {
        _ = request;

        if (!TryGetAuthenticatedIdentityClaims(User, out var claims, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        return await EnsureIdentityForAuthenticatedUserAsync(claims, cancellationToken);
    }

    [HttpPost("internal/ensure")]
    [Authorize(Policy = IdentityAuthorizationPolicies.TrustedInternalService)]
    [ProducesResponseType<EnsureIdentityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EnsureIdentityResponse>> EnsureIdentityInternal(
        [FromBody] TrustedEnsureIdentityRequest? request,
        CancellationToken cancellationToken)
    {
        _ = request;

        if (!TryGetAuthenticatedIdentityClaims(User, out var claims, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        return await EnsureIdentityForAuthenticatedUserAsync(claims, cancellationToken);
    }

    private async Task<ActionResult<EnsureIdentityResponse>> EnsureIdentityForAuthenticatedUserAsync(
        AuthenticatedIdentityClaims claims,
        CancellationToken cancellationToken)
    {
        var response = await _ensureIdentityCommandHandler.HandleAsync(CreateEnsureIdentityCommand(claims), cancellationToken);
        return Ok(response);
    }

    private ActionResult ProblemMissingSubject() =>
        Problem(
            title: "Missing subject claim",
            detail: "The authenticated token does not contain a subject claim.",
            statusCode: StatusCodes.Status401Unauthorized);

    private bool TryGetAuthenticatedIdentityClaims(
        ClaimsPrincipal user,
        out AuthenticatedIdentityClaims claims,
        out ActionResult? failureResult)
    {
        var subject = IdentityClaimReader.GetSubject(user);

        if (string.IsNullOrWhiteSpace(subject))
        {
            claims = default;
            failureResult = ProblemMissingSubject();
            return false;
        }

        claims = new AuthenticatedIdentityClaims(
            subject,
            IdentityClaimReader.GetPreferredUsername(user),
            IdentityClaimReader.GetEmail(user));
        failureResult = null;
        return true;
    }

    private static EnsureIdentityCommand CreateEnsureIdentityCommand(AuthenticatedIdentityClaims claims) =>
        new(
            claims.Subject,
            claims.PreferredUsername,
            claims.Email);

    private readonly record struct AuthenticatedIdentityClaims(
        string Subject,
        string? PreferredUsername,
        string? Email);
}

