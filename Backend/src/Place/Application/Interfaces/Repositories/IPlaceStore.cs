using FoodSocial.Place.Domain;

namespace FoodSocial.Place.Application.Interfaces.Repositories;

/// <summary>
/// Defines persistence operations for the canonical Place catalog.
/// </summary>
public interface IPlaceStore
{
    /// <summary>
    /// Creates a Place or returns the existing Place with the same normalized duplicate scope.
    /// </summary>
    Task<PlaceCreationResult> CreatePlaceAsync(PlaceRecord place, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a Place by identifier.
    /// </summary>
    Task<PlaceRecord?> GetPlaceAsync(Guid placeId, CancellationToken cancellationToken);

    /// <summary>
    /// Searches Places using bounded criteria.
    /// </summary>
    Task<IReadOnlyList<PlaceRecord>> SearchPlacesAsync(PlaceSearchCriteria criteria, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a Place lifecycle status by identifier.
    /// </summary>
    Task<PlaceRecord?> UpdatePlaceStatusAsync(Guid placeId, PlaceLifecycleStatus status, CancellationToken cancellationToken);
}
