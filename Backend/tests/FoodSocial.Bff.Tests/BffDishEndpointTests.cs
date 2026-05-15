using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FoodSocial.Contracts.Dishes;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Xunit;

namespace FoodSocial.Bff.Tests;

public sealed class BffDishEndpointTests
{
    [Fact]
    public async Task SearchDishes_PublicRead_ForwardsQueryToDishApi()
    {
        var upstreamResponse = new DishSearchResponse([
            new DishSummaryResponse(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Phở bò",
                "Noodle soup",
                "Vietnamese",
                ["pho bo"],
                "Active")
        ], Limit: 10);

        await using var factory = new BffDishTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, upstreamResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/dishes/search?query=pho&category=Noodle%20soup&cuisineType=Vietnamese&limit=10");
        var payload = await response.Content.ReadFromJsonAsync<DishSearchResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().BeEquivalentTo(upstreamResponse);
        factory.DishHandler.Requests.Should().ContainSingle();
        var forwardedRequest = factory.DishHandler.Requests.Single();
        forwardedRequest.Method.Should().Be(HttpMethod.Get);
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be("/api/dishes/search?query=pho&category=Noodle%20soup&cuisineType=Vietnamese&limit=10");
        forwardedRequest.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task SearchDishes_PublicRead_CachesSuccessfulJsonResponse()
    {
        var upstreamResponse = new DishSearchResponse([
            new DishSummaryResponse(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Phở bò",
                "Noodle soup",
                "Vietnamese",
                ["pho bo"],
                "Active")
        ], Limit: 10);

        await using var factory = new BffDishTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, upstreamResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var firstResponse = await client.GetAsync("/api/dishes/search?query=pho&limit=10");
        using var secondResponse = await client.GetAsync("/api/dishes/search?query=pho&limit=10");
        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<DishSearchResponse>();
        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<DishSearchResponse>();

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstPayload.Should().BeEquivalentTo(upstreamResponse);
        secondPayload.Should().BeEquivalentTo(upstreamResponse);
        factory.DishHandler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task GetDish_PublicRead_ForwardsToDishApi()
    {
        var dishId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var upstreamResponse = new DishDetailResponse(
            dishId,
            "Bún chả",
            "bun cha",
            "Grilled pork with noodles",
            "Noodles",
            "Vietnamese",
            ["bun cha ha noi"],
            ["pork", "rice noodles"],
            [],
            "Active",
            DateTimeOffset.Parse("2026-04-27T00:00:00Z"),
            DateTimeOffset.Parse("2026-04-27T00:00:00Z"),
            DeprecatedAtUtc: null);

        await using var factory = new BffDishTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, upstreamResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync($"/api/dishes/{dishId}");
        var payload = await response.Content.ReadFromJsonAsync<DishDetailResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().BeEquivalentTo(upstreamResponse);
        factory.DishHandler.Requests.Should().ContainSingle();
        var forwardedRequest = factory.DishHandler.Requests.Single();
        forwardedRequest.Method.Should().Be(HttpMethod.Get);
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be($"/api/dishes/{dishId}");
        forwardedRequest.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task GetDish_WhenDishApiReturnsProblemDetails_PreservesStatusAndProblemBody()
    {
        var dishId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var problem = new ProblemDetails
        {
            Type = "https://foodsocial/errors/dish/not-found",
            Title = "Dish not found",
            Detail = "The requested dish was not found.",
            Status = StatusCodes.Status404NotFound
        };

        await using var factory = new BffDishTestApplicationFactory(_ => ProblemJsonResponse(HttpStatusCode.NotFound, problem));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync($"/api/dishes/{dishId}");
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["type"]!.GetValue<string>().Should().Be(problem.Type);
        payload["title"]!.GetValue<string>().Should().Be(problem.Title);
        payload["detail"]!.GetValue<string>().Should().Be(problem.Detail);
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task SearchDishes_WhenDishApiNetworkFails_ReturnsBadGatewayProblemDetails()
    {
        await using var factory = new BffDishTestApplicationFactory(_ => throw new HttpRequestException("Connection failed."));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/dishes/search?query=pho");
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Dish service unavailable");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status502BadGateway);
        body.Should().NotContain("Connection failed", "BFF responses must not leak downstream exception details");
    }

    private static HttpResponseMessage JsonResponse<TValue>(HttpStatusCode statusCode, TValue value) =>
        new(statusCode)
        {
            Content = JsonContent.Create(value)
        };

    private static HttpResponseMessage ProblemJsonResponse(HttpStatusCode statusCode, ProblemDetails problem) =>
        new(statusCode)
        {
            Content = JsonContent.Create(problem, mediaType: new("application/problem+json"))
        };

    private sealed class BffDishTestApplicationFactory(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : WebApplicationFactory<Program>
    {
        private const string FullTypedDishClientName = "FoodSocial.Bff.DishHttpClient";
        private const string ShortTypedDishClientName = "DishHttpClient";

        internal DishCaptureHttpMessageHandler DishHandler { get; } = new(responseFactory);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Keycloak:Audience", "foodsocial-api");
            builder.UseSetting("Keycloak:Realm", "foodsocial");
            builder.UseSetting("Keycloak:Authority", "https://identity.example.test/realms/foodsocial");
            builder.UseSetting("Keycloak:BffClientId", "foodsocial-bff");
            builder.UseSetting("Keycloak:BffClientSecret", "test-client-secret");
            builder.UseSetting("Services:identity-api:https:0", "https://identity-api.example.test");
            builder.UseSetting("Services:userprofile-api:https:0", "https://userprofile-api.example.test");
            builder.UseSetting("Services:dish-api:https:0", "https://dish-api.example.test");
            builder.UseSetting("Keycloak:ServiceClientAccessToken", "test-service-token");

            builder.ConfigureTestServices(services =>
            {
                services.Configure<HttpClientFactoryOptions>(FullTypedDishClientName, ConfigureDishClient);
                services.Configure<HttpClientFactoryOptions>(ShortTypedDishClientName, ConfigureDishClient);
            });
        }

        private void ConfigureDishClient(HttpClientFactoryOptions options)
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.PrimaryHandler = DishHandler;
            });
        }
    }

    private sealed class DishCaptureHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
        private readonly List<HttpRequestMessage> _requests = [];

        internal IReadOnlyList<HttpRequestMessage> Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(CloneRequest(request));
            return _responseFactory(request) is { } response
                ? Task.FromResult(response)
                : throw new InvalidOperationException("The Dish test response factory returned null.");
        }

        private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}
