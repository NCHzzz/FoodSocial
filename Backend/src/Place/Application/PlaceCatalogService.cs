using FoodSocial.Contracts.Places;
using FoodSocial.Place.Domain;

namespace FoodSocial.Place.Application;

/// <summary>
/// Handles canonical Place use cases.
/// </summary>
public sealed class PlaceCatalogService(IPlaceStore placeStore)
{
    private readonly IPlaceStore _placeStore = placeStore ?? throw new ArgumentNullException(nameof(placeStore));

    /// <summary>
    /// Creates a canonical Place or returns an existing duplicate-safe match.
    /// </summary>
    public async Task<PlaceMutationResult> CreatePlaceAsync(CreatePlaceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        PlaceRecord place;
        try
        {
            place = PlaceRecord.Create(
                request.Name,
                request.Description,
                request.AddressText,
                request.Area,
                request.City,
                request.Country,
                request.Latitude,
                request.Longitude,
                request.CuisineTypes,
                request.PriceRange);
        }
        catch (ArgumentException exception)
        {
            return new PlaceMutationResult(PlaceOperationStatus.Invalid, null, exception.Message);
        }

        var created = await _placeStore.CreatePlaceAsync(place, cancellationToken);
        return new PlaceMutationResult(
            created.Created ? PlaceOperationStatus.Created : PlaceOperationStatus.AlreadyExists,
            MapDetail(created.Place),
            null);
    }

    /// <summary>
    /// Gets a Place by identifier.
    /// </summary>
    public async Task<PlaceDetailResponse?> GetPlaceAsync(Guid placeId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(placeId, Guid.Empty);

        var place = await _placeStore.GetPlaceAsync(placeId, cancellationToken);
        return place is null ? null : MapDetail(place);
    }

    /// <summary>
    /// Searches canonical Places with bounded deterministic results.
    /// </summary>
    public async Task<PlaceSearchResponse> SearchPlacesAsync(PlaceSearchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var criteria = PlaceSearchCriteria.FromRequest(request);
        var results = await _placeStore.SearchPlacesAsync(criteria, cancellationToken);
        return new PlaceSearchResponse(results.Select(MapSummary).ToArray(), criteria.Limit);
    }

    /// <summary>
    /// Updates a Place lifecycle status while retaining it for direct lookup.
    /// </summary>
    public async Task<PlaceMutationResult> UpdatePlaceStatusAsync(Guid placeId, string? status, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(placeId, Guid.Empty);

        if (!Enum.TryParse<PlaceLifecycleStatus>(status, ignoreCase: true, out var lifecycleStatus))
        {
            return new PlaceMutationResult(PlaceOperationStatus.Invalid, null, "Place status must be Active, Closed, or Suspended.");
        }

        var place = await _placeStore.UpdatePlaceStatusAsync(placeId, lifecycleStatus, cancellationToken);
        return place is null
            ? new PlaceMutationResult(PlaceOperationStatus.NotFound, null, "The requested place was not found.")
            : new PlaceMutationResult(PlaceOperationStatus.StatusUpdated, MapDetail(place), null);
    }

    private static PlaceDetailResponse MapDetail(PlaceRecord place) =>
        new(
            place.Id,
            place.Name,
            place.NormalizedName,
            place.Description,
            place.AddressText,
            place.Area,
            place.City,
            place.Country,
            place.Latitude,
            place.Longitude,
            place.CuisineTypes,
            place.PriceRange,
            place.Status.ToString(),
            place.CreatedAtUtc,
            place.UpdatedAtUtc);

    private static PlaceSummaryResponse MapSummary(PlaceRecord place) =>
        new(
            place.Id,
            place.Name,
            place.AddressText,
            place.Area,
            place.City,
            place.Country,
            place.CuisineTypes,
            place.PriceRange,
            place.Status.ToString());
}
