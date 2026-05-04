using Microsoft.AspNetCore.Authentication;
using FoodSocial.Contracts.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Bff;

internal static class BffUserProfileEndpoints
{
    public static IEndpointRouteBuilder MapBffUserProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var usersGroup = endpoints.MapGroup("/api/users");

        usersGroup.MapGet("/{userId:guid}", ForwardPublicAsync);
        usersGroup.MapGet("/{userId:guid}/followers", ForwardPublicAsync);
        usersGroup.MapGet("/{userId:guid}/following", ForwardPublicAsync);

        usersGroup.MapPatch("/{userId:guid}", ForwardProtectedAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        usersGroup.MapPost("/{userId:guid}/follow", ForwardProtectedAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        usersGroup.MapDelete("/{userId:guid}/follow", ForwardProtectedAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        usersGroup.MapGet("/{userId:guid}/saved", ForwardProtectedAsync);
        usersGroup.MapGet("/{userId:guid}/notifications", ForwardProtectedAsync);
        usersGroup.MapPatch("/{userId:guid}/notifications/{notificationId:guid}/read", ForwardProtectedAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        usersGroup.MapPut("/{userId:guid}/saved/posts/{postId:guid}", ForwardProtectedAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        usersGroup.MapDelete("/{userId:guid}/saved/posts/{postId:guid}", ForwardProtectedAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        usersGroup.MapPut("/{userId:guid}/saved/dishes/{dishId:guid}", ForwardProtectedAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        usersGroup.MapDelete("/{userId:guid}/saved/dishes/{dishId:guid}", ForwardProtectedAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        usersGroup.MapPut("/{userId:guid}/saved/places/{placeId:guid}", ForwardProtectedAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        usersGroup.MapDelete("/{userId:guid}/saved/places/{placeId:guid}", ForwardProtectedAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);

        return endpoints;
    }

    private static Task<IResult> ForwardPublicAsync(
        HttpContext httpContext,
        UserProfileHttpClient userProfileClient,
        CancellationToken cancellationToken) =>
        ForwardAsync(httpContext, userProfileClient, accessToken: null, cancellationToken);

    private static async Task<IResult> ForwardProtectedAsync(
        HttpContext httpContext,
        UserProfileHttpClient userProfileClient,
        IdentityOrchestrationHttpClient identityClient,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(userProfileClient);
        ArgumentNullException.ThrowIfNull(identityClient);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var logger = loggerFactory.CreateLogger(typeof(BffUserProfileEndpoints).FullName!);

        var authenticateResult = await httpContext.AuthenticateAsync(BffAuthenticationDefaults.Scheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal?.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var accessToken = await UserProfileHttpClient.ResolveAccessTokenAsync(httpContext);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Results.Unauthorized();
        }

        EnsureIdentityResponse ensuredIdentity;
        try
        {
            ensuredIdentity = await identityClient.EnsureIdentityAsync(
                authenticateResult.Principal,
                accessToken,
                useTrustedInternalEnsure: !BffRequestConventions.HasBearerAuthorizationHeader(httpContext.Request),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "The BFF failed to complete identity orchestration before forwarding a protected UserProfile request. TraceId: {TraceId}",
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

        return await ForwardAsync(httpContext, userProfileClient, accessToken, cancellationToken);
    }

    private static bool IsIdentitySessionAllowed(EnsureIdentityResponse ensuredIdentity)
    {
        ArgumentNullException.ThrowIfNull(ensuredIdentity);

        return !string.Equals(ensuredIdentity.State, "Suspended", StringComparison.Ordinal)
            && !string.Equals(ensuredIdentity.State, "Deleted", StringComparison.Ordinal);
    }

    private static async Task<IResult> ForwardAsync(
        HttpContext httpContext,
        UserProfileHttpClient userProfileClient,
        string? accessToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(userProfileClient);

        var targetPathAndQuery = $"{httpContext.Request.Path}{httpContext.Request.QueryString}";
        using var upstreamResponse = await userProfileClient.ForwardAsync(
            httpContext.Request,
            targetPathAndQuery,
            accessToken,
            cancellationToken);

        var contentType = upstreamResponse.Content.Headers.ContentType?.ToString();
        var responseBytes = await upstreamResponse.Content.ReadAsByteArrayAsync(cancellationToken);

        return new UpstreamHttpResult((int)upstreamResponse.StatusCode, contentType, responseBytes);
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

    private static ProblemDetails CreateProblemDetails(
        string title,
        string detail,
        int statusCode,
        string type,
        string traceId) =>
        new()
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Type = type,
            Extensions =
            {
                ["traceId"] = traceId
            }
        };

    private sealed class UpstreamHttpResult(int statusCode, string? contentType, byte[] body) : IResult
    {
        private readonly byte[] _body = body ?? throw new ArgumentNullException(nameof(body));

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            httpContext.Response.StatusCode = statusCode;
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                httpContext.Response.ContentType = contentType;
            }

            if (_body.Length > 0)
            {
                await httpContext.Response.Body.WriteAsync(_body, httpContext.RequestAborted);
            }
        }
    }
}
