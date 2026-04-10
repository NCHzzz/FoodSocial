using System.Security.Claims;
using FoodSocial.Contracts.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Bff;

internal static class BffAuthEndpoints
{
    public static IEndpointRouteBuilder MapBffAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var authGroup = endpoints.MapGroup("/api/auth");

        authGroup.MapGet("/login", Login);
        authGroup.MapGet("/session", GetSessionAsync);
        authGroup.MapPost("/refresh", (Delegate)RefreshAsync);
        authGroup.MapPost("/logout", Logout);

        return endpoints;
    }

    private static IResult Login(HttpContext httpContext, string? returnUrl)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!BffRequestConventions.TryNormalizeReturnUrl(returnUrl, out var safeReturnUrl))
        {
            return CreateInvalidReturnUrlResult();
        }

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            return Results.LocalRedirect(safeReturnUrl);
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = safeReturnUrl
        };

        return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> GetSessionAsync(
        HttpContext httpContext,
        IdentityOrchestrationHttpClient identityClient,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(identityClient);

        var principal = await AuthenticateSessionPrincipalAsync(httpContext);
        if (principal is null)
        {
            return CreateUnauthenticatedResult(httpContext);
        }

        var accessToken = await ResolveAccessTokenAsync(httpContext);
        EnsureIdentityResponse ensuredIdentity;
        try
        {
            ensuredIdentity = await identityClient.EnsureIdentityAsync(
                principal,
                accessToken,
                useTrustedInternalEnsure: !BffRequestConventions.HasBearerAuthorizationHeader(httpContext.Request),
                cancellationToken);
        }
        catch (Exception)
        {
            return Results.Problem(
                title: "Identity orchestration failed",
                detail: "The BFF could not complete the identity orchestration request.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var sessionResponse = BuildCurrentUserResponse(principal, ensuredIdentity);
        return Results.Ok(sessionResponse);
    }

    private static async Task<IResult> RefreshAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var principal = await AuthenticateSessionPrincipalAsync(httpContext);
        if (principal is null)
        {
            return CreateUnauthenticatedResult(httpContext);
        }

        return Results.Ok(new RefreshSessionResponse(
            Refreshed: false,
            SessionValid: true,
            ReauthenticationRequired: false,
            Reason: "The current authenticated session remains valid and no interactive renewal was performed.",
            ServerTimeUtc: DateTimeOffset.UtcNow));
    }

    private static IResult Logout(string? returnUrl)
    {
        if (!BffRequestConventions.TryNormalizeReturnUrl(returnUrl, out var safeReturnUrl))
        {
            return CreateInvalidReturnUrlResult();
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = safeReturnUrl
        };

        return Results.SignOut(properties, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
    }

    private static CurrentUserResponse BuildCurrentUserResponse(ClaimsPrincipal user, EnsureIdentityResponse ensuredIdentity)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(ensuredIdentity);

        var roles = ensuredIdentity.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();

        var state = ensuredIdentity.State;
        var bootstrapPending = string.Equals(state, "PendingBootstrap", StringComparison.Ordinal)
            || ensuredIdentity.DownstreamBootstrapPending;

        return new CurrentUserResponse(
            new AuthenticatedUserDto(
                ensuredIdentity.Subject,
                IdentityClaimReader.GetPreferredUsername(user) ?? ensuredIdentity.PreferredUsername,
                IdentityClaimReader.GetEmail(user) ?? ensuredIdentity.Email,
                roles),
            new SessionIdentityDto(
                ensuredIdentity.IdentityId,
                state,
                bootstrapPending,
                ensuredIdentity.DownstreamBootstrapPending),
            DateTimeOffset.UtcNow);
    }

    private static async Task<ClaimsPrincipal?> AuthenticateSessionPrincipalAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var authenticateResult = await httpContext.AuthenticateAsync(BffAuthenticationDefaults.Scheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return authenticateResult.Principal;
    }

    private static async Task<string?> ResolveAccessTokenAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var accessToken = await httpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return accessToken;
        }

        return BffRequestConventions.TryResolveBearerToken(httpContext.Request);
    }

    private static IResult CreateUnauthenticatedResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (BffRequestConventions.HasBearerAuthorizationHeader(httpContext.Request))
        {
            return Results.Unauthorized();
        }

        var redirectUri = $"{httpContext.Request.PathBase}{httpContext.Request.Path}{httpContext.Request.QueryString}";
        return Results.Challenge(
            new AuthenticationProperties
            {
                RedirectUri = string.IsNullOrWhiteSpace(redirectUri) ? "/" : redirectUri
            },
            [OpenIdConnectDefaults.AuthenticationScheme]);
    }

    private static IResult CreateInvalidReturnUrlResult() =>
        Results.BadRequest(new ProblemDetails
        {
            Title = "Invalid return URL",
            Detail = "The return URL must be a relative path within the BFF.",
            Status = StatusCodes.Status400BadRequest
        });
}
