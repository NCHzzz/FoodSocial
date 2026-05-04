namespace FoodSocial.Contracts.Dishes;

/// <summary>
/// Query contract for bounded Dish search.
/// </summary>
public sealed record DishSearchRequest(
    string? Query,
    string? Category,
    string? CuisineType,
    bool IncludeDeprecated,
    int? Limit);
