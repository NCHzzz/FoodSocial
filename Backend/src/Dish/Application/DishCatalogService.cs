using FoodSocial.Contracts.Dishes;
using FoodSocial.Dish.Domain;

namespace FoodSocial.Dish.Application;

/// <summary>
/// Handles canonical Dish use cases.
/// </summary>
public sealed class DishCatalogService(IDishStore dishStore)
{
    private readonly IDishStore _dishStore = dishStore ?? throw new ArgumentNullException(nameof(dishStore));

    /// <summary>
    /// Creates a canonical Dish or returns an existing duplicate-safe match.
    /// </summary>
    public async Task<DishMutationResult> CreateDishAsync(CreateDishRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        DishRecord dish;
        try
        {
            dish = DishRecord.Create(
                request.Name,
                request.Description,
                request.Category,
                request.CuisineType,
                request.Aliases,
                request.Ingredients,
                request.DietaryFlags);
        }
        catch (ArgumentException exception)
        {
            return new DishMutationResult(DishOperationStatus.Invalid, null, exception.Message);
        }

        var created = await _dishStore.CreateDishAsync(dish, cancellationToken);
        return new DishMutationResult(
            created.Created ? DishOperationStatus.Created : DishOperationStatus.AlreadyExists,
            MapDetail(created.Dish),
            null);
    }

    /// <summary>
    /// Gets a Dish by identifier.
    /// </summary>
    public async Task<DishDetailResponse?> GetDishAsync(Guid dishId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(dishId, Guid.Empty);

        var dish = await _dishStore.GetDishAsync(dishId, cancellationToken);
        return dish is null ? null : MapDetail(dish);
    }

    /// <summary>
    /// Searches canonical Dishes with bounded deterministic results.
    /// </summary>
    public async Task<DishSearchResponse> SearchDishesAsync(DishSearchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var criteria = DishSearchCriteria.FromRequest(request);
        var results = await _dishStore.SearchDishesAsync(criteria, cancellationToken);
        return new DishSearchResponse(results.Select(MapSummary).ToArray(), criteria.Limit);
    }

    /// <summary>
    /// Deprecates a Dish while retaining it for direct lookup.
    /// </summary>
    public async Task<DishMutationResult> DeprecateDishAsync(Guid dishId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(dishId, Guid.Empty);

        var dish = await _dishStore.DeprecateDishAsync(dishId, cancellationToken);
        return dish is null
            ? new DishMutationResult(DishOperationStatus.NotFound, null, "The requested dish was not found.")
            : new DishMutationResult(DishOperationStatus.Deprecated, MapDetail(dish), null);
    }

    private static DishDetailResponse MapDetail(DishRecord dish) =>
        new(
            dish.Id,
            dish.Name,
            dish.NormalizedName,
            dish.Description,
            dish.Category,
            dish.CuisineType,
            dish.Aliases,
            dish.Ingredients,
            dish.DietaryFlags,
            dish.Status.ToString(),
            dish.CreatedAtUtc,
            dish.UpdatedAtUtc,
            dish.DeprecatedAtUtc);

    private static DishSummaryResponse MapSummary(DishRecord dish) =>
        new(
            dish.Id,
            dish.Name,
            dish.Category,
            dish.CuisineType,
            dish.Aliases,
            dish.Status.ToString());
}
