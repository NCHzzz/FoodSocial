namespace FoodSocial.Dish.Domain;

/// <summary>
/// Builds deterministic Dish search text stored by persistence for indexed lookups.
/// </summary>
public static class DishSearchText
{
    /// <summary>
    /// Builds the alias search text value from normalized alias labels.
    /// </summary>
    public static string BuildAliasesSearchText(IReadOnlyCollection<string> aliases) =>
        SearchText.BuildMultiValueTokenText(aliases);
}

internal static class SearchText
{
    internal static string BuildMultiValueTokenText(IReadOnlyCollection<string> values) =>
        values.Count == 0 ? string.Empty : $"|{string.Join('|', values)}|";
}
