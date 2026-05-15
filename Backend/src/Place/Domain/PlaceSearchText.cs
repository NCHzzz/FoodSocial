namespace FoodSocial.Place.Domain;

/// <summary>
/// Builds deterministic Place search text stored by persistence for indexed lookups.
/// </summary>
public static class PlaceSearchText
{
    /// <summary>
    /// Builds cuisine search text from normalized cuisine labels.
    /// </summary>
    public static string BuildCuisineTypesSearchText(IReadOnlyCollection<string> cuisineTypes) =>
        SearchText.BuildMultiValueTokenText(cuisineTypes);
}

internal static class SearchText
{
    internal static string BuildMultiValueTokenText(IReadOnlyCollection<string> values)
    {
        if (values.Count == 0)
        {
            return string.Empty;
        }

        return $"|{string.Join('|', values)}|";
    }
}
