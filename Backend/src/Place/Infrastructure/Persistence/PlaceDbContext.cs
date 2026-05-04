using FoodSocial.Place.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FoodSocial.Place.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for the Place boundary.
/// </summary>
public sealed class PlaceDbContext(DbContextOptions<PlaceDbContext> options) : DbContext(options)
{
    private const char MultiValueSeparator = '|';
    private static readonly ValueConverter<List<string>, string> MultiValueListConverter = new(
        values => SerializeMultiValueList(values),
        value => DeserializeMultiValueList(value));

    private static readonly ValueComparer<List<string>> MultiValueListComparer = new(
        (left, right) => ListsAreEqual(left, right),
        values => GetListHashCode(values),
        values => values.ToList());

    /// <summary>
    /// Gets the persisted canonical Places.
    /// </summary>
    public DbSet<PlaceRecord> Places => Set<PlaceRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlaceRecord>(entity =>
        {
            entity.ToTable("places");

            entity.HasKey(place => place.Id);

            entity.Property(place => place.Name).HasMaxLength(180).IsRequired();
            entity.Property(place => place.NormalizedName).HasMaxLength(180).IsRequired();
            entity.Property(place => place.Description).HasMaxLength(1200);
            entity.Property(place => place.AddressText).HasMaxLength(300).IsRequired();
            entity.Property(place => place.NormalizedAddressText).HasMaxLength(300).IsRequired();
            entity.Property(place => place.Area).HasMaxLength(120).IsRequired();
            entity.Property(place => place.NormalizedArea).HasMaxLength(120).IsRequired();
            entity.Property(place => place.City).HasMaxLength(120).IsRequired();
            entity.Property(place => place.NormalizedCity).HasMaxLength(120).IsRequired();
            entity.Property(place => place.Country).HasMaxLength(120).IsRequired();
            entity.Property(place => place.NormalizedCountry).HasMaxLength(120).IsRequired();
            entity.Property(place => place.Latitude).HasPrecision(9, 6);
            entity.Property(place => place.Longitude).HasPrecision(9, 6);
            entity.Property(place => place.CuisineTypes)
                .HasConversion(MultiValueListConverter, MultiValueListComparer);
            entity.Property(place => place.CuisineTypesSearchText)
                .HasColumnType("text")
                .IsRequired();
            entity.Property(place => place.PriceRange).HasMaxLength(16);
            entity.Property(place => place.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(place => place.CreatedAtUtc).IsRequired();
            entity.Property(place => place.UpdatedAtUtc).IsRequired();

            entity.HasIndex(place => place.NormalizedName);
            entity.HasIndex(place => new { place.NormalizedName, place.NormalizedCity, place.NormalizedArea }).IsUnique();
            entity.HasIndex(place => new { place.NormalizedCity, place.NormalizedArea });
            entity.HasIndex(place => place.CuisineTypes);
            entity.HasIndex(place => place.CuisineTypesSearchText);
            entity.HasIndex(place => place.PriceRange);
            entity.HasIndex(place => place.Status);
        });
    }

    private static string SerializeMultiValueList(IReadOnlyCollection<string> values) =>
        values.Count == 0 ? string.Empty : string.Join(MultiValueSeparator, values);

    private static List<string> DeserializeMultiValueList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(MultiValueSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static bool ListsAreEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right) =>
        ReferenceEquals(left, right)
        || (left is not null
            && right is not null
            && left.SequenceEqual(right, StringComparer.Ordinal));

    private static int GetListHashCode(IReadOnlyList<string>? values)
    {
        if (values is null)
        {
            return 0;
        }

        var hash = new HashCode();
        foreach (var value in values)
        {
            hash.Add(value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
