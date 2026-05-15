using System.Globalization;
using System.Text;

namespace FoodSocial.Dish.Domain;

/// <summary>
/// Represents a canonical Dish owned by the Dish service boundary.
/// </summary>
public sealed class DishRecord
{
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private DishRecord()
    {
    }

    private DishRecord(
        string name,
        string normalizedName,
        string? description,
        string? category,
        string? cuisineType,
        IEnumerable<string>? aliases,
        IEnumerable<string>? ingredients,
        IEnumerable<string>? dietaryFlags)
    {
        Id = Guid.NewGuid();
        Name = name;
        NormalizedName = normalizedName;
        Description = description;
        Category = category;
        CuisineType = cuisineType;
        Aliases = NormalizeDistinctValues(aliases, normalizeForSearch: true);
        AliasesSearchText = DishSearchText.BuildAliasesSearchText(Aliases);
        Ingredients = NormalizeDistinctValues(ingredients, normalizeForSearch: false);
        DietaryFlags = NormalizeDistinctValues(dietaryFlags, normalizeForSearch: false);
        Status = DishLifecycleStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    /// <summary>
    /// Gets the stable Dish identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the tenant isolation identifier.
    /// </summary>
    public Guid TenantId { get; private set; } = DefaultTenantId;

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the normalized name used for duplicate detection and search.
    /// </summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional dish description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the optional dish category.
    /// </summary>
    public string? Category { get; private set; }

    /// <summary>
    /// Gets the optional cuisine type.
    /// </summary>
    public string? CuisineType { get; private set; }

    /// <summary>
    /// Gets normalized alternate names.
    /// </summary>
    public List<string> Aliases { get; private set; } = [];

    /// <summary>
    /// Gets persisted alias search text derived from normalized aliases.
    /// </summary>
    public string AliasesSearchText { get; private set; } = string.Empty;

    /// <summary>
    /// Gets normalized ingredient labels.
    /// </summary>
    public List<string> Ingredients { get; private set; } = [];

    /// <summary>
    /// Gets normalized dietary flags.
    /// </summary>
    public List<string> DietaryFlags { get; private set; } = [];

    /// <summary>
    /// Gets the lifecycle status.
    /// </summary>
    public DishLifecycleStatus Status { get; private set; }

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC deprecation timestamp when deprecated.
    /// </summary>
    public DateTimeOffset? DeprecatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a canonical active Dish after validation and normalization.
    /// </summary>
    public static DishRecord Create(
        string? name,
        string? description,
        string? category,
        string? cuisineType,
        IEnumerable<string>? aliases,
        IEnumerable<string>? ingredients,
        IEnumerable<string>? dietaryFlags)
    {
        var normalizedDisplayName = NormalizeOptional(name);
        if (normalizedDisplayName is null)
        {
            throw new ArgumentException("Dish name is required.", nameof(name));
        }

        return new DishRecord(
            normalizedDisplayName,
            NormalizeSearchValue(normalizedDisplayName),
            NormalizeOptional(description),
            NormalizeOptional(category),
            NormalizeOptional(cuisineType),
            aliases,
            ingredients,
            dietaryFlags);
    }

    /// <summary>
    /// Marks the Dish as deprecated while retaining the canonical record.
    /// </summary>
    public void Deprecate()
    {
        if (Status == DishLifecycleStatus.Deprecated)
        {
            return;
        }

        Status = DishLifecycleStatus.Deprecated;
        DeprecatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DeprecatedAtUtc.Value;
    }

    /// <summary>
    /// Normalizes free-text values for deterministic search and duplicate detection.
    /// </summary>
    public static string NormalizeSearchValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var trimmed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(trimmed.Length);
        var previousWasWhitespace = false;

        foreach (var character in trimmed)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            builder.Append(character switch
            {
                'Đ' or 'đ' => 'd',
                _ => char.ToLowerInvariant(character)
            });
            previousWasWhitespace = false;
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<string> NormalizeDistinctValues(IEnumerable<string>? values, bool normalizeForSearch)
    {
        var result = new List<string>();
        if (values is null)
        {
            return result;
        }

        foreach (var value in values)
        {
            var normalized = NormalizeOptional(value);
            if (normalized is null)
            {
                continue;
            }

            normalized = normalizeForSearch ? NormalizeSearchValue(normalized) : normalized;
            if (!result.Contains(normalized, StringComparer.Ordinal))
            {
                result.Add(normalized);
            }
        }

        return result;
    }
}
