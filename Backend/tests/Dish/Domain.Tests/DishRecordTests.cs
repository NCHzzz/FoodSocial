using FoodSocial.Dish.Domain;

namespace FoodSocial.Dish.Domain.Tests;

public sealed class DishRecordTests
{
    [Fact]
    public void Create_ShouldThrow_WhenNameIsMissing()
    {
        Action act = () => DishRecord.Create(null, null, null, null, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Dish name is required.*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenNameIsWhitespace()
    {
        Action act = () => DishRecord.Create("   ", null, null, null, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Dish name is required.*");
    }

    [Fact]
    public void Create_ShouldNormalizeVietnameseDiacritics()
    {
        var dish = DishRecord.Create("Phở bò", null, null, "vietnamese", null, null, null);

        dish.Name.Should().Be("Phở bò");
        dish.NormalizedName.Should().Be("pho bo");
    }

    [Fact]
    public void Create_ShouldCollapseWhitespace()
    {
        var dish = DishRecord.Create("  Bun   Bo   Nam   ", null, null, null, null, null, null);

        dish.NormalizedName.Should().Be("bun bo nam");
    }

    [Fact]
    public void Create_ShouldStartAsActive()
    {
        var dish = DishRecord.Create("Bun Bo", null, null, null, null, null, null);

        dish.Status.Should().Be(DishLifecycleStatus.Active);
        dish.DeprecatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Deprecate_ShouldSetStatusAndTimestamp()
    {
        var dish = DishRecord.Create("Bun Bo", null, null, null, null, null, null);
        var beforeDeprecate = DateTimeOffset.UtcNow;

        dish.Deprecate();

        dish.Status.Should().Be(DishLifecycleStatus.Deprecated);
        dish.DeprecatedAtUtc.Should().NotBeNull();
        dish.DeprecatedAtUtc.Should().BeOnOrAfter(beforeDeprecate);
        dish.UpdatedAtUtc.Should().Be(dish.DeprecatedAtUtc);
    }

    [Fact]
    public void Deprecate_ShouldBeIdempotent()
    {
        var dish = DishRecord.Create("Bun Bo", null, null, null, null, null, null);
        dish.Deprecate();
        var firstDeprecatedAt = dish.DeprecatedAtUtc;

        dish.Deprecate();

        dish.DeprecatedAtUtc.Should().Be(firstDeprecatedAt);
    }

    [Fact]
    public void Create_ShouldDeduplicateAliases()
    {
        var dish = DishRecord.Create(
            "Pho",
            null,
            null,
            null,
            new[] { "Pho", "pho", "PHỞ", "  pho  " },
            null,
            null);

        dish.Aliases.Should().HaveCount(1);
    }

    [Fact]
    public void Create_ShouldSetDefaultTenantId()
    {
        var dish = DishRecord.Create("Bun Bo", null, null, null, null, null, null);

        dish.TenantId.Should().Be(DishRecord.DefaultTenantId);
        dish.TenantId.Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    }

    [Fact]
    public void NormalizeSearchValue_ShouldHandleNullGracefully()
    {
        Action act = () => DishRecord.NormalizeSearchValue(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NormalizeSearchValue_ShouldReturnEmptyForEmptyInput()
    {
        DishRecord.NormalizeSearchValue("").Should().Be("");
        DishRecord.NormalizeSearchValue("   ").Should().Be("");
    }
}
