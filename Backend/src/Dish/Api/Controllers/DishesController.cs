using FoodSocial.Contracts.Dishes;
using FoodSocial.Dish.Api;
using FoodSocial.Dish.Application;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Dish.Api.Controllers;

/// <summary>
/// Exposes canonical Dish catalog endpoints.
/// </summary>
[ApiController]
[Route("api/dishes")]
public sealed class DishesController(DishCatalogService dishCatalogService) : ControllerBase
{
    private readonly DishCatalogService _dishCatalogService = dishCatalogService ?? throw new ArgumentNullException(nameof(dishCatalogService));

    /// <summary>
    /// Gets a canonical Dish by identifier.
    /// </summary>
    [HttpGet("{dishId:guid}")]
    [ProducesResponseType<DishDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DishDetailResponse>> GetDish(Guid dishId, CancellationToken cancellationToken)
    {
        if (dishId == Guid.Empty)
        {
            return Problem(title: "Invalid dish id", detail: "Dish id must not be empty.", statusCode: StatusCodes.Status400BadRequest);
        }

        var dish = await _dishCatalogService.GetDishAsync(dishId, cancellationToken);
        return dish is null
            ? Problem(title: "Dish not found", detail: "The requested dish was not found.", statusCode: StatusCodes.Status404NotFound)
            : Ok(dish);
    }

    /// <summary>
    /// Searches canonical Dishes with bounded results.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType<DishSearchResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DishSearchResponse>> SearchDishes(
        [FromQuery] string? query,
        [FromQuery] string? category,
        [FromQuery] string? cuisineType,
        [FromQuery] bool includeDeprecated,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var results = await _dishCatalogService.SearchDishesAsync(
            new DishSearchRequest(query, category, cuisineType, includeDeprecated, limit),
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Creates a canonical Dish.
    /// </summary>
    [HttpPost]
    [ServiceFilter(typeof(DishManagementAuthorizationFilter))]
    [ProducesResponseType<DishDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<DishDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DishDetailResponse>> CreateDish([FromBody] CreateDishRequest request, CancellationToken cancellationToken)
    {
        var result = await _dishCatalogService.CreateDishAsync(request, cancellationToken);
        return result.Status switch
        {
            DishOperationStatus.Created => CreatedAtAction(nameof(GetDish), new { dishId = result.Dish!.Id }, result.Dish),
            DishOperationStatus.AlreadyExists => Ok(result.Dish),
            DishOperationStatus.Invalid => Problem(title: "Invalid dish request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest),
            _ => Problem(title: "Dish creation failed", detail: result.Error ?? "The dish could not be created.", statusCode: StatusCodes.Status400BadRequest)
        };
    }

    /// <summary>
    /// Deprecates a canonical Dish without deleting it.
    /// </summary>
    [HttpPatch("{dishId:guid}/deprecate")]
    [ServiceFilter(typeof(DishManagementAuthorizationFilter))]
    [ProducesResponseType<DishLifecycleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DishLifecycleResponse>> DeprecateDish(Guid dishId, CancellationToken cancellationToken)
    {
        if (dishId == Guid.Empty)
        {
            return Problem(title: "Invalid dish id", detail: "Dish id must not be empty.", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await _dishCatalogService.DeprecateDishAsync(dishId, cancellationToken);
        return result.Status switch
        {
            DishOperationStatus.Deprecated => Ok(new DishLifecycleResponse(result.Dish!.Id, result.Dish.Status, result.Dish.UpdatedAtUtc, result.Dish.DeprecatedAtUtc)),
            DishOperationStatus.NotFound => Problem(title: "Dish not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Dish deprecation failed", detail: result.Error ?? "The dish could not be deprecated.", statusCode: StatusCodes.Status400BadRequest)
        };
    }
}
