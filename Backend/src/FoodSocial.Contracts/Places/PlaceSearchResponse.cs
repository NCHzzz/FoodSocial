namespace FoodSocial.Contracts.Places;

/// <summary>
/// Response contract for bounded Place search results.
/// </summary>
public sealed record PlaceSearchResponse(
    IReadOnlyList<PlaceSummaryResponse> Items,
    int Limit);
