using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FoodSocial.Bff.Tests;

public sealed class BffRateLimitingEndpointTests
{
    [Fact]
    public async Task SearchAssist_WhenThresholdExceeded_ReturnsSafeProblemDetails()
    {
        await using var factory = new BffRateLimitingTestApplicationFactory(searchAssistPermitLimit: 1);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var firstResponse = await client.PostAsJsonAsync("/api/search/assist", new { query = "pho" });
        using var secondResponse = await client.PostAsJsonAsync("/api/search/assist", new { query = "pho" });
        var body = await secondResponse.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be((HttpStatusCode)429, body);
        secondResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["type"]!.GetValue<string>().Should().Be("https://foodsocial/errors/bff/rate-limit-exceeded");
        payload["title"]!.GetValue<string>().Should().Be("Rate limit exceeded");
        payload["status"]!.GetValue<int>().Should().Be(429);
        payload["rateLimitPolicy"]!.GetValue<string>().Should().Be("search-assist");
        payload["traceId"].Should().NotBeNull();
        body.Should().NotContain("test-client-secret");
    }

    [Fact]
    public async Task RiskyWrite_WhenThresholdExceeded_ReturnsRateLimitBeforeEndpointWork()
    {
        await using var factory = new BffRateLimitingTestApplicationFactory(riskyWritePermitLimit: 1);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var firstResponse = await client.PostAsJsonAsync("/api/posts", new { caption = "pho" });
        using var secondResponse = await client.PostAsJsonAsync("/api/posts", new { caption = "pho" });
        var body = await secondResponse.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        firstResponse.StatusCode.Should().NotBe((HttpStatusCode)429);
        secondResponse.StatusCode.Should().Be((HttpStatusCode)429, body);
        payload.Should().NotBeNull();
        payload!["rateLimitPolicy"]!.GetValue<string>().Should().Be("risky-write");
    }

    [Fact]
    public async Task HealthEndpoints_BypassRateLimiting()
    {
        await using var factory = new BffRateLimitingTestApplicationFactory(defaultPermitLimit: 1);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var firstResponse = await client.GetAsync("/health");
        using var secondResponse = await client.GetAsync("/health");

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RateLimiting_WhenDisabled_DoesNotRejectRiskyEndpoint()
    {
        await using var factory = new BffRateLimitingTestApplicationFactory(searchAssistPermitLimit: 1, enabled: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var firstResponse = await client.PostAsJsonAsync("/api/search/assist", new { query = "pho" });
        using var secondResponse = await client.PostAsJsonAsync("/api/search/assist", new { query = "pho" });

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class BffRateLimitingTestApplicationFactory(
        int defaultPermitLimit = 300,
        int searchAssistPermitLimit = 60,
        int riskyWritePermitLimit = 30,
        bool enabled = true) : WebApplicationFactory<Program>
    {
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
            builder.UseSetting("RateLimiting:Enabled", enabled.ToString());
            builder.UseSetting("RateLimiting:Default:PermitLimit", defaultPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.UseSetting("RateLimiting:Default:WindowSeconds", "60");
            builder.UseSetting("RateLimiting:SearchAssist:PermitLimit", searchAssistPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.UseSetting("RateLimiting:SearchAssist:WindowSeconds", "60");
            builder.UseSetting("RateLimiting:RiskyWrite:PermitLimit", riskyWritePermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.UseSetting("RateLimiting:RiskyWrite:WindowSeconds", "60");
        }
    }
}
