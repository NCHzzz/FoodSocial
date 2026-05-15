namespace FoodSocial.Bff;

internal static class BffPlaceEndpoints
{
    public static IEndpointRouteBuilder MapBffPlaceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var placesGroup = endpoints.MapGroup("/api/places");

        placesGroup.MapGet("/search", ForwardPublicAsync);
        placesGroup.MapGet("/{placeId:guid}", ForwardPublicAsync);

        return endpoints;
    }

    private static Task<IResult> ForwardPublicAsync(
        HttpContext httpContext,
        PlaceHttpClient placeClient,
        BffCatalogResponseCache catalogCache,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
        ForwardAsync(httpContext, placeClient, catalogCache, loggerFactory, cancellationToken);

    private static async Task<IResult> ForwardAsync(
        HttpContext httpContext,
        PlaceHttpClient placeClient,
        BffCatalogResponseCache catalogCache,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(placeClient);
        ArgumentNullException.ThrowIfNull(catalogCache);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var targetPathAndQuery = $"{httpContext.Request.Path}{httpContext.Request.QueryString}";
        var cachedResponse = await catalogCache.TryGetAsync("place", httpContext.Request, targetPathAndQuery, cancellationToken);
        if (cachedResponse is not null)
        {
            return new BffUpstreamHttpResult(cachedResponse.StatusCode, cachedResponse.ContentType, cachedResponse.Body);
        }

        using var upstreamResponse = await ForwardToPlaceApiAsync(httpContext, placeClient, loggerFactory, targetPathAndQuery, cancellationToken);
        if (upstreamResponse is null)
        {
            return CreateUnavailableProblem(httpContext);
        }

        var contentType = upstreamResponse.Content.Headers.ContentType?.ToString();
        var responseBytes = await upstreamResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        var statusCode = (int)upstreamResponse.StatusCode;

        await catalogCache.StoreAsync("place", httpContext.Request, targetPathAndQuery, statusCode, contentType, responseBytes, cancellationToken);

        return new BffUpstreamHttpResult(statusCode, contentType, responseBytes);
    }

    private static async Task<HttpResponseMessage?> ForwardToPlaceApiAsync(
        HttpContext httpContext,
        PlaceHttpClient placeClient,
        ILoggerFactory loggerFactory,
        string targetPathAndQuery,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(typeof(BffPlaceEndpoints).FullName!);

        try
        {
            return await placeClient.ForwardAsync(httpContext.Request, targetPathAndQuery, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Place service request failed for {TargetPathAndQuery}. TraceId: {TraceId}", targetPathAndQuery, httpContext.TraceIdentifier);
            return null;
        }
        catch (InvalidOperationException exception) when (exception.InnerException is OperationCanceledException)
        {
            logger.LogWarning(exception, "Place service request timed out for {TargetPathAndQuery}. TraceId: {TraceId}", targetPathAndQuery, httpContext.TraceIdentifier);
            return null;
        }
    }

    private static IResult CreateUnavailableProblem(HttpContext httpContext) =>
        Results.Problem(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = "https://foodsocial/errors/bff/place-service-unavailable",
            Title = "Place service unavailable",
            Detail = "The BFF could not reach the Place service. Please retry later.",
            Status = StatusCodes.Status502BadGateway,
            Extensions =
            {
                ["traceId"] = httpContext.TraceIdentifier
            }
        });
}
