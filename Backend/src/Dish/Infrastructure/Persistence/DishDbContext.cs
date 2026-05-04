using FoodSocial.Dish.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FoodSocial.Dish.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for the Dish boundary.
/// </summary>
public sealed class DishDbContext(DbContextOptions<DishDbContext> options) : DbContext(options)
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
    /// Gets the persisted canonical Dishes.
    /// </summary>
    public DbSet<DishRecord> Dishes => Set<DishRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DishRecord>(entity =>
        {
            entity.ToTable("dishes");

            entity.HasKey(dish => dish.Id);

            entity.Property(dish => dish.Name)
                .HasMaxLength(160)
                .IsRequired();
            entity.Property(dish => dish.NormalizedName)
                .HasMaxLength(160)
                .IsRequired();
            entity.Property(dish => dish.Description)
                .HasMaxLength(1000);
            entity.Property(dish => dish.Category)
                .HasMaxLength(100);
            entity.Property(dish => dish.CuisineType)
                .HasMaxLength(100);
            entity.Property(dish => dish.Aliases)
                .HasConversion(MultiValueListConverter, MultiValueListComparer);
            entity.Property(dish => dish.AliasesSearchText)
                .HasColumnName("aliases_search_text");
            entity.Property(dish => dish.Ingredients)
                .HasConversion(MultiValueListConverter, MultiValueListComparer);
            entity.Property(dish => dish.DietaryFlags)
                .HasConversion(MultiValueListConverter, MultiValueListComparer);
            entity.Property(dish => dish.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(dish => dish.CreatedAtUtc)
                .IsRequired();
            entity.Property(dish => dish.UpdatedAtUtc)
                .IsRequired();

            entity.HasIndex(dish => dish.NormalizedName)
                .IsUnique();
            entity.HasIndex(dish => dish.Category);
            entity.HasIndex(dish => dish.CuisineType);
            entity.HasIndex(dish => dish.Status);
        });
    }

    private static string SerializeMultiValueList(IReadOnlyCollection<string> values) =>
        values.Count == 0
            ? string.Empty
            : string.Join(MultiValueSeparator, values);

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
