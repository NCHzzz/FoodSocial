namespace FoodSocial.Contracts.Dishes;

/// <summary>
/// Request contract for creating a canonical Dish record.
/// </summary>
/// <param name="Name">The dish display name.</param>
/// <param name="Description">The optional dish description.</param>
/// <param name="Category">The optional dish category.</param>
/// <param name="CuisineType">The optional cuisine type.</param>
/// <param name="Aliases">The optional alternate names.</param>
/// <param name="Ingredients">The optional ingredient names.</param>
/// <param name="DietaryFlags">The optional dietary flags.</param>
public sealed record CreateDishRequest(
    string? Name,
    string? Description,
    string? Category,
    string? CuisineType,
    IReadOnlyList<string>? Aliases,
    IReadOnlyList<string>? Ingredients,
    IReadOnlyList<string>? DietaryFlags);
