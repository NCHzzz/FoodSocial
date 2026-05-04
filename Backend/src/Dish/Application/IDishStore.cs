using FoodSocial.Dish.Domain;

namespace FoodSocial.Dish.Application;

/// <summary>
/// Defines persistence operations for the canonical Dish catalog.
/// </summary>
public interface IDishStore
{
    /// <summary>
    /// Creates a Dish or returns the existing Dish with the same normalized name.
    /// </summary>
    Task<DishCreationResult> CreateDishAsync(DishRecord dish, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a Dish by identifier.
    /// </summary>
    Task<DishRecord?> GetDishAsync(Guid dishId, CancellationToken cancellationToken);

    /// <summary>
    /// Searches Dishes using bounded criteria.
    /// </summary>
    Task<IReadOnlyList<DishRecord>> SearchDishesAsync(DishSearchCriteria criteria, CancellationToken cancellationToken);

    /// <summary>
    /// Deprecates a Dish by identifier.
    /// </summary>
    Task<DishRecord?> DeprecateDishAsync(Guid dishId, CancellationToken cancellationToken);
}
