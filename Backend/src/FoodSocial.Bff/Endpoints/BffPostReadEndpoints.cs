using System.Text.Json;
using FoodSocial.Contracts.Posts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Bff;

/// <summary>
/// Public read endpoints for Posts: search, get-by-id, discover feed, following feed,
/// and authenticated media read.
/// </summary>
internal static class BffPostReadEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapBffPostReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api");

        group.MapGet("/posts/search", ForwardSearchAsync);
        group.MapGet("/posts/{postId:guid}", ForwardGetByIdAsync);
        group.MapGet("/feed/discover", ForwardDiscoverAsync);
        group.MapGet("/feed/following", ComposeFollowingFeedAsync);
        group.MapGet("/media/uploads/{uploadId:guid}/{variant}", ForwardMediaReadAsync);

        return endpoints;
    }

    private static Task<IResult> ForwardSearchAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        CancellationToken cancellationToken) =>
        postProxy.ForwardGetAsync(
            CreatePublicForwardPathAndQuery(httpContext),
            httpContext,
            Guid.Empty,
            Array.Empty<string>(),
            cancellationToken);

    private static Task<IResult> ForwardGetByIdAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        CancellationToken cancellationToken) =>
        postProxy.ForwardAsync(
            httpContext,
            $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
            Guid.Empty,
            Array.Empty<string>(),
            cancellationToken);

    private static Task<IResult> ForwardDiscoverAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        CancellationToken cancellationToken) =>
        postProxy.ForwardAsync(
            httpContext,
            $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
            Guid.Empty,
            Array.Empty<string>(),
            cancellationToken);

    private static async Task<IResult> ComposeFollowingFeedAsync(
        HttpContext httpContext,
        [FromServices] FollowingFeedComposer followingFeedComposer,
        [FromServices] IBffSessionContext sessionContext,
        int? limit,
        CancellationToken cancellationToken)
    {
        var session = await sessionContext.ResolveAllowedAsync(httpContext, cancellationToken);
        if (session is null) return Results.Unauthorized();
        if (session.Result is not null) return session.Result;

        var composed = await followingFeedComposer.ComposeAsync(session.IdentityId, limit, cancellationToken);
        if (composed is null) return BffProblemResults.UserProfileServiceUnavailable(httpContext);
        return Results.Ok(composed);
    }

    private static async Task<IResult> ForwardMediaReadAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        [FromServices] IBffSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var authenticateResult = await httpContext.AuthenticateAsync(BffAuthenticationDefaults.Scheme);
        if (authenticateResult.Succeeded && authenticateResult.Principal?.Identity?.IsAuthenticated == true)
        {
            var session = await sessionContext.ResolveAllowedAsync(httpContext, cancellationToken);
            if (session is null) return Results.Unauthorized();
            if (session.Result is not null) return session.Result;
            return await postProxy.ForwardAsync(
                httpContext,
                $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
                session.IdentityId,
                session.Roles,
                cancellationToken);
        }

        return await postProxy.ForwardAsync(
            httpContext,
            $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
            Guid.Empty,
            Array.Empty<string>(),
            cancellationToken);
    }

    private static string CreatePublicForwardPathAndQuery(HttpContext httpContext)
    {
        if (!string.Equals(httpContext.Request.Path.Value, "/api/posts/search", StringComparison.OrdinalIgnoreCase))
        {
            return $"{httpContext.Request.Path}{httpContext.Request.QueryString}";
        }

        var queryItems = new List<KeyValuePair<string, string?>>();
        foreach (var item in httpContext.Request.Query)
        {
            if (string.Equals(item.Key, "includeHidden", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var value in item.Value)
            {
                queryItems.Add(new KeyValuePair<string, string?>(item.Key, value));
            }
        }

        queryItems.Add(new KeyValuePair<string, string?>("includeHidden", bool.FalseString));
        return $"{httpContext.Request.Path}{QueryString.Create(queryItems)}";
    }
}
