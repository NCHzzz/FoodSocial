using FoodSocial.Dish.Application;
using FoodSocial.Dish.Domain;

namespace FoodSocial.Dish.Api.Tests;

internal sealed class InMemoryDishStore : IDishStore
{
    private readonly List<DishRecord> _dishes = [];

    public int Count => _dishes.Count;

    public Task<DishCreationResult> CreateDishAsync(DishRecord dish, CancellationToken cancellationToken)
    {
        var existing = _dishes.SingleOrDefault(candidate => candidate.NormalizedName == dish.NormalizedName);
        if (existing is not null)
        {
            return Task.FromResult(new DishCreationResult(existing, Created: false));
        }

        _dishes.Add(dish);
        return Task.FromResult(new DishCreationResult(dish, Created: true));
    }

    public Task<DishRecord?> GetDishAsync(Guid dishId, CancellationToken cancellationToken) =>
        Task.FromResult(_dishes.SingleOrDefault(candidate => candidate.Id == dishId));

    public Task<IReadOnlyList<DishRecord>> SearchDishesAsync(DishSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _dishes.AsEnumerable();
        if (!criteria.IncludeDeprecated)
        {
            query = query.Where(dish => dish.Status == DishLifecycleStatus.Active);
        }

        if (criteria.NormalizedQuery is not null)
        {
            query = query.Where(dish =>
                dish.NormalizedName.Contains(criteria.NormalizedQuery, StringComparison.Ordinal)
                || dish.Aliases.Any(alias => alias.Contains(criteria.NormalizedQuery, StringComparison.Ordinal)));
        }

        if (criteria.Category is not null)
        {
            query = query.Where(dish => string.Equals(dish.Category, criteria.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.CuisineType is not null)
        {
            query = query.Where(dish => string.Equals(dish.CuisineType, criteria.CuisineType, StringComparison.OrdinalIgnoreCase));
        }

        IReadOnlyList<DishRecord> results = query
            .OrderBy(dish => dish.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dish => dish.Id)
            .Take(criteria.Limit)
            .ToArray();

        return Task.FromResult(results);
    }

    public Task<DishRecord?> DeprecateDishAsync(Guid dishId, CancellationToken cancellationToken)
    {
        var dish = _dishes.SingleOrDefault(candidate => candidate.Id == dishId);
        dish?.Deprecate();
        return Task.FromResult(dish);
    }
}
