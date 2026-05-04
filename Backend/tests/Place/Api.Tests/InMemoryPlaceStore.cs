using FoodSocial.Place.Application;
using FoodSocial.Place.Domain;

namespace FoodSocial.Place.Api.Tests;

internal sealed class InMemoryPlaceStore : IPlaceStore
{
    private readonly List<PlaceRecord> _places = [];

    internal int Count => _places.Count;

    public Task<PlaceCreationResult> CreatePlaceAsync(PlaceRecord place, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(place);

        var existing = _places.SingleOrDefault(candidate =>
            candidate.NormalizedName == place.NormalizedName
            && candidate.NormalizedCity == place.NormalizedCity
            && candidate.NormalizedArea == place.NormalizedArea);
        if (existing is not null)
        {
            return Task.FromResult(new PlaceCreationResult(existing, Created: false));
        }

        _places.Add(place);
        return Task.FromResult(new PlaceCreationResult(place, Created: true));
    }

    public Task<PlaceRecord?> GetPlaceAsync(Guid placeId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(placeId, Guid.Empty);
        return Task.FromResult(_places.SingleOrDefault(place => place.Id == placeId));
    }

    public Task<IReadOnlyList<PlaceRecord>> SearchPlacesAsync(PlaceSearchCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        IEnumerable<PlaceRecord> query = _places;
        if (!criteria.IncludeInactive)
        {
            query = query.Where(place => place.Status == PlaceLifecycleStatus.Active);
        }

        if (criteria.NormalizedQuery is not null)
        {
            query = query.Where(place =>
                place.NormalizedName.Contains(criteria.NormalizedQuery, StringComparison.Ordinal)
                || place.NormalizedAddressText.Contains(criteria.NormalizedQuery, StringComparison.Ordinal)
                || place.NormalizedCity.Contains(criteria.NormalizedQuery, StringComparison.Ordinal)
                || place.NormalizedArea.Contains(criteria.NormalizedQuery, StringComparison.Ordinal));
        }

        if (criteria.NormalizedCity is not null)
        {
            query = query.Where(place => place.NormalizedCity == criteria.NormalizedCity);
        }

        if (criteria.NormalizedArea is not null)
        {
            query = query.Where(place => place.NormalizedArea == criteria.NormalizedArea);
        }

        if (criteria.CuisineType is not null)
        {
            query = query.Where(place => place.CuisineTypes.Contains(criteria.CuisineType, StringComparer.Ordinal));
        }

        if (criteria.PriceRange is not null)
        {
            query = query.Where(place => string.Equals(place.PriceRange, criteria.PriceRange, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<PlaceRecord>>(query.OrderBy(place => place.Name).ThenBy(place => place.Id).Take(criteria.Limit).ToArray());
    }

    public Task<PlaceRecord?> UpdatePlaceStatusAsync(Guid placeId, PlaceLifecycleStatus status, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(placeId, Guid.Empty);

        var place = _places.SingleOrDefault(candidate => candidate.Id == placeId);
        place?.ChangeStatus(status);
        return Task.FromResult(place);
    }
}
