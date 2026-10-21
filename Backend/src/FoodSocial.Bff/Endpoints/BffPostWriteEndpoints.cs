using System.Text.Json;
using FoodSocial.Contracts.Posts;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Bff;

/// <summary>
/// Authenticated write endpoints for Posts: create, like/unlike, comment, repost, media upload.
/// </summary>
internal static class BffPostWriteEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapBffPostWriteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api")
            .AddEndpointFilter(BffCsrfEndpointFilter.Instance);

        group.MapPost("/posts", ForwardCreatePostAsync);
        group.MapPost("/posts/{postId:guid}/likes", ForwardActorMutationAsync);
        group.MapDelete("/posts/{postId:guid}/likes", ForwardActorMutationAsync);
        group.MapPost("/posts/{postId:guid}/comments", ForwardCreateCommentAsync);
        group.MapDelete("/comments/{commentId:guid}", ForwardDeleteCommentAsync);
        group.MapPost("/posts/{postId:guid}/repost", ForwardCreateRepostAsync);
        group.MapPost("/media/uploads", ForwardMediaUploadAsync);

        return endpoints;
    }

    private static async Task<IResult> ForwardCreatePostAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        [FromServices] IBffSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var session = await sessionContext.ResolveAllowedAsync(httpContext, cancellationToken);
        if (session is null) return Results.Unauthorized();
        if (session.Result is not null) return session.Result;

        var deserialized = await DeserializeOrProblemAsync<CreatePostRequest>(httpContext, "Post request body is required.", cancellationToken);
        if (deserialized.Result is not null) return deserialized.Result;
        var securedRequest = deserialized.Value! with { AuthorId = session.IdentityId };
        return await postProxy.ForwardJsonAsync(
            httpContext,
            HttpMethods.Post,
            $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
            securedRequest,
            session.IdentityId,
            session.Roles,
            cancellationToken);
    }

    private static async Task<IResult> ForwardMediaUploadAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        [FromServices] IBffSessionContext sessionContext,
        CancellationToken cancellationToken)
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

    private static async Task<IResult> ForwardActorMutationAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        [FromServices] IBffSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var session = await sessionContext.ResolveAllowedAsync(httpContext, cancellationToken);
        if (session is null) return Results.Unauthorized();
        if (session.Result is not null) return session.Result;

        return await postProxy.ForwardJsonAsync(
            httpContext,
            HttpMethods.Post,
            $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
            new ActorRequest(session.IdentityId),
            session.IdentityId,
            session.Roles,
            cancellationToken);
    }

    private static async Task<IResult> ForwardCreateCommentAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        [FromServices] IBffSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var session = await sessionContext.ResolveAllowedAsync(httpContext, cancellationToken);
        if (session is null) return Results.Unauthorized();
        if (session.Result is not null) return session.Result;

        var deserialized = await DeserializeOrProblemAsync<CreatePostCommentRequest>(httpContext, "Comment request body is required.", cancellationToken);
        if (deserialized.Result is not null) return deserialized.Result;
        var securedRequest = deserialized.Value! with { AuthorId = session.IdentityId };
        return await postProxy.ForwardJsonAsync(
            httpContext,
            HttpMethods.Post,
            $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
            securedRequest,
            session.IdentityId,
            session.Roles,
            cancellationToken);
    }

    private static async Task<IResult> ForwardDeleteCommentAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        [FromServices] IBffSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var session = await sessionContext.ResolveAllowedAsync(httpContext, cancellationToken);
        if (session is null) return Results.Unauthorized();
        if (session.Result is not null) return session.Result;

        return await postProxy.ForwardJsonAsync(
            httpContext,
            HttpMethods.Delete,
            $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
            new DeletePostCommentRequest(session.IdentityId),
            session.IdentityId,
            session.Roles,
            cancellationToken);
    }

    private static async Task<IResult> ForwardCreateRepostAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        [FromServices] IBffSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var session = await sessionContext.ResolveAllowedAsync(httpContext, cancellationToken);
        if (session is null) return Results.Unauthorized();
        if (session.Result is not null) return session.Result;

        var deserialized = await DeserializeOrProblemAsync<CreatePostRepostRequest>(httpContext, "Repost request body is required.", cancellationToken);
        if (deserialized.Result is not null) return deserialized.Result;
        var securedRequest = deserialized.Value! with { ActorId = session.IdentityId };
        return await postProxy.ForwardJsonAsync(
            httpContext,
            HttpMethods.Post,
            $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
            securedRequest,
            session.IdentityId,
            session.Roles,
            cancellationToken);
    }

    private static async Task<DeserializedRequest<TRequest>> DeserializeOrProblemAsync<TRequest>(HttpContext httpContext, string emptyBodyDetail, CancellationToken cancellationToken)
    {
        try
        {
            var request = await JsonSerializer.DeserializeAsync<TRequest>(httpContext.Request.Body, JsonOptions, cancellationToken);
            return request is null
                ? DeserializedRequest<TRequest>.Problem(BffProblemResults.InvalidRequest(httpContext, emptyBodyDetail))
                : DeserializedRequest<TRequest>.Success(request);
        }
        catch (JsonException)
        {
            return DeserializedRequest<TRequest>.Problem(BffProblemResults.InvalidRequest(httpContext, "Request body must be valid JSON."));
        }
    }

    private sealed record DeserializedRequest<TRequest>(TRequest? Value, IResult? Result)
    {
        internal static DeserializedRequest<TRequest> Success(TRequest value) => new(value, null);
        internal static DeserializedRequest<TRequest> Problem(IResult result) => new(default, result);
    }
}
