using FluentAssertions;
using FoodSocial.Dish.Domain;
using Xunit;

namespace FoodSocial.Dish.Api.Tests;

public sealed class DishDomainTests
{
    [Fact]
    public void Create_ValidDish_NormalizesValuesAndStartsActive()
    {
        var dish = DishRecord.Create(
            "  Phở Bò  ",
            "  Beef noodle soup  ",
            "  Noodle Soup  ",
            "  Vietnamese  ",
            [" pho bo ", "PHỞ BÒ"],
            [" rice noodles ", " beef "],
            [" dairy-free "]);

        dish.Name.Should().Be("Phở Bò");
        dish.NormalizedName.Should().Be("pho bo");
        dish.Description.Should().Be("Beef noodle soup");
        dish.Category.Should().Be("Noodle Soup");
        dish.CuisineType.Should().Be("Vietnamese");
        dish.Aliases.Should().ContainSingle().Which.Should().Be("pho bo");
        dish.Ingredients.Should().Equal("rice noodles", "beef");
        dish.DietaryFlags.Should().Equal("dairy-free");
        dish.Status.Should().Be(DishLifecycleStatus.Active);
        dish.DeprecatedAtUtc.Should().BeNull();
    }

    [Theory]
    [InlineData("Bún Đậu", "bun dau")]
    [InlineData("Đà Nẵng mì quảng", "da nang mi quang")]
    [InlineData("Cơm Tấm", "com tam")]
    public void NormalizeSearchValue_VietnameseText_MapsAccentAndDStroke(string value, string expected)
    {
        DishRecord.NormalizeSearchValue(value).Should().Be(expected);
    }

    [Fact]
    public void Create_BlankName_RejectsDish()
    {
        var action = () => DishRecord.Create("   ", null, null, null, [], [], []);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Dish name is required.*");
    }

    [Fact]
    public void Deprecate_ActiveDish_MarksDeprecatedAndKeepsRecordStable()
    {
        var dish = DishRecord.Create("Bún bò Huế", null, "Noodle Soup", "Vietnamese", [], [], []);
        var createdAt = dish.CreatedAtUtc;

        dish.Deprecate();

        dish.Status.Should().Be(DishLifecycleStatus.Deprecated);
        dish.CreatedAtUtc.Should().Be(createdAt);
        dish.DeprecatedAtUtc.Should().NotBeNull();
        dish.UpdatedAtUtc.Should().BeOnOrAfter(createdAt);
    }
}
