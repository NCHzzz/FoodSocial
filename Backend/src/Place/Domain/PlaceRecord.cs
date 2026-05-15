using System.Globalization;
using System.Text;

namespace FoodSocial.Place.Domain;

/// <summary>
/// Represents a canonical Place owned by the Place service boundary.
/// </summary>
public sealed class PlaceRecord
{
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private const decimal MinimumLatitude = -90m;
    private const decimal MaximumLatitude = 90m;
    private const decimal MinimumLongitude = -180m;
    private const decimal MaximumLongitude = 180m;

    private PlaceRecord()
    {
    }

    private PlaceRecord(
        string name,
        string normalizedName,
        string? description,
        string addressText,
        string normalizedAddressText,
        string area,
        string normalizedArea,
        string city,
        string normalizedCity,
        string country,
        string normalizedCountry,
        decimal? latitude,
        decimal? longitude,
        IEnumerable<string>? cuisineTypes,
        string? priceRange)
    {
        Id = Guid.NewGuid();
        Name = name;
        NormalizedName = normalizedName;
        Description = description;
        AddressText = addressText;
        NormalizedAddressText = normalizedAddressText;
        Area = area;
        NormalizedArea = normalizedArea;
        City = city;
        NormalizedCity = normalizedCity;
        Country = country;
        NormalizedCountry = normalizedCountry;
        Latitude = latitude;
        Longitude = longitude;
        CuisineTypes = NormalizeDistinctValues(cuisineTypes);
        CuisineTypesSearchText = BuildMultiValueSearchText(CuisineTypes);
        PriceRange = NormalizeOptional(priceRange);
        Status = PlaceLifecycleStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    /// <summary>Gets the stable Place identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the tenant isolation identifier.</summary>
    public Guid TenantId { get; private set; } = DefaultTenantId;

    /// <summary>Gets the display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the normalized name used for search and duplicate detection.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets the optional Place description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the display address text.</summary>
    public string AddressText { get; private set; } = string.Empty;

    /// <summary>Gets the normalized address text used for search.</summary>
    public string NormalizedAddressText { get; private set; } = string.Empty;

    /// <summary>Gets the display area or district.</summary>
    public string Area { get; private set; } = string.Empty;

    /// <summary>Gets the normalized area used for search and duplicate detection.</summary>
    public string NormalizedArea { get; private set; } = string.Empty;

    /// <summary>Gets the display city.</summary>
    public string City { get; private set; } = string.Empty;

    /// <summary>Gets the normalized city used for search and duplicate detection.</summary>
    public string NormalizedCity { get; private set; } = string.Empty;

    /// <summary>Gets the display country.</summary>
    public string Country { get; private set; } = string.Empty;

    /// <summary>Gets the normalized country used for search.</summary>
    public string NormalizedCountry { get; private set; } = string.Empty;

    /// <summary>Gets the optional latitude.</summary>
    public decimal? Latitude { get; private set; }

    /// <summary>Gets the optional longitude.</summary>
    public decimal? Longitude { get; private set; }

    /// <summary>Gets normalized cuisine type labels.</summary>
    public List<string> CuisineTypes { get; private set; } = [];

    /// <summary>Gets the normalized cuisine search text used by persistence queries.</summary>
    public string CuisineTypesSearchText { get; private set; } = string.Empty;

    /// <summary>Gets the optional price range.</summary>
    public string? PriceRange { get; private set; }

    /// <summary>Gets the lifecycle status.</summary>
    public PlaceLifecycleStatus Status { get; private set; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Gets the UTC update timestamp.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a canonical active Place after validation and normalization.
    /// </summary>
    public static PlaceRecord Create(
        string? name,
        string? description,
        string? addressText,
        string? area,
        string? city,
        string? country,
        decimal? latitude,
        decimal? longitude,
        IEnumerable<string>? cuisineTypes,
        string? priceRange)
    {
        var normalizedDisplayName = NormalizeOptional(name);
        if (normalizedDisplayName is null)
        {
            throw new ArgumentException("Place name is required.", nameof(name));
        }

        var normalizedAddress = NormalizeOptional(addressText);
        if (normalizedAddress is null)
        {
            throw new ArgumentException("Place address is required.", nameof(addressText));
        }

        var normalizedArea = NormalizeOptional(area);
        if (normalizedArea is null)
        {
            throw new ArgumentException("Place area is required.", nameof(area));
        }

        var normalizedCity = NormalizeOptional(city);
        if (normalizedCity is null)
        {
            throw new ArgumentException("Place city is required.", nameof(city));
        }

        var normalizedCountry = NormalizeOptional(country);
        if (normalizedCountry is null)
        {
            throw new ArgumentException("Place country is required.", nameof(country));
        }

        if (latitude is < MinimumLatitude or > MaximumLatitude)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Place latitude must be between -90 and 90.");
        }

        if (longitude is < MinimumLongitude or > MaximumLongitude)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Place longitude must be between -180 and 180.");
        }

        return new PlaceRecord(
            normalizedDisplayName,
            NormalizeSearchValue(normalizedDisplayName),
            NormalizeOptional(description),
            normalizedAddress,
            NormalizeSearchValue(normalizedAddress),
            normalizedArea,
            NormalizeSearchValue(normalizedArea),
            normalizedCity,
            NormalizeSearchValue(normalizedCity),
            normalizedCountry,
            NormalizeSearchValue(normalizedCountry),
            latitude,
            longitude,
            cuisineTypes,
            priceRange);
    }

    /// <summary>
    /// Changes the Place lifecycle status without deleting the canonical record.
    /// </summary>
    public void ChangeStatus(PlaceLifecycleStatus status)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
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

    private static List<string> NormalizeDistinctValues(IEnumerable<string>? values)
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

            normalized = NormalizeSearchValue(normalized);
            if (!result.Contains(normalized, StringComparer.Ordinal))
            {
                result.Add(normalized);
            }
        }

        return result;
    }

    private static string BuildMultiValueSearchText(IReadOnlyCollection<string> values) =>
        values.Count == 0 ? string.Empty : $"|{string.Join('|', values)}|";
}
