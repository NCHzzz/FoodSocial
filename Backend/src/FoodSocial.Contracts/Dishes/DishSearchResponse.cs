namespace FoodSocial.Contracts.Dishes;

/// <summary>
/// Response contract for bounded Dish search results.
/// </summary>
public sealed record DishSearchResponse(
    IReadOnlyList<DishSummaryResponse> Items,
    int Limit);
