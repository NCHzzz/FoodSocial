namespace FoodSocial.Bff;

internal static class BffDishEndpoints
{
    public static IEndpointRouteBuilder MapBffDishEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var dishesGroup = endpoints.MapGroup("/api/dishes");

        dishesGroup.MapGet("/search", ForwardPublicAsync);
        dishesGroup.MapGet("/{dishId:guid}", ForwardPublicAsync);

        return endpoints;
    }

    private static Task<IResult> ForwardPublicAsync(
        HttpContext httpContext,
        DishHttpClient dishClient,
        BffCatalogResponseCache catalogCache,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
        ForwardAsync(httpContext, dishClient, catalogCache, loggerFactory, cancellationToken);

    private static async Task<IResult> ForwardAsync(
        HttpContext httpContext,
        DishHttpClient dishClient,
        BffCatalogResponseCache catalogCache,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(dishClient);
        ArgumentNullException.ThrowIfNull(catalogCache);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var targetPathAndQuery = $"{httpContext.Request.Path}{httpContext.Request.QueryString}";
        var cachedResponse = await catalogCache.TryGetAsync("dish", httpContext.Request, targetPathAndQuery, cancellationToken);
        if (cachedResponse is not null)
        {
            return new BffUpstreamHttpResult(cachedResponse.StatusCode, cachedResponse.ContentType, cachedResponse.Body);
        }

        using var upstreamResponse = await ForwardToDishApiAsync(httpContext, dishClient, loggerFactory, targetPathAndQuery, cancellationToken);
        if (upstreamResponse is null)
        {
            return CreateUnavailableProblem(httpContext);
        }

        var contentType = upstreamResponse.Content.Headers.ContentType?.ToString();
        var responseBytes = await upstreamResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        var statusCode = (int)upstreamResponse.StatusCode;

        await catalogCache.StoreAsync("dish", httpContext.Request, targetPathAndQuery, statusCode, contentType, responseBytes, cancellationToken);

        return new BffUpstreamHttpResult(statusCode, contentType, responseBytes);
    }

    private static async Task<HttpResponseMessage?> ForwardToDishApiAsync(
        HttpContext httpContext,
        DishHttpClient dishClient,
        ILoggerFactory loggerFactory,
        string targetPathAndQuery,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(typeof(BffDishEndpoints).FullName!);

        try
        {
            return await dishClient.ForwardAsync(httpContext.Request, targetPathAndQuery, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Dish service request failed for {TargetPathAndQuery}. TraceId: {TraceId}", targetPathAndQuery, httpContext.TraceIdentifier);
            return null;
        }
        catch (InvalidOperationException exception) when (exception.InnerException is OperationCanceledException)
        {
            logger.LogWarning(exception, "Dish service request timed out for {TargetPathAndQuery}. TraceId: {TraceId}", targetPathAndQuery, httpContext.TraceIdentifier);
            return null;
        }
    }

    private static IResult CreateUnavailableProblem(HttpContext httpContext) =>
        Results.Problem(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = "https://foodsocial/errors/bff/dish-service-unavailable",
            Title = "Dish service unavailable",
            Detail = "The BFF could not reach the Dish service. Please retry later.",
            Status = StatusCodes.Status502BadGateway,
            Extensions =
            {
                ["traceId"] = httpContext.TraceIdentifier
            }
        });
}
