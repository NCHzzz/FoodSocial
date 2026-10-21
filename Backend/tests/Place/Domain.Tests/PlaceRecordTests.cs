using FoodSocial.Place.Domain;

namespace FoodSocial.Place.Domain.Tests;

public sealed class PlaceRecordTests
{
    [Fact]
    public void Create_ShouldThrow_WhenNameIsMissing()
    {
        Action act = () => PlaceRecord.Create(null, null, "1 St", "District 1", "HCMC", "VN", null, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Place name is required.*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenAddressIsMissing()
    {
        Action act = () => PlaceRecord.Create("Quan An", null, null, "District 1", "HCMC", "VN", null, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Place address is required.*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenAreaIsMissing()
    {
        Action act = () => PlaceRecord.Create("Quan An", null, "1 St", null, "HCMC", "VN", null, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Place area is required.*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenCityIsMissing()
    {
        Action act = () => PlaceRecord.Create("Quan An", null, "1 St", "District 1", null, "VN", null, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Place city is required.*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenCountryIsMissing()
    {
        Action act = () => PlaceRecord.Create("Quan An", null, "1 St", "District 1", "HCMC", null, null, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Place country is required.*");
    }

    [Theory]
    [InlineData(-90.5)]
    [InlineData(90.5)]
    public void Create_ShouldThrow_WhenLatitudeOutOfRange(decimal latitude)
    {
        Action act = () => PlaceRecord.Create("Quan An", null, "1 St", "District 1", "HCMC", "VN", latitude, 0m, null, null);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*latitude must be between -90 and 90*");
    }

    [Theory]
    [InlineData(-180.5)]
    [InlineData(180.5)]
    public void Create_ShouldThrow_WhenLongitudeOutOfRange(decimal longitude)
    {
        Action act = () => PlaceRecord.Create("Quan An", null, "1 St", "District 1", "HCMC", "VN", 0m, longitude, null, null);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*longitude must be between -180 and 180*");
    }

    [Fact]
    public void Create_ShouldAcceptBoundaryCoordinates()
    {
        var place = PlaceRecord.Create("Quan An", null, "1 St", "District 1", "HCMC", "VN", -90m, 180m, null, null);

        place.Latitude.Should().Be(-90m);
        place.Longitude.Should().Be(180m);
    }

    [Fact]
    public void Create_ShouldNormalizeVietnameseDiacritics()
    {
        var place = PlaceRecord.Create("Quán Ăn Đặc Biệt", null, "1 Nguyễn Huệ", "District 1", "HCMC", "VN", null, null, null, null);

        place.NormalizedName.Should().Be("quan an dac biet");
        place.NormalizedCity.Should().Be("hcmc");
    }

    [Fact]
    public void Create_ShouldStartAsActive()
    {
        var place = PlaceRecord.Create("Quan An", null, "1 St", "District 1", "HCMC", "VN", null, null, null, null);

        place.Status.Should().Be(PlaceLifecycleStatus.Active);
    }

    [Fact]
    public void ChangeStatus_ShouldUpdateStatusAndTimestamp()
    {
        var place = PlaceRecord.Create("Quan An", null, "1 St", "District 1", "HCMC", "VN", null, null, null, null);
        var beforeChange = DateTimeOffset.UtcNow;

        place.ChangeStatus(PlaceLifecycleStatus.Closed);

        place.Status.Should().Be(PlaceLifecycleStatus.Closed);
        place.UpdatedAtUtc.Should().BeOnOrAfter(beforeChange);
    }

    [Fact]
    public void ChangeStatus_ShouldBeIdempotent()
    {
        var place = PlaceRecord.Create("Quan An", null, "1 St", "District 1", "HCMC", "VN", null, null, null, null);
        place.ChangeStatus(PlaceLifecycleStatus.Closed);
        var firstUpdateAt = place.UpdatedAtUtc;

        place.ChangeStatus(PlaceLifecycleStatus.Closed);

        place.UpdatedAtUtc.Should().Be(firstUpdateAt);
    }

    [Fact]
    public void Create_ShouldDeduplicateCuisineTypes()
    {
        var place = PlaceRecord.Create(
            "Quan An",
            null,
            "1 St",
            "District 1",
            "HCMC",
            "VN",
            null,
            null,
            new[] { "vietnamese", "VIETNAMESE", "Vietnamese" },
            null);

        place.CuisineTypes.Should().HaveCount(1);
    }
}
