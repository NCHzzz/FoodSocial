using System.Text.Json;
using FoodSocial.Contracts.Identity;
using FoodSocial.Contracts.Posts;
using FoodSocial.Contracts.UserProfiles;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Bff;

internal static class BffPostEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapBffPostEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/posts/search", ForwardAsync);
        endpoints.MapGet("/api/posts/{postId:guid}", ForwardAsync);
        endpoints.MapGet("/api/feed/discover", ForwardAsync);
        endpoints.MapGet("/api/feed/following", ComposeFollowingFeedAsync);
        endpoints.MapGet("/api/media/uploads/{uploadId:guid}/{variant}", ForwardMediaReadAsync);
        endpoints.MapPost("/api/posts", ForwardCreatePostAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        endpoints.MapPost("/api/media/uploads", ForwardMediaUploadAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        endpoints.MapPost("/api/posts/{postId:guid}/likes", ForwardActorMutationAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        endpoints.MapDelete("/api/posts/{postId:guid}/likes", ForwardActorMutationAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        endpoints.MapPost("/api/posts/{postId:guid}/comments", ForwardCreateCommentAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        endpoints.MapDelete("/api/comments/{commentId:guid}", ForwardDeleteCommentAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        endpoints.MapPost("/api/posts/{postId:guid}/repost", ForwardCreateRepostAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        endpoints.MapPost("/api/posts/{postId:guid}/report", ForwardCreateReportAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        endpoints.MapPost("/api/comments/{commentId:guid}/report", ForwardCreateReportAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        endpoints.MapGet("/api/me/notifications", ForwardNotificationsReadAsync);
        endpoints.MapPatch("/api/notifications/{notificationId:guid}/read", ForwardNotificationReadMutationAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        endpoints.MapGet("/api/admin/reports", ForwardModeratorReadAsync);
        endpoints.MapPost("/api/admin/reports/{reportId:guid}/resolve", ForwardResolveReportAsync).AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        return endpoints;
    }

    private static async Task<IResult> ComposeFollowingFeedAsync(
        HttpContext httpContext,
        PostHttpClient postClient,
        UserProfileHttpClient userProfileClient,
        IdentityOrchestrationHttpClient identityClient,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var session = await ResolveAllowedSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session.Result;

        var limit = ResolveFeedLimit(httpContext.Request.Query);
        var followingPath = $"/api/users/{session.IdentityId}/following";
        using var followingResponse = await GetFollowingGraphAsync(httpContext, userProfileClient, loggerFactory, followingPath, cancellationToken);
        if (followingResponse is null) return CreateUserProfileUnavailableProblem(httpContext);
        if (!followingResponse.IsSuccessStatusCode)
        {
            return await CreateUpstreamResultAsync(followingResponse, cancellationToken);
        }

        var following = await followingResponse.Content.ReadFromJsonAsync<UserProfileListResponse>(JsonOptions, cancellationToken);
        if (following is null)
        {
            return CreateUserProfileUnavailableProblem(httpContext);
        }

        if (following.Items.Count == 0)
        {
            return Results.Ok(new FollowingFeedResponse([], limit));
        }

        var posts = new List<PostSummaryResponse>();
        foreach (var followedProfile in following.Items)
        {
            var postQuery = QueryString.Create(
            [
                new KeyValuePair<string, string?>("authorId", followedProfile.IdentityId.ToString()),
                new KeyValuePair<string, string?>("limit", limit.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string?>("includeHidden", bool.FalseString)
            ]);
            var postPathAndQuery = $"/api/posts/search{postQuery}";
            using var postsResponse = await ForwardToPostApiAsync(httpContext, postClient, loggerFactory, postPathAndQuery, session.IdentityId, session.Roles, cancellationToken);
            if (postsResponse is null) return CreateUnavailableProblem(httpContext);
            if (!postsResponse.IsSuccessStatusCode)
            {
                return await CreateUpstreamResultAsync(postsResponse, cancellationToken);
            }

            var authorPosts = await postsResponse.Content.ReadFromJsonAsync<PostSearchResponse>(JsonOptions, cancellationToken);
            if (authorPosts is null)
            {
                return CreateUnavailableProblem(httpContext);
            }

            posts.AddRange(authorPosts.Items);
        }

        var items = posts
            .GroupBy(post => post.Id)
            .Select(group => group.First())
            .OrderByDescending(post => post.CreatedAtUtc)
            .ThenBy(post => post.Id)
            .Take(limit)
            .ToArray();

        return Results.Ok(new FollowingFeedResponse(items, limit));
    }

    private static async Task<IResult> ForwardCreatePostAsync(HttpContext httpContext, PostHttpClient postClient, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var session = await ResolveAllowedSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session.Result;

        var request = await DeserializeOrProblemAsync<CreatePostRequest>(httpContext, "Post request body is required.", cancellationToken);
        if (request.Result is not null) return request.Result;
        var securedRequest = request.Value! with { AuthorId = session.IdentityId };
        return await ForwardJsonAsync(httpContext, postClient, loggerFactory, securedRequest, session.IdentityId, session.Roles, cancellationToken);
    }

    private static async Task<IResult> ForwardMediaUploadAsync(HttpContext httpContext, PostHttpClient postClient, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var session = await ResolveAllowedSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session.Result;

        return await ForwardAuthenticatedAsync(httpContext, postClient, loggerFactory, session.IdentityId, session.Roles, cancellationToken);
    }

    private static async Task<IResult> ForwardMediaReadAsync(HttpContext httpContext, PostHttpClient postClient, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var authenticateResult = await httpContext.AuthenticateAsync(BffAuthenticationDefaults.Scheme);
        if (authenticateResult.Succeeded && authenticateResult.Principal?.Identity?.IsAuthenticated == true)
        {
            var session = await ResolveAllowedSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
            if (session.Result is not null) return session.Result;

            return await ForwardAuthenticatedAsync(httpContext, postClient, loggerFactory, session.IdentityId, session.Roles, cancellationToken);
        }

        return await ForwardAsync(httpContext, postClient, loggerFactory, cancellationToken);
    }

    private static async Task<IResult> ForwardActorMutationAsync(HttpContext httpContext, PostHttpClient postClient, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var session = await ResolveAllowedSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session.Result;

        return await ForwardJsonAsync(httpContext, postClient, loggerFactory, new ActorRequest(session.IdentityId), session.IdentityId, session.Roles, cancellationToken);
    }

    private static async Task<IResult> ForwardCreateCommentAsync(HttpContext httpContext, PostHttpClient postClient, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var session = await ResolveAllowedSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session.Result;

        var request = await DeserializeOrProblemAsync<CreatePostCommentRequest>(httpContext, "Comment request body is required.", cancellationToken);
        if (request.Result is not null) return request.Result;
        var securedRequest = request.Value! with { AuthorId = session.IdentityId };
        return await ForwardJsonAsync(httpContext, postClient, loggerFactory, securedRequest, session.IdentityId, session.Roles, cancellationToken);
    }

    private static async Task<IResult> ForwardDeleteCommentAsync(HttpContext httpContext, PostHttpClient postClient, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var session = await ResolveAllowedSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session.Result;

        return await ForwardJsonAsync(httpContext, postClient, loggerFactory, new DeletePostCommentRequest(session.IdentityId), session.IdentityId, session.Roles, cancellationToken);
    }

    private static async Task<IResult> ForwardCreateRepostAsync(HttpContext httpContext, PostHttpClient postClient, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var session = await ResolveAllowedSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session.Result;

        var request = await DeserializeOrProblemAsync<CreatePostRepostRequest>(httpContext, "Repost request body is required.", cancellationToken);
        if (request.Result is not null) return request.Result;
        var securedRequest = request.Value! with { ActorId = session.IdentityId };
        return await ForwardJsonAsync(httpContext, postClient, loggerFactory, securedRequest, session.IdentityId, session.Roles, cancellationToken);
    }

    private static async Task<IResult> ForwardCreateReportAsync(HttpContext httpContext, PostHttpClient postClient, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var session = await ResolveAllowedSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session.Result;

        var request = await DeserializeOrProblemAsync<CreateContentReportRequest>(httpContext, "Report request body is required.", cancellationToken);
        if (request.Result is not null) return request.Result;
        var securedRequest = request.Value! with { ReporterId = session.IdentityId };
        return await ForwardJsonAsync(httpContext, postClient, loggerFactory, securedRequest, session.IdentityId, session.Roles, cancellationToken);
    }

    private static async Task<IResult> ForwardModeratorReadAsync(HttpContext httpContext, PostHttpClient postClient, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var session = await ResolveModeratorSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session.Result;

        return await ForwardAuthenticatedAsync(httpContext, postClient, loggerFactory, session.IdentityId, session.Roles, cancellationToken);
    }

    private static async Task<IResult> ForwardNotificationsReadAsync(HttpContext httpContext, PostHttpClient postClient, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var session = await ResolveAllowedSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session.Result;

        var targetPathAndQuery = $"/api/notifications{httpContext.Request.QueryString}";
        return await ForwardAuthenticatedAsync(httpContext, postClient, loggerFactory, session.IdentityId, session.Roles, cancellationToken, targetPathAndQuery);
    }

    private static async Task<IResult> ForwardNotificationReadMutationAsync(HttpContext httpContext, PostHttpClient postClient, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var session = await ResolveAllowedSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session.Result;

        return await ForwardJsonAsync(httpContext, postClient, loggerFactory, new ActorRequest(session.IdentityId), session.IdentityId, session.Roles, cancellationToken);
    }

    private static async Task<IResult> ForwardResolveReportAsync(HttpContext httpContext, PostHttpClient postClient, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var session = await ResolveModeratorSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session.Result;

        var request = await DeserializeOrProblemAsync<ResolveContentReportRequest>(httpContext, "Report resolution request body is required.", cancellationToken);
        if (request.Result is not null) return request.Result;
        var securedRequest = request.Value! with { ModeratorId = session.IdentityId };
        return await ForwardJsonAsync(httpContext, postClient, loggerFactory, securedRequest, session.IdentityId, session.Roles, cancellationToken);
    }

    private static async Task<IResult> ForwardAsync(HttpContext httpContext, PostHttpClient postClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext); ArgumentNullException.ThrowIfNull(postClient); ArgumentNullException.ThrowIfNull(loggerFactory);
        var targetPathAndQuery = CreatePublicForwardPathAndQuery(httpContext);
        using var upstreamResponse = await ForwardToPostApiAsync(httpContext, postClient, loggerFactory, targetPathAndQuery, Guid.Empty, [], cancellationToken);
        if (upstreamResponse is null) return CreateUnavailableProblem(httpContext);
        var contentType = upstreamResponse.Content.Headers.ContentType?.ToString();
        var responseBytes = await upstreamResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        return new BffUpstreamHttpResult((int)upstreamResponse.StatusCode, contentType, responseBytes);
    }

    private static async Task<IResult> CreateUpstreamResultAsync(HttpResponseMessage upstreamResponse, CancellationToken cancellationToken)
    {
        var contentType = upstreamResponse.Content.Headers.ContentType?.ToString();
        var responseBytes = await upstreamResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        return new BffUpstreamHttpResult((int)upstreamResponse.StatusCode, contentType, responseBytes);
    }

    private static async Task<IResult> ForwardAuthenticatedAsync(HttpContext httpContext, PostHttpClient postClient, ILoggerFactory loggerFactory, Guid identityId, IReadOnlyList<string> roles, CancellationToken cancellationToken, string? targetPathAndQueryOverride = null)
    {
        var targetPathAndQuery = targetPathAndQueryOverride ?? $"{httpContext.Request.Path}{httpContext.Request.QueryString}";
        using var upstreamResponse = await ForwardToPostApiAsync(httpContext, postClient, loggerFactory, targetPathAndQuery, identityId, roles, cancellationToken);
        if (upstreamResponse is null) return CreateUnavailableProblem(httpContext);
        var contentType = upstreamResponse.Content.Headers.ContentType?.ToString();
        var responseBytes = await upstreamResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        return new BffUpstreamHttpResult((int)upstreamResponse.StatusCode, contentType, responseBytes);
    }

    private static async Task<IResult> ForwardJsonAsync<TRequest>(HttpContext httpContext, PostHttpClient postClient, ILoggerFactory loggerFactory, TRequest request, Guid identityId, IReadOnlyList<string> roles, CancellationToken cancellationToken)
    {
        var targetPathAndQuery = $"{httpContext.Request.Path}{httpContext.Request.QueryString}";
        using var upstreamResponse = await ForwardJsonToPostApiAsync(httpContext, postClient, loggerFactory, targetPathAndQuery, request, identityId, roles, cancellationToken);
        if (upstreamResponse is null) return CreateUnavailableProblem(httpContext);
        var contentType = upstreamResponse.Content.Headers.ContentType?.ToString();
        var responseBytes = await upstreamResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        return new BffUpstreamHttpResult((int)upstreamResponse.StatusCode, contentType, responseBytes);
    }

    private static async Task<HttpResponseMessage?> ForwardToPostApiAsync(HttpContext httpContext, PostHttpClient postClient, ILoggerFactory loggerFactory, string targetPathAndQuery, Guid identityId, IReadOnlyList<string> roles, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(typeof(BffPostEndpoints).FullName!);
        try { return await postClient.ForwardAsync(httpContext.Request, targetPathAndQuery, identityId, roles, cancellationToken); }
        catch (HttpRequestException exception) { logger.LogWarning(exception, "Post service request failed for {TargetPathAndQuery}. TraceId: {TraceId}", targetPathAndQuery, httpContext.TraceIdentifier); return null; }
        catch (InvalidOperationException exception) when (exception.InnerException is OperationCanceledException) { logger.LogWarning(exception, "Post service request timed out for {TargetPathAndQuery}. TraceId: {TraceId}", targetPathAndQuery, httpContext.TraceIdentifier); return null; }
    }

    private static async Task<HttpResponseMessage?> ForwardJsonToPostApiAsync<TRequest>(HttpContext httpContext, PostHttpClient postClient, ILoggerFactory loggerFactory, string targetPathAndQuery, TRequest request, Guid identityId, IReadOnlyList<string> roles, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(typeof(BffPostEndpoints).FullName!);
        try { return await postClient.ForwardJsonAsync(httpContext.Request.Method, targetPathAndQuery, request, identityId, roles, cancellationToken); }
        catch (HttpRequestException exception) { logger.LogWarning(exception, "Post service request failed for {TargetPathAndQuery}. TraceId: {TraceId}", targetPathAndQuery, httpContext.TraceIdentifier); return null; }
        catch (InvalidOperationException exception) when (exception.InnerException is OperationCanceledException) { logger.LogWarning(exception, "Post service request timed out for {TargetPathAndQuery}. TraceId: {TraceId}", targetPathAndQuery, httpContext.TraceIdentifier); return null; }
    }

    private static async Task<HttpResponseMessage?> GetFollowingGraphAsync(HttpContext httpContext, UserProfileHttpClient userProfileClient, ILoggerFactory loggerFactory, string targetPathAndQuery, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(typeof(BffPostEndpoints).FullName!);
        try { return await userProfileClient.GetAsync(targetPathAndQuery, accessToken: null, cancellationToken); }
        catch (HttpRequestException exception) { logger.LogWarning(exception, "UserProfile service request failed for following feed graph {TargetPathAndQuery}. TraceId: {TraceId}", targetPathAndQuery, httpContext.TraceIdentifier); return null; }
        catch (InvalidOperationException exception) when (exception.InnerException is OperationCanceledException) { logger.LogWarning(exception, "UserProfile service request timed out for following feed graph {TargetPathAndQuery}. TraceId: {TraceId}", targetPathAndQuery, httpContext.TraceIdentifier); return null; }
    }

    private static async Task<DeserializedRequest<TRequest>> DeserializeOrProblemAsync<TRequest>(HttpContext httpContext, string emptyBodyDetail, CancellationToken cancellationToken)
    {
        try
        {
            var request = await JsonSerializer.DeserializeAsync<TRequest>(httpContext.Request.Body, JsonOptions, cancellationToken);
            return request is null
                ? DeserializedRequest<TRequest>.Problem(CreateInvalidRequestProblem(httpContext, emptyBodyDetail))
                : DeserializedRequest<TRequest>.Success(request);
        }
        catch (JsonException)
        {
            return DeserializedRequest<TRequest>.Problem(CreateInvalidRequestProblem(httpContext, "Request body must be valid JSON."));
        }
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

    private static int ResolveFeedLimit(IQueryCollection query)
    {
        const int defaultLimit = 20;
        const int maxLimit = 100;

        return int.TryParse(query["limit"].FirstOrDefault(), out var requestedLimit)
            ? Math.Clamp(requestedLimit, 1, maxLimit)
            : defaultLimit;
    }

    private static async Task<ResolvedBffIdentity> ResolveAllowedSessionAsync(HttpContext httpContext, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var authenticateResult = await httpContext.AuthenticateAsync(BffAuthenticationDefaults.Scheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal?.Identity?.IsAuthenticated != true)
        {
            return ResolvedBffIdentity.Unauthorized();
        }

        var accessToken = await httpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(accessToken)) accessToken = BffRequestConventions.ResolveBearerTokenOrNull(httpContext.Request);
        if (string.IsNullOrWhiteSpace(accessToken)) return ResolvedBffIdentity.Unauthorized();

        try
        {
            var ensuredIdentity = await identityClient.EnsureIdentityAsync(
                authenticateResult.Principal,
                accessToken,
                useTrustedInternalEnsure: !BffRequestConventions.HasBearerAuthorizationHeader(httpContext.Request),
                cancellationToken);

            if (string.Equals(ensuredIdentity.State, "Suspended", StringComparison.Ordinal)
                || string.Equals(ensuredIdentity.State, "Deleted", StringComparison.Ordinal))
            {
                return ResolvedBffIdentity.Forbidden(httpContext);
            }

            return ResolvedBffIdentity.Success(ensuredIdentity.IdentityId, ensuredIdentity.Roles);
        }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger(typeof(BffPostEndpoints).FullName!).LogError(exception, "The BFF failed to complete identity orchestration before forwarding a protected Post request. TraceId: {TraceId}", httpContext.TraceIdentifier);
            return ResolvedBffIdentity.Problem(httpContext);
        }
    }

    private static async Task<ResolvedBffIdentity> ResolveModeratorSessionAsync(HttpContext httpContext, IdentityOrchestrationHttpClient identityClient, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var session = await ResolveAllowedSessionAsync(httpContext, identityClient, loggerFactory, cancellationToken);
        if (session.Result is not null) return session;
        return HasModeratorRole(session.Roles)
            ? session
            : ResolvedBffIdentity.ModerationForbidden(httpContext);
    }

    private static bool HasModeratorRole(IReadOnlyList<string> roles) =>
        roles.Any(role => string.Equals(role, "moderator", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase));

    private static IResult CreateUnavailableProblem(HttpContext httpContext) => Results.Problem(new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Type = "https://foodsocial/errors/bff/post-service-unavailable",
        Title = "Post service unavailable",
        Detail = "The BFF could not reach the Post service. Please retry later.",
        Status = StatusCodes.Status502BadGateway,
        Extensions = { ["traceId"] = httpContext.TraceIdentifier }
    });

    private static IResult CreateUserProfileUnavailableProblem(HttpContext httpContext) => Results.Problem(new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Type = "https://foodsocial/errors/bff/user-profile-service-unavailable",
        Title = "UserProfile service unavailable",
        Detail = "The BFF could not reach the UserProfile service. Please retry later.",
        Status = StatusCodes.Status502BadGateway,
        Extensions = { ["traceId"] = httpContext.TraceIdentifier }
    });

    private static IResult CreateInvalidRequestProblem(HttpContext httpContext, string detail) => Results.Problem(new ProblemDetails
    {
        Type = "https://foodsocial/errors/bff/invalid-post-request",
        Title = "Invalid Post request",
        Detail = detail,
        Status = StatusCodes.Status400BadRequest,
        Extensions = { ["traceId"] = httpContext.TraceIdentifier }
    });

    private sealed record DeserializedRequest<TRequest>(TRequest? Value, IResult? Result)
    {
        internal static DeserializedRequest<TRequest> Success(TRequest value) => new(value, null);
        internal static DeserializedRequest<TRequest> Problem(IResult result) => new(default, result);
    }

    private sealed record ResolvedBffIdentity(Guid IdentityId, IReadOnlyList<string> Roles, IResult? Result)
    {
        internal static ResolvedBffIdentity Success(Guid identityId, IReadOnlyList<string> roles) => new(identityId, roles, null);
        internal static ResolvedBffIdentity Unauthorized() => new(Guid.Empty, [], Results.Unauthorized());
        internal static ResolvedBffIdentity Forbidden(HttpContext httpContext) => new(Guid.Empty, [], Results.Problem(new ProblemDetails
        {
            Type = "https://foodsocial/errors/bff/identity-session-forbidden",
            Title = "Identity session is not allowed",
            Detail = "The current identity state is not allowed to use this session.",
            Status = StatusCodes.Status403Forbidden,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }));
        internal static ResolvedBffIdentity ModerationForbidden(HttpContext httpContext) => new(Guid.Empty, [], Results.Problem(new ProblemDetails
        {
            Type = "https://foodsocial/errors/bff/post-moderation-forbidden",
            Title = "Post moderation role required",
            Detail = "A moderator or admin role is required for Post report review.",
            Status = StatusCodes.Status403Forbidden,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }));
        internal static ResolvedBffIdentity Problem(HttpContext httpContext) => new(Guid.Empty, [], Results.Problem(new ProblemDetails
        {
            Type = "https://foodsocial/errors/bff/identity-orchestration-failed",
            Title = "Identity orchestration failed",
            Detail = "The BFF could not complete the identity orchestration request.",
            Status = StatusCodes.Status500InternalServerError,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }));
    }
}
