using FluentAssertions;
using FoodSocial.Contracts.Dishes;
using FoodSocial.Dish.Application;
using Xunit;

namespace FoodSocial.Dish.Api.Tests;

public sealed class DishApplicationTests
{
    [Fact]
    public async Task CreateDishAsync_DuplicateNormalizedName_ReturnsExistingDishWithoutCreatingDuplicate()
    {
        var store = new InMemoryDishStore();
        var service = new DishCatalogService(store);
        var request = new CreateDishRequest("Phở Bò", null, "Noodle Soup", "Vietnamese", [], [], []);

        var first = await service.CreateDishAsync(request, CancellationToken.None);
        var second = await service.CreateDishAsync(request with { Name = " pho bo " }, CancellationToken.None);

        first.Status.Should().Be(DishOperationStatus.Created);
        second.Status.Should().Be(DishOperationStatus.AlreadyExists);
        second.Dish!.Id.Should().Be(first.Dish!.Id);
        store.Count.Should().Be(1);
    }

    [Fact]
    public async Task SearchDishesAsync_DefaultSearchExcludesDeprecatedDishesAndHonorsFilters()
    {
        var store = new InMemoryDishStore();
        var service = new DishCatalogService(store);
        var pho = await service.CreateDishAsync(new CreateDishRequest("Phở Bò", null, "Noodle Soup", "Vietnamese", ["pho"], [], []), CancellationToken.None);
        await service.CreateDishAsync(new CreateDishRequest("Pizza", null, "Flatbread", "Italian", [], [], []), CancellationToken.None);
        await service.DeprecateDishAsync(pho.Dish!.Id, CancellationToken.None);

        var defaultResults = await service.SearchDishesAsync(new DishSearchRequest("pho", "Noodle Soup", "Vietnamese", IncludeDeprecated: false, Limit: 10), CancellationToken.None);
        var includeDeprecatedResults = await service.SearchDishesAsync(new DishSearchRequest("pho", "Noodle Soup", "Vietnamese", IncludeDeprecated: true, Limit: 10), CancellationToken.None);

        defaultResults.Items.Should().BeEmpty();
        includeDeprecatedResults.Items.Should().ContainSingle().Which.Id.Should().Be(pho.Dish.Id);
    }
}
