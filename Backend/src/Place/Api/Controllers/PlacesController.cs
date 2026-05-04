using FoodSocial.Contracts.Places;
using FoodSocial.Place.Application;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Place.Api.Controllers;

/// <summary>
/// Exposes canonical Place catalog endpoints.
/// </summary>
[ApiController]
[Route("api/places")]
public sealed class PlacesController(PlaceCatalogService placeCatalogService) : ControllerBase
{
    private readonly PlaceCatalogService _placeCatalogService = placeCatalogService ?? throw new ArgumentNullException(nameof(placeCatalogService));

    /// <summary>
    /// Gets a canonical Place by identifier.
    /// </summary>
    [HttpGet("{placeId:guid}")]
    [ProducesResponseType<PlaceDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaceDetailResponse>> GetPlace(Guid placeId, CancellationToken cancellationToken)
    {
        if (placeId == Guid.Empty)
        {
            return Problem(title: "Invalid place id", detail: "Place id must not be empty.", statusCode: StatusCodes.Status400BadRequest);
        }

        var place = await _placeCatalogService.GetPlaceAsync(placeId, cancellationToken);
        return place is null
            ? Problem(title: "Place not found", detail: "The requested place was not found.", statusCode: StatusCodes.Status404NotFound)
            : Ok(place);
    }

    /// <summary>
    /// Searches canonical Places with bounded results.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType<PlaceSearchResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlaceSearchResponse>> SearchPlaces(
        [FromQuery] string? query,
        [FromQuery] string? city,
        [FromQuery] string? area,
        [FromQuery] string? cuisineType,
        [FromQuery] string? priceRange,
        [FromQuery] bool includeInactive,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var results = await _placeCatalogService.SearchPlacesAsync(
            new PlaceSearchRequest(query, city, area, cuisineType, priceRange, includeInactive, limit),
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Creates a canonical Place.
    /// </summary>
    [HttpPost]
    [ServiceFilter(typeof(PlaceManagementAuthorizationFilter))]
    [ProducesResponseType<PlaceDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<PlaceDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlaceDetailResponse>> CreatePlace([FromBody] CreatePlaceRequest request, CancellationToken cancellationToken)
    {
        var result = await _placeCatalogService.CreatePlaceAsync(request, cancellationToken);
        return result.Status switch
        {
            PlaceOperationStatus.Created => CreatedAtAction(nameof(GetPlace), new { placeId = result.Place!.Id }, result.Place),
            PlaceOperationStatus.AlreadyExists => Ok(result.Place),
            PlaceOperationStatus.Invalid => Problem(title: "Invalid place request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest),
            _ => Problem(title: "Place creation failed", detail: result.Error ?? "The place could not be created.", statusCode: StatusCodes.Status400BadRequest)
        };
    }

    /// <summary>
    /// Updates a canonical Place lifecycle status without deleting it.
    /// </summary>
    [HttpPatch("{placeId:guid}/status")]
    [ServiceFilter(typeof(PlaceManagementAuthorizationFilter))]
    [ProducesResponseType<PlaceLifecycleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaceLifecycleResponse>> UpdatePlaceStatus(Guid placeId, [FromBody] UpdatePlaceStatusRequest request, CancellationToken cancellationToken)
    {
        if (placeId == Guid.Empty)
        {
            return Problem(title: "Invalid place id", detail: "Place id must not be empty.", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await _placeCatalogService.UpdatePlaceStatusAsync(placeId, request.Status, cancellationToken);
        return result.Status switch
        {
            PlaceOperationStatus.StatusUpdated => Ok(new PlaceLifecycleResponse(result.Place!.Id, result.Place.Status, result.Place.UpdatedAtUtc)),
            PlaceOperationStatus.NotFound => Problem(title: "Place not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            PlaceOperationStatus.Invalid => Problem(title: "Invalid place status", detail: result.Error, statusCode: StatusCodes.Status400BadRequest),
            _ => Problem(title: "Place status update failed", detail: result.Error ?? "The place status could not be updated.", statusCode: StatusCodes.Status400BadRequest)
        };
    }
}
