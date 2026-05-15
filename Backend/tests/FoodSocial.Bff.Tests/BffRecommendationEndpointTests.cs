using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FoodSocial.Contracts.Dishes;
using FoodSocial.Contracts.Places;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Xunit;

namespace FoodSocial.Bff.Tests;

public sealed class BffRecommendationEndpointTests
{
    [Fact]
    public async Task GetRecommendations_ReturnsDishPrimaryPlaceSecondaryDeterministicFallback()
    {
        var dishResponse = new DishSearchResponse([
            new DishSummaryResponse(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Phở bò",
                "Noodle soup",
                "Vietnamese",
                ["pho bo"],
                "Active")
        ], Limit: 5);
        var placeResponse = new PlaceSearchResponse([
            new PlaceSummaryResponse(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Phở Lệ",
                "413 Nguyễn Trãi",
                "District 5",
                "Ho Chi Minh City",
                "Vietnam",
                ["vietnamese"],
                "$$",
                "Active")
        ], Limit: 5);

        await using var factory = new BffRecommendationTestApplicationFactory(
            _ => JsonResponse(HttpStatusCode.OK, dishResponse),
            _ => JsonResponse(HttpStatusCode.OK, placeResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/recommendations?query=pho&location=Ho%20Chi%20Minh%20City&limit=5");
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        payload.Should().NotBeNull();
        payload!["recommendationStatus"]!.GetValue<string>().Should().Be("ai-disabled-fallback");
        payload["fallbackUsed"]!.GetValue<bool>().Should().BeTrue();
        payload["dishes"]!.AsArray().Should().HaveCount(1);
        payload["places"]!.AsArray().Should().HaveCount(1);
        payload["dishes"]![0]!["rank"]!.GetValue<int>().Should().Be(1);
        payload["places"]![0]!["rank"]!.GetValue<int>().Should().Be(1);
        payload["explanation"]!.GetValue<string>().Should().Contain("dish-primary");

        factory.DishHandler.Requests.Should().ContainSingle();
        factory.PlaceHandler.Requests.Should().ContainSingle();
        factory.DishHandler.Requests.Single().RequestUri!.PathAndQuery.Should().Be("/api/dishes/search?limit=5&query=pho");
        factory.PlaceHandler.Requests.Single().RequestUri!.PathAndQuery.Should().Be("/api/places/search?limit=5&query=pho&city=Ho%20Chi%20Minh%20City");
    }

    [Fact]
    public async Task GetRecommendations_WhenDishServiceFails_DegradesWithoutFiveHundred()
    {
        var placeResponse = new PlaceSearchResponse([
            new PlaceSummaryResponse(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Phở Lệ",
                "413 Nguyễn Trãi",
                "District 5",
                "Ho Chi Minh City",
                "Vietnam",
                ["vietnamese"],
                "$$",
                "Active")
        ], Limit: 5);

        await using var factory = new BffRecommendationTestApplicationFactory(
            _ => throw new HttpRequestException("dish offline with secret=do-not-leak"),
            _ => JsonResponse(HttpStatusCode.OK, placeResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/recommendations?query=pho&limit=5");
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        payload.Should().NotBeNull();
        payload!["dishes"]!.AsArray().Should().BeEmpty();
        payload["places"]!.AsArray().Should().HaveCount(1);
        payload["fallbackReasons"]!.AsArray().Select(reason => reason!.GetValue<string>()).Should().Contain(reason => reason.Contains("Dish catalog retrieval failed", StringComparison.Ordinal));
        body.Should().NotContain("do-not-leak");
    }

    [Fact]
    public async Task GetRecommendations_WhenAiEnabledStillUsesUnconfiguredFallbackWithoutProviderSecrets()
    {
        await using var factory = new BffRecommendationTestApplicationFactory(
            _ => JsonResponse(HttpStatusCode.OK, new DishSearchResponse([], Limit: 5)),
            _ => JsonResponse(HttpStatusCode.OK, new PlaceSearchResponse([], Limit: 5)),
            recommendationsAiEnabled: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/recommendations?query=pho&limit=5");
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        payload.Should().NotBeNull();
        payload!["recommendationStatus"]!.GetValue<string>().Should().Be("provider-unconfigured-fallback");
        payload["fallbackUsed"]!.GetValue<bool>().Should().BeTrue();
        body.Should().NotContain("apiKey");
        body.ToLowerInvariant().Should().NotContain("secret");
    }

    [Fact]
    public async Task GetRecommendations_WhenLimitInvalid_ReturnsProblemDetails()
    {
        await using var factory = new BffRecommendationTestApplicationFactory(
            _ => JsonResponse(HttpStatusCode.OK, new DishSearchResponse([], Limit: 5)),
            _ => JsonResponse(HttpStatusCode.OK, new PlaceSearchResponse([], Limit: 5)));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/recommendations?limit=not-a-number");
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["type"]!.GetValue<string>().Should().Be("https://foodsocial/errors/bff/invalid-recommendation-request");
        payload["status"]!.GetValue<int>().Should().Be(400);
    }

    private static HttpResponseMessage JsonResponse<TValue>(HttpStatusCode statusCode, TValue value) =>
        new(statusCode)
        {
            Content = JsonContent.Create(value)
        };

    private sealed class BffRecommendationTestApplicationFactory(
        Func<HttpRequestMessage, HttpResponseMessage> dishResponseFactory,
        Func<HttpRequestMessage, HttpResponseMessage> placeResponseFactory,
        bool recommendationsAiEnabled = false) : WebApplicationFactory<Program>
    {
        private const string FullTypedDishClientName = "FoodSocial.Bff.DishHttpClient";
        private const string ShortTypedDishClientName = "DishHttpClient";
        private const string FullTypedPlaceClientName = "FoodSocial.Bff.PlaceHttpClient";
        private const string ShortTypedPlaceClientName = "PlaceHttpClient";

        internal CaptureHttpMessageHandler DishHandler { get; } = new(dishResponseFactory);
        internal CaptureHttpMessageHandler PlaceHandler { get; } = new(placeResponseFactory);

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
            builder.UseSetting("Services:place-api:https:0", "https://place-api.example.test");
            builder.UseSetting("Services:post-api:https:0", "https://post-api.example.test");
            builder.UseSetting("Keycloak:ServiceClientAccessToken", "test-service-token");
            builder.UseSetting("Recommendations:AiEnabled", recommendationsAiEnabled.ToString());
            builder.UseSetting("Recommendations:DefaultLimit", "10");
            builder.UseSetting("Recommendations:MaxLimit", "25");

            builder.ConfigureTestServices(services =>
            {
                services.Configure<HttpClientFactoryOptions>(FullTypedDishClientName, ConfigureDishClient);
                services.Configure<HttpClientFactoryOptions>(ShortTypedDishClientName, ConfigureDishClient);
                services.Configure<HttpClientFactoryOptions>(FullTypedPlaceClientName, ConfigurePlaceClient);
                services.Configure<HttpClientFactoryOptions>(ShortTypedPlaceClientName, ConfigurePlaceClient);
            });
        }

        private void ConfigureDishClient(HttpClientFactoryOptions options)
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.PrimaryHandler = DishHandler;
            });
        }

        private void ConfigurePlaceClient(HttpClientFactoryOptions options)
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.PrimaryHandler = PlaceHandler;
            });
        }
    }

    private sealed class CaptureHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
        private readonly List<HttpRequestMessage> _requests = [];

        internal IReadOnlyList<HttpRequestMessage> Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(CloneRequest(request));
            return _responseFactory(request) is { } response
                ? Task.FromResult(response)
                : throw new InvalidOperationException("The recommendation test response factory returned null.");
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
