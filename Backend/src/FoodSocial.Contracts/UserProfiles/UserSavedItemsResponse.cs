namespace FoodSocial.Contracts.UserProfiles;

/// <summary>
/// Represents the owner-scoped saved-items view for a profile.
/// </summary>
/// <param name="Posts">The saved posts.</param>
/// <param name="Dishes">The saved dishes.</param>
/// <param name="Places">The saved places.</param>
public sealed record UserSavedItemsResponse(
    IReadOnlyList<SavedPostReferenceDto> Posts,
    IReadOnlyList<SavedDishReferenceDto> Dishes,
    IReadOnlyList<SavedPlaceReferenceDto> Places);
