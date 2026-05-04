namespace FoodSocial.Contracts.Dishes;

/// <summary>
/// Detailed response contract for a canonical Dish record.
/// </summary>
public sealed record DishDetailResponse(
    Guid Id,
    string Name,
    string NormalizedName,
    string? Description,
    string? Category,
    string? CuisineType,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> Ingredients,
    IReadOnlyList<string> DietaryFlags,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DeprecatedAtUtc);
