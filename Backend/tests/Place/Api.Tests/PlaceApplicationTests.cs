using FluentAssertions;
using FoodSocial.Contracts.Places;
using FoodSocial.Place.Application;
using Xunit;

namespace FoodSocial.Place.Api.Tests;

public sealed class PlaceApplicationTests
{
    [Fact]
    public async Task CreatePlaceAsync_DuplicateNormalizedNameCityArea_ReturnsExistingPlaceWithoutCreatingDuplicate()
    {
        var store = new InMemoryPlaceStore();
        var service = new PlaceCatalogService(store);
        var request = new CreatePlaceRequest("Cơm Tấm Ba Ghiền", null, "84 Đặng Văn Ngữ", "Phú Nhuận", "Ho Chi Minh City", "Vietnam", null, null, ["Vietnamese"], "$$");

        var first = await service.CreatePlaceAsync(request, CancellationToken.None);
        var second = await service.CreatePlaceAsync(request with { Name = " com tam ba ghien ", City = " ho chi minh city ", Area = " phu nhuan " }, CancellationToken.None);

        first.Status.Should().Be(PlaceOperationStatus.Created);
        second.Status.Should().Be(PlaceOperationStatus.AlreadyExists);
        second.Place!.Id.Should().Be(first.Place!.Id);
        store.Count.Should().Be(1);
    }

    [Fact]
    public async Task SearchPlacesAsync_DefaultSearchExcludesInactivePlacesAndHonorsLocationFilters()
    {
        var store = new InMemoryPlaceStore();
        var service = new PlaceCatalogService(store);
        var place = await service.CreatePlaceAsync(new CreatePlaceRequest("Phở Thìn", null, "13 Lò Đúc", "Hai Bà Trưng", "Hanoi", "Vietnam", null, null, ["Vietnamese"], "$"), CancellationToken.None);
        await service.UpdatePlaceStatusAsync(place.Place!.Id, "Closed", CancellationToken.None);

        var defaultResults = await service.SearchPlacesAsync(new PlaceSearchRequest("pho", "Hanoi", "Hai Bà Trưng", "Vietnamese", "$", IncludeInactive: false, Limit: 10), CancellationToken.None);
        var includeInactiveResults = await service.SearchPlacesAsync(new PlaceSearchRequest("pho", "Hanoi", "Hai Bà Trưng", "Vietnamese", "$", IncludeInactive: true, Limit: 10), CancellationToken.None);

        defaultResults.Items.Should().BeEmpty();
        includeInactiveResults.Items.Should().ContainSingle().Which.Id.Should().Be(place.Place.Id);
    }
}
