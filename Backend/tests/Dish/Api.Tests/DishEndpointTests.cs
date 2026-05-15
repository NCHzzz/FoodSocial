using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using FoodSocial.Contracts.Dishes;
using FoodSocial.Dish.Infrastructure.Persistence;
using FoodSocial.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DishDbContext>();
        var outbox = await dbContext.IntegrationOutboxMessages.SingleAsync();
        outbox.EventType.Should().Be(nameof(DishCatalogChangedIntegrationEvent));
        outbox.DeliveryState.Should().BeOneOf(IntegrationOutboxDeliveryState.Pending, IntegrationOutboxDeliveryState.FailedRetryable);
        outbox.RoutingKey.Should().Be("dish.catalog.created.v1");
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
        payload!["type"]!.GetValue<string>().Should().Be("https://foodsocial/errors/common/bad-request");
        payload!["title"]!.GetValue<string>().Should().Be("Invalid dish id");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status400BadRequest);
        payload["instance"]!.GetValue<string>().Should().Be($"/api/dishes/{Guid.Empty}");
        payload["traceId"].Should().NotBeNull();
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
    public async Task ReadAndSearch_ExcludeForeignTenantDishes()
    {
        await using var factory = new DishApiFactory();
        var defaultTenantId = await factory.SeedDishAsync("Tenant Phở", "Noodles", "Vietnamese");
        var foreignTenantId = await factory.SeedDishInTenantAsync("Foreign Tenant Phở", Guid.Parse("00000000-0000-0000-0000-000000000002"));
        using var client = factory.CreateClient();

        using var foreignRead = await client.GetAsync($"/api/dishes/{foreignTenantId}");
        using var search = await client.GetAsync("/api/dishes/search?query=pho&limit=10");

        foreignRead.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var searchBody = await search.Content.ReadAsStringAsync();
        search.StatusCode.Should().Be(HttpStatusCode.OK, searchBody);
        var items = (JsonNode.Parse(searchBody) as JsonObject)!["items"]!.AsArray();
        items.Select(item => item!["id"]!.GetValue<string>()).Should().Contain(defaultTenantId.ToString());
        items.Select(item => item!["id"]!.GetValue<string>()).Should().NotContain(foreignTenantId.ToString());
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
