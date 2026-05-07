using FoodSocial.Contracts.Dishes;
using FoodSocial.Contracts.Places;

namespace FoodSocial.Bff;

internal sealed record DiscoverRecommendationRequest(string? Query, string? Location, int Limit);

internal sealed record DiscoverRecommendationsResponse(
    IReadOnlyList<RecommendedDishResponse> Dishes,
    IReadOnlyList<RecommendedPlaceResponse> Places,
    string RecommendationStatus,
    bool FallbackUsed,
    IReadOnlyList<string> FallbackReasons,
    string Explanation,
    int Limit);

internal sealed record RecommendedDishResponse(
    Guid Id,
    string Name,
    string? Category,
    string? CuisineType,
    IReadOnlyList<string> Aliases,
    string Status,
    int Rank,
    string Reason);

internal sealed record RecommendedPlaceResponse(
    Guid Id,
    string Name,
    string AddressText,
    string Area,
    string City,
    string Country,
    IReadOnlyList<string> CuisineTypes,
    string? PriceRange,
    string Status,
    int Rank,
    string Reason);

internal static class BffRecommendationModelMapping
{
    public static RecommendedDishResponse ToRecommendedDish(this DishSummaryResponse dish, int rank, string reason) =>
        new(dish.Id, dish.Name, dish.Category, dish.CuisineType, dish.Aliases, dish.Status, rank, reason);

    public static RecommendedPlaceResponse ToRecommendedPlace(this PlaceSummaryResponse place, int rank, string reason) =>
        new(place.Id, place.Name, place.AddressText, place.Area, place.City, place.Country, place.CuisineTypes, place.PriceRange, place.Status, rank, reason);
}
