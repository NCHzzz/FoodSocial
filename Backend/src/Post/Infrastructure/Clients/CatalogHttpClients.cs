using System.Net.Http.Json;
using FoodSocial.Contracts.Dishes;
using FoodSocial.Contracts.Places;
using FoodSocial.Post.Application.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FoodSocial.Post.Infrastructure.Clients;

internal sealed class HttpPostCatalogReferenceValidator(
    DishCatalogHttpClient dishClient,
    PlaceCatalogHttpClient placeClient,
    ILogger<HttpPostCatalogReferenceValidator> logger) : IPostCatalogReferenceValidator
{
    private readonly DishCatalogHttpClient _dishClient = dishClient ?? throw new ArgumentNullException(nameof(dishClient));
    private readonly PlaceCatalogHttpClient _placeClient = placeClient ?? throw new ArgumentNullException(nameof(placeClient));
    private readonly ILogger<HttpPostCatalogReferenceValidator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<PostCatalogReferenceValidationResult> ValidateReferencesAsync(Guid dishId, Guid? placeId, CancellationToken cancellationToken)
    {
        if (dishId == Guid.Empty)
        {
            return PostCatalogReferenceValidationResult.Invalid("Dish id is required.");
        }

        var dish = await _dishClient.GetDishAsync(dishId, cancellationToken);
        if (dish is null)
        {
            return PostCatalogReferenceValidationResult.Invalid("Dish must reference an existing canonical dish.");
        }

        if (!string.Equals(dish.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return PostCatalogReferenceValidationResult.Invalid("Dish must reference an active canonical dish.");
        }

        if (placeId is null || placeId == Guid.Empty)
        {
            return PostCatalogReferenceValidationResult.Valid;
        }

        var place = await _placeClient.GetPlaceAsync(placeId.Value, cancellationToken);
        if (place is null)
        {
            return PostCatalogReferenceValidationResult.Invalid("Place must reference an existing canonical place when provided.");
        }

        if (!string.Equals(place.Status, PlaceWireConstants.StatusActive, StringComparison.OrdinalIgnoreCase))
        {
            return PostCatalogReferenceValidationResult.Invalid("Place must reference an active canonical place when provided.");
        }

        return PostCatalogReferenceValidationResult.Valid;
    }
}

internal sealed class DishCatalogHttpClient(HttpClient httpClient, ILogger<DishCatalogHttpClient> logger)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<DishCatalogHttpClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<DishDetailResponse?> GetDishAsync(Guid dishId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"/api/dishes/{dishId}", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DishDetailResponse>(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Dish lookup failed for {DishId}.", dishId);
            return null;
        }
    }
}

internal sealed class PlaceCatalogHttpClient(HttpClient httpClient, ILogger<PlaceCatalogHttpClient> logger)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<PlaceCatalogHttpClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<PlaceDetailResponse?> GetPlaceAsync(Guid placeId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"/api/places/{placeId}", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PlaceDetailResponse>(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Place lookup failed for {PlaceId}.", placeId);
            return null;
        }
    }
}

internal static class CatalogHttpClientConfiguration
{
    internal static Uri ResolveDishApiBaseAddress(IConfiguration configuration)
    {
        var discoveredAddress = configuration["Services:dish-api:https:0"]
            ?? configuration["Services:dish-api:http:0"]
            ?? throw new InvalidOperationException("Dish API base address is not configured. Set an Aspire service reference for 'dish-api'.");

        return new Uri(discoveredAddress.TrimEnd('/'), UriKind.Absolute);
    }

    internal static Uri ResolvePlaceApiBaseAddress(IConfiguration configuration)
    {
        var discoveredAddress = configuration["Services:place-api:https:0"]
            ?? configuration["Services:place-api:http:0"]
            ?? throw new InvalidOperationException("Place API base address is not configured. Set an Aspire service reference for 'place-api'.");

        return new Uri(discoveredAddress.TrimEnd('/'), UriKind.Absolute);
    }
}
