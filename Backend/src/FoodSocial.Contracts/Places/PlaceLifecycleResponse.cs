namespace FoodSocial.Contracts.Places;

/// <summary>
/// Response contract for Place lifecycle mutations.
/// </summary>
public sealed record PlaceLifecycleResponse(
    Guid Id,
    string Status,
    DateTimeOffset UpdatedAtUtc);
