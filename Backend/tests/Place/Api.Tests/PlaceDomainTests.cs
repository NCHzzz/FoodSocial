using FluentAssertions;
using FoodSocial.Place.Domain;
using Xunit;

namespace FoodSocial.Place.Api.Tests;

public sealed class PlaceDomainTests
{
    [Fact]
    public void Create_ValidPlace_NormalizesValuesAndStartsActive()
    {
        var place = PlaceRecord.Create(
            "  Quán Ăn Ngon  ",
            "  Vietnamese comfort food  ",
            "  18 Phan Bội Châu  ",
            "  District 1  ",
            "  Hồ Chí Minh  ",
            "  Việt Nam  ",
            10.7769m,
            106.7009m,
            [" Vietnamese ", "VIETNAMESE"],
            "  $$  ");

        place.Name.Should().Be("Quán Ăn Ngon");
        place.NormalizedName.Should().Be("quan an ngon");
        place.Area.Should().Be("District 1");
        place.City.Should().Be("Hồ Chí Minh");
        place.CuisineTypes.Should().ContainSingle().Which.Should().Be("vietnamese");
        place.Status.Should().Be(PlaceLifecycleStatus.Active);
    }

    [Fact]
    public void Create_BlankRequiredFields_RejectsPlace()
    {
        var action = () => PlaceRecord.Create(" ", null, " ", null, " ", "Vietnam", null, null, [], null);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Place name is required.*");
    }

    [Fact]
    public void ChangeStatus_ActivePlace_CanCloseAndSuspendWithoutChangingCreatedAt()
    {
        var place = PlaceRecord.Create("Bún Chả 145", null, "145 Bùi Viện", "District 1", "Ho Chi Minh City", "Vietnam", null, null, ["Vietnamese"], "$");
        var createdAt = place.CreatedAtUtc;

        place.ChangeStatus(PlaceLifecycleStatus.Closed);
        place.ChangeStatus(PlaceLifecycleStatus.Suspended);

        place.Status.Should().Be(PlaceLifecycleStatus.Suspended);
        place.CreatedAtUtc.Should().Be(createdAt);
        place.UpdatedAtUtc.Should().BeOnOrAfter(createdAt);
    }
}
