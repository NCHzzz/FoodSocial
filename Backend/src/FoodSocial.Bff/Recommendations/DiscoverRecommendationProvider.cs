using FoodSocial.Contracts.Dishes;
using FoodSocial.Contracts.Places;
using Microsoft.Extensions.Options;

namespace FoodSocial.Bff;

internal interface IDiscoverRecommendationProvider
{
    Task<DiscoverRecommendationsResponse> RecommendAsync(DiscoverRecommendationRequest request, CancellationToken cancellationToken);
}

internal sealed class DeterministicDiscoverRecommendationProvider(
    DishHttpClient dishClient,
    PlaceHttpClient placeClient,
    IOptions<BffRecommendationOptions> options,
    ILogger<DeterministicDiscoverRecommendationProvider> logger) : IDiscoverRecommendationProvider
{
    private readonly DishHttpClient _dishClient = dishClient ?? throw new ArgumentNullException(nameof(dishClient));
    private readonly PlaceHttpClient _placeClient = placeClient ?? throw new ArgumentNullException(nameof(placeClient));
    private readonly BffRecommendationOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<DeterministicDiscoverRecommendationProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<DiscoverRecommendationsResponse> RecommendAsync(DiscoverRecommendationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fallbackReasons = new List<string>();
        var recommendationStatus = _options.AiEnabled ? "provider-unconfigured-fallback" : "ai-disabled-fallback";
        fallbackReasons.Add(_options.AiEnabled
            ? "No paid AI recommendation provider is configured for beta; deterministic Discover ranking was used."
            : "AI recommendations are disabled by configuration; deterministic Discover ranking was used.");

        var dishSearch = await SearchDishesAsync(request, fallbackReasons, cancellationToken);
        var placeSearch = await SearchPlacesAsync(request, fallbackReasons, cancellationToken);

        var dishes = (dishSearch?.Items ?? [])
            .Where(dish => string.Equals(dish.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Select((dish, index) => dish.ToRecommendedDish(index + 1, BuildDishReason(request)))
            .ToArray();

        var places = (placeSearch?.Items ?? [])
            .Where(place => string.Equals(place.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Select((place, index) => place.ToRecommendedPlace(index + 1, BuildPlaceReason(request)))
            .ToArray();

        _logger.LogInformation(
            "Discover recommendations returned deterministic fallback. Status: {RecommendationStatus}. Dishes: {DishCount}. Places: {PlaceCount}. Reasons: {FallbackReasonCount}.",
            recommendationStatus,
            dishes.Length,
            places.Length,
            fallbackReasons.Count);

        return new DiscoverRecommendationsResponse(
            Dishes: dishes,
            Places: places,
            RecommendationStatus: recommendationStatus,
            FallbackUsed: true,
            FallbackReasons: fallbackReasons,
            Explanation: "Discover recommendations are dish-primary and place-secondary. Deterministic catalog retrieval keeps the user flow available without a paid AI dependency.",
            Limit: request.Limit);
    }

    private async Task<DishSearchResponse?> SearchDishesAsync(
        DiscoverRecommendationRequest request,
        ICollection<string> fallbackReasons,
        CancellationToken cancellationToken)
    {
        var targetPathAndQuery = BuildDishSearchPath(request);

        try
        {
            var response = await _dishClient.SearchAsync(targetPathAndQuery, cancellationToken);
            if (response is null)
            {
                fallbackReasons.Add("Dish catalog retrieval returned no usable response; dish recommendations were degraded instead of returning an error.");
            }

            return response;
        }
        catch (Exception exception) when (IsDownstreamFallbackException(exception, cancellationToken))
        {
            _logger.LogWarning(exception, "Dish recommendation fallback used after Dish service failure. TargetPathAndQuery: {TargetPathAndQuery}.", targetPathAndQuery);
            fallbackReasons.Add("Dish catalog retrieval failed; dish recommendations were degraded instead of returning an error.");
            return null;
        }
    }

    private async Task<PlaceSearchResponse?> SearchPlacesAsync(
        DiscoverRecommendationRequest request,
        ICollection<string> fallbackReasons,
        CancellationToken cancellationToken)
    {
        var targetPathAndQuery = BuildPlaceSearchPath(request);

        try
        {
            var response = await _placeClient.SearchAsync(targetPathAndQuery, cancellationToken);
            if (response is null)
            {
                fallbackReasons.Add("Place catalog retrieval returned no usable response; place recommendations were degraded instead of returning an error.");
            }

            return response;
        }
        catch (Exception exception) when (IsDownstreamFallbackException(exception, cancellationToken))
        {
            _logger.LogWarning(exception, "Place recommendation fallback used after Place service failure. TargetPathAndQuery: {TargetPathAndQuery}.", targetPathAndQuery);
            fallbackReasons.Add("Place catalog retrieval failed; place recommendations were degraded instead of returning an error.");
            return null;
        }
    }

    private static bool IsDownstreamFallbackException(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException
        || exception is InvalidOperationException
        || exception is System.Text.Json.JsonException
        || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested;

    private static string BuildDishSearchPath(DiscoverRecommendationRequest request)
    {
        var queryParameters = new List<string> { $"limit={request.Limit}" };
        AddQueryParameter(queryParameters, "query", request.Query);
        return $"/api/dishes/search?{string.Join('&', queryParameters)}";
    }

    private static string BuildPlaceSearchPath(DiscoverRecommendationRequest request)
    {
        var queryParameters = new List<string> { $"limit={request.Limit}" };
        AddQueryParameter(queryParameters, "query", request.Query);
        AddQueryParameter(queryParameters, "city", request.Location);
        return $"/api/places/search?{string.Join('&', queryParameters)}";
    }

    private static void AddQueryParameter(ICollection<string> queryParameters, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        queryParameters.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value.Trim())}");
    }

    private static string BuildDishReason(DiscoverRecommendationRequest request) =>
        string.IsNullOrWhiteSpace(request.Query)
            ? "Dish-first deterministic Discover fallback ranking."
            : $"Dish-first deterministic match for '{request.Query.Trim()}'.";

    private static string BuildPlaceReason(DiscoverRecommendationRequest request) =>
        string.IsNullOrWhiteSpace(request.Location)
            ? "Place-secondary deterministic Discover fallback ranking."
            : $"Place-secondary deterministic match near {request.Location.Trim()}.";
}
