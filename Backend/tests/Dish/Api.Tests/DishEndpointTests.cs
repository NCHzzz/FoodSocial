using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using FoodSocial.Contracts.Dishes;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace FoodSocial.Dish.Api.Tests;

public sealed class DishEndpointTests
{
    [Fact]
    public async Task PostDish_ValidRequest_CreatesAndReturnsDishDetails()
    {
        await using var factory = new DishApiFactory();
        await factory.EnsureDatabaseCreatedAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-FoodSocial-Dish-Management-Key", DishApiFactory.ManagementApiKey);

        using var response = await client.PostAsJsonAsync(
            "/api/dishes",
            new CreateDishRequest("Bánh Mì", "Vietnamese sandwich", "Sandwich", "Vietnamese", ["banh mi"], ["bread"], []));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var payload = JsonNode.Parse(body) as JsonObject;
        payload.Should().NotBeNull();
        payload!["name"]!.GetValue<string>().Should().Be("Bánh Mì");
        payload["normalizedName"]!.GetValue<string>().Should().Be("banh mi");
        payload["status"]!.GetValue<string>().Should().Be("Active");
    }

    [Fact]
    public async Task GetDish_UnknownDish_ReturnsProblemDetailsNotFound()
    {
        await using var factory = new DishApiFactory();
        await factory.EnsureDatabaseCreatedAsync();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/dishes/{Guid.Parse("99999999-9999-9999-9999-999999999999")}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetDish_EmptyGuid_ReturnsProblemDetailsBadRequest()
    {
        await using var factory = new DishApiFactory();
        await factory.EnsureDatabaseCreatedAsync();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/dishes/{Guid.Empty}");
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Invalid dish id");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task SearchDish_AppliesFiltersAndExcludesDeprecatedByDefault()
    {
        await using var factory = new DishApiFactory();
        var activeId = await factory.SeedDishAsync("Bún Chả", "Noodles", "Vietnamese");
        _ = await factory.SeedDishAsync("Deprecated Bún", "Noodles", "Vietnamese", deprecated: true);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/dishes/search?query=bun&category=Noodles&cuisineType=Vietnamese&limit=10");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var payload = JsonNode.Parse(body) as JsonObject;
        payload.Should().NotBeNull();
        var items = payload!["items"]!.AsArray();
        items.Should().ContainSingle();
        items[0]!["id"]!.GetValue<string>().Should().Be(activeId.ToString());
    }

    [Fact]
    public async Task DeprecateDish_ActiveDish_UpdatesLifecycleAndDefaultSearchOmitsIt()
    {
        await using var factory = new DishApiFactory();
        var dishId = await factory.SeedDishAsync("Hủ Tiếu", "Noodles", "Vietnamese");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-FoodSocial-Dish-Management-Key", DishApiFactory.ManagementApiKey);

        using var deprecateResponse = await client.PatchAsync($"/api/dishes/{dishId}/deprecate", content: null);
        deprecateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var getResponse = await client.GetAsync($"/api/dishes/{dishId}");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        JsonNode.Parse(getBody)!["status"]!.GetValue<string>().Should().Be("Deprecated");

        using var searchResponse = await client.GetAsync("/api/dishes/search?query=hu+tieu");
        var searchBody = await searchResponse.Content.ReadAsStringAsync();
        JsonNode.Parse(searchBody)!["items"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task DeprecateDish_WithoutManagementAuthorization_ReturnsUnauthorized()
    {
        await using var factory = new DishApiFactory();
        var dishId = await factory.SeedDishAsync("Mì Quảng", "Noodles", "Vietnamese");
        using var client = factory.CreateClient();

        using var response = await client.PatchAsync($"/api/dishes/{dishId}/deprecate", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeprecateDish_WithWrongManagementAuthorization_ReturnsForbiddenProblemDetails()
    {
        await using var factory = new DishApiFactory();
        var dishId = await factory.SeedDishAsync("Mì Quảng", "Noodles", "Vietnamese");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-FoodSocial-Dish-Management-Key", "wrong-management-key");

        using var response = await client.PatchAsync($"/api/dishes/{dishId}/deprecate", content: null);
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Dish management authorization failed");
    }

    [Fact]
    public async Task DeprecateDish_EmptyGuid_ReturnsProblemDetailsBadRequest()
    {
        await using var factory = new DishApiFactory();
        await factory.EnsureDatabaseCreatedAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-FoodSocial-Dish-Management-Key", DishApiFactory.ManagementApiKey);

        using var response = await client.PatchAsync($"/api/dishes/{Guid.Empty}/deprecate", content: null);
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Invalid dish id");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status400BadRequest);
    }
}
