namespace FoodSocial.Dish.Domain;

/// <summary>
/// Represents the lifecycle state of a canonical Dish record.
/// </summary>
public enum DishLifecycleStatus
{
    /// <summary>
    /// The Dish is active and returned in default search results.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The Dish is deprecated but retained for historical references.
    /// </summary>
    Deprecated = 1
}
