using System.Security.Claims;
using FoodSocial.Contracts.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
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

        if (!TryCreateRedirectProperties(returnUrl, out var redirectProperties))
        {
            return CreateInvalidReturnUrlResult(httpContext);
        }

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            return Results.LocalRedirect(redirectProperties.RedirectUri!);
        }

        return Results.Challenge(redirectProperties, [OpenIdConnectDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> GetSessionAsync(
        HttpContext httpContext,
        IdentityOrchestrationHttpClient identityClient,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(identityClient);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var logger = loggerFactory.CreateLogger(typeof(BffAuthEndpoints).FullName!);

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
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "The BFF failed to complete identity orchestration for the current session request. TraceId: {TraceId}",
                httpContext.TraceIdentifier);

            return Results.Problem(CreateProblemDetails(
                title: "Identity orchestration failed",
                detail: "The BFF could not complete the identity orchestration request.",
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://foodsocial/errors/bff/identity-orchestration-failed",
                traceId: httpContext.TraceIdentifier));
        }

        if (!IsIdentitySessionAllowed(ensuredIdentity))
        {
            return CreateForbiddenIdentitySessionResult(httpContext);
        }

        var sessionResponse = BuildCurrentUserResponse(principal, ensuredIdentity);
        return Results.Ok(sessionResponse);
    }

    private static async Task<IResult> RefreshAsync(
        HttpContext httpContext,
        IdentityOrchestrationHttpClient identityClient,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(identityClient);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var logger = loggerFactory.CreateLogger(typeof(BffAuthEndpoints).FullName!);

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
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "The BFF failed to complete identity orchestration for the current refresh request. TraceId: {TraceId}",
                httpContext.TraceIdentifier);

            return Results.Problem(CreateProblemDetails(
                title: "Identity orchestration failed",
                detail: "The BFF could not complete the identity orchestration request.",
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://foodsocial/errors/bff/identity-orchestration-failed",
                traceId: httpContext.TraceIdentifier));
        }

        if (!IsIdentityRefreshAllowed(ensuredIdentity))
        {
            return CreateForbiddenIdentitySessionResult(httpContext);
        }

        return Results.Ok(new RefreshSessionResponse(
            Refreshed: false,
            SessionValid: true,
            ReauthenticationRequired: false,
            Reason: "The current authenticated session remains valid and no interactive renewal was performed.",
            ServerTimeUtc: DateTimeOffset.UtcNow));
    }

    private static IResult Logout(HttpContext httpContext, string? returnUrl)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!TryCreateRedirectProperties(returnUrl, out var redirectProperties))
        {
            return CreateInvalidReturnUrlResult(httpContext);
        }

        return Results.SignOut(redirectProperties, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
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

    private static bool IsIdentitySessionAllowed(EnsureIdentityResponse ensuredIdentity)
    {
        ArgumentNullException.ThrowIfNull(ensuredIdentity);

        return !string.Equals(ensuredIdentity.State, "Suspended", StringComparison.Ordinal)
            && !string.Equals(ensuredIdentity.State, "Deleted", StringComparison.Ordinal);
    }

    private static bool IsIdentityRefreshAllowed(EnsureIdentityResponse ensuredIdentity) =>
        IsIdentitySessionAllowed(ensuredIdentity);

    private static async Task<string?> ResolveAccessTokenAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var accessToken = await httpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return accessToken;
        }

        return BffRequestConventions.ResolveBearerTokenOrNull(httpContext.Request);
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

    private static IResult CreateInvalidReturnUrlResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return Results.BadRequest(CreateProblemDetails(
            title: "Invalid return URL",
            detail: "The return URL must be a relative path within the BFF.",
            statusCode: StatusCodes.Status400BadRequest,
            type: "https://foodsocial/errors/bff/invalid-return-url",
            traceId: httpContext.TraceIdentifier));
    }

    private static IResult CreateForbiddenIdentitySessionResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return Results.Problem(CreateProblemDetails(
            title: "Identity session is not allowed",
            detail: "The current identity state is not allowed to use this session.",
            statusCode: StatusCodes.Status403Forbidden,
            type: "https://foodsocial/errors/bff/identity-session-forbidden",
            traceId: httpContext.TraceIdentifier));
    }

    private static bool TryCreateRedirectProperties(string? returnUrl, out AuthenticationProperties authenticationProperties)
    {
        if (!BffRequestConventions.TryNormalizeReturnUrl(returnUrl, out var safeReturnUrl))
        {
            authenticationProperties = default!;
            return false;
        }

        authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = safeReturnUrl,
        };

        return true;
    }

    private static ProblemDetails CreateProblemDetails(
        string title,
        string detail,
        int statusCode,
        string type,
        string traceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        return new ProblemDetails
        {
            Type = type,
            Title = title,
            Detail = detail,
            Status = statusCode,
            Extensions =
            {
                ["traceId"] = traceId,
            },
        };
    }
}
