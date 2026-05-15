namespace FoodSocial.Post.Domain;

/// <summary>
/// Builds deterministic Post search text stored by persistence for indexed lookups.
/// </summary>
public static class PostSearchText
{
    /// <summary>
    /// Builds tag search text from normalized tag labels.
    /// </summary>
    public static string BuildTagsSearchText(IEnumerable<string> tags) =>
        string.Join(' ', tags);
}
