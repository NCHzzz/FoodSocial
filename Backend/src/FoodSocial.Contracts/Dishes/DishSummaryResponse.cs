namespace FoodSocial.Contracts.Dishes;

/// <summary>
/// Summary response contract for Dish lists and search results.
/// </summary>
public sealed record DishSummaryResponse(
    Guid Id,
    string Name,
    string? Category,
    string? CuisineType,
    IReadOnlyList<string> Aliases,
    string Status);
