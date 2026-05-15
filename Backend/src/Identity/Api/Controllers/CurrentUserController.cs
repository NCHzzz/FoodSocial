using System.Security.Claims;
using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Identity.Api.Controllers;

[ApiController]
[Route("identity/current-user")]
public sealed class CurrentUserController(
    EnsureIdentityCommandHandler ensureIdentityCommandHandler,
    GetCurrentUserQueryHandler getCurrentUserQueryHandler)
    : ControllerBase
{
    private readonly EnsureIdentityCommandHandler _ensureIdentityCommandHandler = ensureIdentityCommandHandler ?? throw new ArgumentNullException(nameof(ensureIdentityCommandHandler));
    private readonly GetCurrentUserQueryHandler _getCurrentUserQueryHandler = getCurrentUserQueryHandler ?? throw new ArgumentNullException(nameof(getCurrentUserQueryHandler));

    [HttpGet]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedIdentityClaims(User, out var claims, out var missingSubjectResult))
        {
            return missingSubjectResult!;
        }

        var response = await _getCurrentUserQueryHandler.HandleAsync(
            new CurrentUserQuery(claims.Subject, claims.PreferredUsername, claims.Email),
            cancellationToken);

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
        if (request is null || string.IsNullOrWhiteSpace(request.Subject))
        {
            return Problem(
                title: "Missing subject claim",
                detail: "The trusted internal ensure request must include the browser-authenticated subject.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return await EnsureIdentityForAuthenticatedUserAsync(
            new AuthenticatedIdentityClaims(request.Subject, request.PreferredUsername, request.Email),
            cancellationToken);
    }

    private async Task<ActionResult<EnsureIdentityResponse>> EnsureIdentityForAuthenticatedUserAsync(
        AuthenticatedIdentityClaims claims,
        CancellationToken cancellationToken)
    {
        var response = await _ensureIdentityCommandHandler.HandleAsync(CreateEnsureIdentityCommand(claims), cancellationToken);
        return Ok(response);
    }

    private ActionResult ProblemMissingSubject() =>
        this.FoodSocialProblem(
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
