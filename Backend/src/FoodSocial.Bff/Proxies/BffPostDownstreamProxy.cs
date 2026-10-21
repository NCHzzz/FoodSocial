using System.Text.Json;

namespace FoodSocial.Bff;

/// <summary>
/// Deep module that owns the BFF-side translation of downstream Post service
/// failures into canonical ProblemDetails, plus the success-side propagation of
/// the upstream response back to the browser.
/// </summary>
/// <remarks>
/// <para>The proxy is the only place in the BFF that decides what an upstream
/// failure becomes. Endpoint handlers stay one-liners that delegate here. The
/// failure-mode rules are:</para>
/// <list type="bullet">
///   <item><see cref="HttpRequestException"/> (connection refused, DNS, 5xx) → 502 <c>post-service-unavailable</c>.</item>
///   <item><see cref="System.Text.Json.JsonException"/> / <see cref="InvalidOperationException"/> from the Post service → 502 <c>post-service-unavailable</c>.</item>
///   <item>Upstream non-2xx → propagated as the upstream status (e.g. 400 stays 400, 404 stays 404).</item>
///   <item>Upstream 2xx → propagated as 200 with the upstream body.</item>
/// </list>
/// <para>Tests at this interface exercise "given an upstream 500, the BFF returns 502
/// with traceId" without spinning up a per-endpoint harness.</para>
/// </remarks>
internal sealed class BffPostDownstreamProxy(
    PostHttpClient postClient,
    ILogger<BffPostDownstreamProxy> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PostHttpClient _postClient = postClient ?? throw new ArgumentNullException(nameof(postClient));
    private readonly ILogger<BffPostDownstreamProxy> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Forwards an inbound GET (or otherwise body-less) call to the Post service
    /// and returns the result as an <see cref="IResult"/>. Used by deep modules
    /// (composers) and by endpoint handlers that own a <see cref="HttpContext"/>.
    /// </summary>
    public async Task<IResult> ForwardGetAsync(
        string targetPathAndQuery,
        HttpContext httpContext,
        Guid identityId,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPathAndQuery);
        try
        {
            var upstream = await _postClient.ForwardGetAsync(targetPathAndQuery, identityId, roles, cancellationToken);
            return ToResult(httpContext, upstream, targetPathAndQuery);
        }
        catch (Exception exception) when (IsDownstreamFailure(exception, cancellationToken))
        {
            return LogAndUnavailable(httpContext, exception, targetPathAndQuery);
        }
    }

    /// <summary>
    /// Forwards an inbound call (with method and optional body taken from the
    /// <see cref="HttpRequest"/>) and returns the result as an <see cref="IResult"/>.
    /// </summary>
    public async Task<IResult> ForwardAsync(
        HttpContext httpContext,
        string targetPathAndQuery,
        Guid identityId,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPathAndQuery);
        try
        {
            var upstream = await _postClient.ForwardAsync(httpContext.Request, targetPathAndQuery, identityId, roles, cancellationToken);
            return ToResult(httpContext, upstream, targetPathAndQuery);
        }
        catch (Exception exception) when (IsDownstreamFailure(exception, cancellationToken))
        {
            return LogAndUnavailable(httpContext, exception, targetPathAndQuery);
        }
    }

    /// <summary>
    /// Forwards a JSON body call (POST/PUT/PATCH) and returns the result as an
    /// <see cref="IResult"/>. The HTTP method is supplied by the caller (the
    /// <see cref="HttpContext"/>-based method is only used to inherit body when present).
    /// </summary>
    public async Task<IResult> ForwardJsonAsync<TRequest>(
        HttpContext httpContext,
        string method,
        string targetPathAndQuery,
        TRequest body,
        Guid identityId,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPathAndQuery);
        try
        {
            var upstream = await _postClient.ForwardJsonAsync(method, targetPathAndQuery, body, identityId, roles, cancellationToken);
            return ToResult(httpContext, upstream, targetPathAndQuery);
        }
        catch (Exception exception) when (IsDownstreamFailure(exception, cancellationToken))
        {
            return LogAndUnavailable(httpContext, exception, targetPathAndQuery);
        }
    }

    private IResult ToResult(HttpContext httpContext, HttpResponseMessage upstream, string targetPathAndQuery)
    {
        var contentType = upstream.Content.Headers.ContentType?.ToString();
        var responseBytes = upstream.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        return new BffUpstreamHttpResult((int)upstream.StatusCode, contentType, responseBytes);
    }

    private IResult LogAndUnavailable(HttpContext httpContext, Exception exception, string targetPathAndQuery)
    {
        _logger.LogWarning(exception, "Post service request failed for {TargetPathAndQuery}. TraceId: {TraceId}", targetPathAndQuery, httpContext.TraceIdentifier);
        return BffProblemResults.PostServiceUnavailable(httpContext);
    }

    private static bool IsDownstreamFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException
        || exception is InvalidOperationException
        || exception is System.Text.Json.JsonException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);
}
