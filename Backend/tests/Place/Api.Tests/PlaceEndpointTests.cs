using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using FoodSocial.Contracts.Places;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace FoodSocial.Place.Api.Tests;

public sealed class PlaceEndpointTests
{
    [Fact]
    public async Task PostPlace_ValidRequest_CreatesAndReturnsPlaceDetails()
    {
        await using var factory = new PlaceApiFactory();
        await factory.EnsureDatabaseCreatedAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-FoodSocial-Place-Management-Key", PlaceApiFactory.ManagementApiKey);

        using var response = await client.PostAsJsonAsync(
            "/api/places",
            new CreatePlaceRequest("Quán Ăn Ngon", "Vietnamese comfort food", "18 Phan Bội Châu", "District 1", "Ho Chi Minh City", "Vietnam", 10.7769m, 106.7009m, ["Vietnamese"], "$$"));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var payload = JsonNode.Parse(body) as JsonObject;
        payload.Should().NotBeNull();
        payload!["name"]!.GetValue<string>().Should().Be("Quán Ăn Ngon");
        payload["normalizedName"]!.GetValue<string>().Should().Be("quan an ngon");
        payload["status"]!.GetValue<string>().Should().Be("Active");
    }

    [Fact]
    public async Task GetPlace_UnknownPlace_ReturnsProblemDetailsNotFound()
    {
        await using var factory = new PlaceApiFactory();
        await factory.EnsureDatabaseCreatedAsync();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/places/{Guid.Parse("99999999-9999-9999-9999-999999999999")}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetPlace_EmptyGuid_ReturnsProblemDetailsBadRequest()
    {
        await using var factory = new PlaceApiFactory();
        await factory.EnsureDatabaseCreatedAsync();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/places/{Guid.Empty}");
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Invalid place id");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task SearchPlaces_AppliesFiltersAndExcludesInactiveByDefault()
    {
        await using var factory = new PlaceApiFactory();
        var activeId = await factory.SeedPlaceAsync("Bánh Mì Huỳnh Hoa", "26 Lê Thị Riêng", "District 1", "Ho Chi Minh City", "Vietnamese", "$"
        );
        _ = await factory.SeedPlaceAsync("Closed Bánh Mì", "1 Old Street", "District 1", "Ho Chi Minh City", "Vietnamese", "$", status: "Closed");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/places/search?query=banh+mi&city=Ho+Chi+Minh+City&area=District+1&cuisineType=Vietnamese&priceRange=$&limit=10");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var payload = JsonNode.Parse(body) as JsonObject;
        payload.Should().NotBeNull();
        var items = payload!["items"]!.AsArray();
        items.Should().ContainSingle();
        items[0]!["id"]!.GetValue<string>().Should().Be(activeId.ToString());
    }

    [Fact]
    public async Task PatchPlaceStatus_ActivePlace_UpdatesLifecycleAndDefaultSearchOmitsIt()
    {
        await using var factory = new PlaceApiFactory();
        var placeId = await factory.SeedPlaceAsync("Bún Chả Hương Liên", "24 Lê Văn Hưu", "Hai Bà Trưng", "Hanoi", "Vietnamese", "$$");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-FoodSocial-Place-Management-Key", PlaceApiFactory.ManagementApiKey);

        using var statusResponse = await client.PatchAsJsonAsync($"/api/places/{placeId}/status", new UpdatePlaceStatusRequest("Closed"));
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var getResponse = await client.GetAsync($"/api/places/{placeId}");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        JsonNode.Parse(getBody)!["status"]!.GetValue<string>().Should().Be("Closed");

        using var searchResponse = await client.GetAsync("/api/places/search?query=bun+cha");
        var searchBody = await searchResponse.Content.ReadAsStringAsync();
        JsonNode.Parse(searchBody)!["items"]!.AsArray().Should().BeEmpty();
    }

    [Fact]
    public async Task PatchPlaceStatus_WithoutManagementAuthorization_ReturnsUnauthorized()
    {
        await using var factory = new PlaceApiFactory();
        var placeId = await factory.SeedPlaceAsync("Mì Quảng 85", "85 Street", "Thanh Khê", "Da Nang", "Vietnamese", "$"
        );
        using var client = factory.CreateClient();

        using var response = await client.PatchAsJsonAsync($"/api/places/{placeId}/status", new UpdatePlaceStatusRequest("Suspended"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PatchPlaceStatus_WithWrongManagementAuthorization_ReturnsForbiddenProblemDetails()
    {
        await using var factory = new PlaceApiFactory();
        var placeId = await factory.SeedPlaceAsync("Mì Quảng 85", "85 Street", "Thanh Khê", "Da Nang", "Vietnamese", "$"
        );
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-FoodSocial-Place-Management-Key", "wrong-management-key");

        using var response = await client.PatchAsJsonAsync($"/api/places/{placeId}/status", new UpdatePlaceStatusRequest("Suspended"));
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Place management authorization failed");
    }

    [Fact]
    public async Task PatchPlaceStatus_EmptyGuid_ReturnsProblemDetailsBadRequest()
    {
        await using var factory = new PlaceApiFactory();
        await factory.EnsureDatabaseCreatedAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-FoodSocial-Place-Management-Key", PlaceApiFactory.ManagementApiKey);

        using var response = await client.PatchAsJsonAsync($"/api/places/{Guid.Empty}/status", new UpdatePlaceStatusRequest("Closed"));
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Invalid place id");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status400BadRequest);
    }
}
