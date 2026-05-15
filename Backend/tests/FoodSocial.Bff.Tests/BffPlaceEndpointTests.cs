using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FoodSocial.Contracts.Places;
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

public sealed class BffPlaceEndpointTests
{
    [Fact]
    public async Task SearchPlaces_PublicRead_ForwardsQueryToPlaceApi()
    {
        var upstreamResponse = new PlaceSearchResponse([
            new PlaceSummaryResponse(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Quán Ăn Ngon",
                "18 Phan Bội Châu",
                "District 1",
                "Ho Chi Minh City",
                "Vietnam",
                ["vietnamese"],
                "$$",
                "Active")
        ], Limit: 10);

        await using var factory = new BffPlaceTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, upstreamResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/places/search?query=quan&city=Ho%20Chi%20Minh%20City&area=District%201&cuisineType=Vietnamese&priceRange=$$&limit=10");
        var payload = await response.Content.ReadFromJsonAsync<PlaceSearchResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().BeEquivalentTo(upstreamResponse);
        factory.PlaceHandler.Requests.Should().ContainSingle();
        var forwardedRequest = factory.PlaceHandler.Requests.Single();
        forwardedRequest.Method.Should().Be(HttpMethod.Get);
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be("/api/places/search?query=quan&city=Ho%20Chi%20Minh%20City&area=District%201&cuisineType=Vietnamese&priceRange=$$&limit=10");
        forwardedRequest.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task SearchPlaces_PublicRead_CachesSuccessfulJsonResponse()
    {
        var upstreamResponse = new PlaceSearchResponse([
            new PlaceSummaryResponse(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Quán Ăn Ngon",
                "18 Phan Bội Châu",
                "District 1",
                "Ho Chi Minh City",
                "Vietnam",
                ["vietnamese"],
                "$$",
                "Active")
        ], Limit: 10);

        await using var factory = new BffPlaceTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, upstreamResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var firstResponse = await client.GetAsync("/api/places/search?query=quan&limit=10");
        using var secondResponse = await client.GetAsync("/api/places/search?query=quan&limit=10");
        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<PlaceSearchResponse>();
        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<PlaceSearchResponse>();

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstPayload.Should().BeEquivalentTo(upstreamResponse);
        secondPayload.Should().BeEquivalentTo(upstreamResponse);
        factory.PlaceHandler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task GetPlace_PublicRead_ForwardsToPlaceApi()
    {
        var placeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var upstreamResponse = new PlaceDetailResponse(
            placeId,
            "Bún Chả Hương Liên",
            "bun cha huong lien",
            "Famous grilled pork noodles",
            "24 Lê Văn Hưu",
            "Hai Bà Trưng",
            "Hanoi",
            "Vietnam",
            21.0184m,
            105.8542m,
            ["vietnamese"],
            "$$",
            "Active",
            DateTimeOffset.Parse("2026-04-27T00:00:00Z"),
            DateTimeOffset.Parse("2026-04-27T00:00:00Z"));

        await using var factory = new BffPlaceTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, upstreamResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync($"/api/places/{placeId}");
        var payload = await response.Content.ReadFromJsonAsync<PlaceDetailResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().BeEquivalentTo(upstreamResponse);
        factory.PlaceHandler.Requests.Should().ContainSingle();
        var forwardedRequest = factory.PlaceHandler.Requests.Single();
        forwardedRequest.Method.Should().Be(HttpMethod.Get);
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be($"/api/places/{placeId}");
        forwardedRequest.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task GetPlace_WhenPlaceApiReturnsProblemDetails_PreservesStatusAndProblemBody()
    {
        var placeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var problem = new ProblemDetails
        {
            Type = "https://foodsocial/errors/place/not-found",
            Title = "Place not found",
            Detail = "The requested place was not found.",
            Status = StatusCodes.Status404NotFound
        };

        await using var factory = new BffPlaceTestApplicationFactory(_ => ProblemJsonResponse(HttpStatusCode.NotFound, problem));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync($"/api/places/{placeId}");
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
    public async Task SearchPlaces_WhenPlaceApiNetworkFails_ReturnsBadGatewayProblemDetails()
    {
        await using var factory = new BffPlaceTestApplicationFactory(_ => throw new HttpRequestException("Connection failed."));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/places/search?query=quan");
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Place service unavailable");
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

    private sealed class BffPlaceTestApplicationFactory(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : WebApplicationFactory<Program>
    {
        private const string FullTypedPlaceClientName = "FoodSocial.Bff.PlaceHttpClient";
        private const string ShortTypedPlaceClientName = "PlaceHttpClient";

        internal PlaceCaptureHttpMessageHandler PlaceHandler { get; } = new(responseFactory);

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
            builder.UseSetting("Keycloak:ServiceClientAccessToken", "test-service-token");

            builder.ConfigureTestServices(services =>
            {
                services.Configure<HttpClientFactoryOptions>(FullTypedPlaceClientName, ConfigurePlaceClient);
                services.Configure<HttpClientFactoryOptions>(ShortTypedPlaceClientName, ConfigurePlaceClient);
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

    private sealed class PlaceCaptureHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
        private readonly List<HttpRequestMessage> _requests = [];

        internal IReadOnlyList<HttpRequestMessage> Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(CloneRequest(request));
            return _responseFactory(request) is { } response
                ? Task.FromResult(response)
                : throw new InvalidOperationException("The Place test response factory returned null.");
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
