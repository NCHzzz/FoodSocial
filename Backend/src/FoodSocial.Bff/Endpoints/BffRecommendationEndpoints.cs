using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FoodSocial.Bff;

internal static class BffRecommendationEndpoints
{
    private const int MinimumLimit = 1;

    public static IEndpointRouteBuilder MapBffRecommendationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/recommendations", GetDiscoverRecommendationsAsync);
        return endpoints;
    }

    private static async Task<IResult> GetDiscoverRecommendationsAsync(
        HttpContext httpContext,
        DeterministicDiscoverRecommendationProvider recommendationProvider,
        IOptions<BffRecommendationOptions> options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(recommendationProvider);

        ArgumentNullException.ThrowIfNull(options);

        var recommendationOptions = options.Value;
        var requestedLimit = ResolveRequestedLimit(httpContext.Request.Query["limit"], recommendationOptions.DefaultLimit);
        if (requestedLimit is null)
        {
            return CreateInvalidRequestProblem(httpContext, "Recommendation limit must be a positive integer.");
        }

        var boundedLimit = Math.Clamp(requestedLimit.Value, MinimumLimit, Math.Max(MinimumLimit, recommendationOptions.MaxLimit));
        var request = new DiscoverRecommendationRequest(
            Query: Normalize(httpContext.Request.Query["query"]),
            Location: Normalize(httpContext.Request.Query["location"]),
            Limit: boundedLimit);

        var response = await recommendationProvider.RecommendAsync(request, cancellationToken);
        return Results.Ok(response);
    }

    private static int? ResolveRequestedLimit(string? rawLimit, int defaultLimit)
    {
        if (string.IsNullOrWhiteSpace(rawLimit))
        {
            return defaultLimit;
        }

        return int.TryParse(rawLimit, out var limit) && limit > 0 ? limit : null;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IResult CreateInvalidRequestProblem(HttpContext httpContext, string detail) =>
        Results.Problem(new ProblemDetails
        {
            Type = "https://foodsocial/errors/bff/invalid-recommendation-request",
            Title = "Invalid recommendation request",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        });
}
