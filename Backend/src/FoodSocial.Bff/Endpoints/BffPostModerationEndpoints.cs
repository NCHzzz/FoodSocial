using System.Text.Json;
using FoodSocial.Contracts.Posts;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Bff;

/// <summary>
/// Moderator-protected Post endpoints: report intake and admin report review/resolve.
/// </summary>
internal static class BffPostModerationEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapBffPostModerationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api");

group.MapPost("/posts/{postId:guid}/report", ForwardCreateReportAsync)
.AddEndpointFilter(BffCsrfEndpointFilter.Instance);
group.MapPost("/comments/{commentId:guid}/report", ForwardCreateReportAsync)
            .AddEndpointFilter(BffCsrfEndpointFilter.Instance);
        group.MapPost("/users/{userId:guid}/report", ForwardCreateReportAsync)
            .AddEndpointFilter(BffCsrfEndpointFilter.Instance);

        group.MapGet("/me/notifications", ForwardNotificationsReadAsync);

        group.MapPatch("/notifications/{notificationId:guid}/read", ForwardNotificationReadMutationAsync)
            .AddEndpointFilter(BffCsrfEndpointFilter.Instance);

        group.MapGet("/admin/reports", ForwardModeratorReadAsync);
        group.MapPost("/admin/reports/{reportId:guid}/resolve", ForwardResolveReportAsync)
            .AddEndpointFilter(BffCsrfEndpointFilter.Instance);

        return endpoints;
    }

    private static async Task<IResult> ForwardCreateReportAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        [FromServices] IBffSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var session = await sessionContext.ResolveAllowedAsync(httpContext, cancellationToken);
        if (session is null) return Results.Unauthorized();
        if (session.Result is not null) return session.Result;

        var deserialized = await DeserializeOrProblemAsync<CreateContentReportRequest>(httpContext, "Report request body is required.", cancellationToken);
        if (deserialized.Result is not null) return deserialized.Result;
        var securedRequest = deserialized.Value! with { ReporterId = session.IdentityId };
        return await postProxy.ForwardJsonAsync(
            httpContext,
            HttpMethods.Post,
            $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
            securedRequest,
            session.IdentityId,
            session.Roles,
            cancellationToken);
    }

    private static async Task<IResult> ForwardModeratorReadAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        [FromServices] IBffSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var session = await sessionContext.ResolveModeratorAsync(httpContext, cancellationToken);
        if (session is null) return Results.Unauthorized();
        if (session.Result is not null) return session.Result;

        return await postProxy.ForwardGetAsync(
            $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
            httpContext,
            session.IdentityId,
            session.Roles,
            cancellationToken);
    }

    private static async Task<IResult> ForwardNotificationsReadAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        [FromServices] IBffSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var session = await sessionContext.ResolveAllowedAsync(httpContext, cancellationToken);
        if (session is null) return Results.Unauthorized();
        if (session.Result is not null) return session.Result;

        var targetPathAndQuery = $"/api/notifications{httpContext.Request.QueryString}";
        return await postProxy.ForwardGetAsync(
            targetPathAndQuery,
            httpContext,
            session.IdentityId,
            session.Roles,
            cancellationToken);
    }

    private static async Task<IResult> ForwardNotificationReadMutationAsync(
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
            HttpMethods.Patch,
            $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
            new ActorRequest(session.IdentityId),
            session.IdentityId,
            session.Roles,
            cancellationToken);
    }

    private static async Task<IResult> ForwardResolveReportAsync(
        HttpContext httpContext,
        [FromServices] BffPostDownstreamProxy postProxy,
        [FromServices] IBffSessionContext sessionContext,
        CancellationToken cancellationToken)
    {
        var session = await sessionContext.ResolveModeratorAsync(httpContext, cancellationToken);
        if (session is null) return Results.Unauthorized();
        if (session.Result is not null) return session.Result;

        var deserialized = await DeserializeOrProblemAsync<ResolveContentReportRequest>(httpContext, "Report resolution request body is required.", cancellationToken);
        if (deserialized.Result is not null) return deserialized.Result;
        var securedRequest = deserialized.Value! with { ModeratorId = session.IdentityId };
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
