namespace FoodSocial.Contracts.Dishes;

/// <summary>
/// Response contract for Dish lifecycle mutations.
/// </summary>
public sealed record DishLifecycleResponse(
    Guid Id,
    string Status,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DeprecatedAtUtc);
