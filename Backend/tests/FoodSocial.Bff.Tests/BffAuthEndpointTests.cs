using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Nodes;
using FoodSocial.Contracts.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace FoodSocial.Bff.Tests;

public sealed class BffAuthEndpointTests
{
    [Theory]
    [InlineData("Suspended")]
    [InlineData("Deleted")]
    public async Task Session_BlockedIdentityState_ReturnsForbiddenProblemDetailsWithoutUserPayload(string blockedState)
    {
        await using var factory = new BffTestApplicationFactory(blockedState);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie());

        using var response = await client.GetAsync("/api/auth/session");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var payload = JsonNode.Parse(body) as JsonObject;

        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Identity session is not allowed");
        payload["detail"]!.GetValue<string>().Should().Be("The current identity state is not allowed to use this session.");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status403Forbidden);
        payload["type"]!.GetValue<string>().Should().Be("https://foodsocial/errors/bff/identity-session-forbidden");
        payload["user"].Should().BeNull();
        payload["identity"].Should().BeNull();
        body.Should().NotContain("blocked-subject");
        body.Should().NotContain(blockedState);
    }

    [Theory]
    [InlineData("Suspended")]
    [InlineData("Deleted")]
    public async Task Refresh_BlockedIdentityState_ReturnsForbiddenProblemDetailsWithoutRefreshSuccess(string blockedState)
    {
        await using var factory = new BffTestApplicationFactory(blockedState);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie());

        using var response = await client.PostAsJsonAsync("/api/auth/refresh", new { });
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var payload = JsonNode.Parse(body) as JsonObject;

        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Identity session is not allowed");
        payload["detail"]!.GetValue<string>().Should().Be("The current identity state is not allowed to use this session.");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status403Forbidden);
        body.Should().NotContain("\"sessionValid\":true");
        body.Should().NotContain("blocked-subject");
        body.Should().NotContain(blockedState);
    }

    [Fact]
    public async Task CookieOptions_AreExplicitForBrowserOnlyBffSessions()
    {
        await using var factory = new BffTestApplicationFactory("Active");

        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        options.Cookie.Name.Should().Be("__Host-FoodSocial.Bff");
        options.Cookie.HttpOnly.Should().BeTrue();
        options.Cookie.SecurePolicy.Should().Be(CookieSecurePolicy.Always);
        options.Cookie.SameSite.Should().Be(SameSiteMode.Lax);
        options.ExpireTimeSpan.Should().Be(TimeSpan.FromHours(8));
        options.SlidingExpiration.Should().BeTrue();
    }

    private sealed class BffTestApplicationFactory(string identityState) : WebApplicationFactory<Program>
    {
        private static readonly string FullTypedIdentityClientName = typeof(Program).Assembly
            .GetType("FoodSocial.Bff.IdentityOrchestrationHttpClient", throwOnError: true)!
            .FullName!;
        private const string ShortTypedIdentityClientName = "IdentityOrchestrationHttpClient";
        private readonly IdentityStateHttpMessageHandler _identityHandler = new(identityState);

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Keycloak:Audience", "foodsocial-api");
            builder.UseSetting("Keycloak:Realm", "foodsocial");
            builder.UseSetting("Keycloak:Authority", "https://identity.example.test/realms/foodsocial");
            builder.UseSetting("Keycloak:BffClientId", "foodsocial-bff");
            builder.UseSetting("Keycloak:BffClientSecret", "test-client-secret");
            builder.UseSetting("Services:identity-api:https:0", "https://identity-api.example.test");
            builder.UseSetting("Identity:InternalTrust:SharedSecret", "test-shared-secret");

            builder.ConfigureTestServices(services =>
            {
                services.Configure<HttpClientFactoryOptions>(FullTypedIdentityClientName, ConfigureIdentityClient);
                services.Configure<HttpClientFactoryOptions>(ShortTypedIdentityClientName, ConfigureIdentityClient);
            });
        }

        private void ConfigureIdentityClient(HttpClientFactoryOptions options)
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.PrimaryHandler = _identityHandler;
            });
        }

        internal string CreateAuthenticatedSessionCookie()
        {
            var options = Services
                .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(CookieAuthenticationDefaults.AuthenticationScheme);

            var claims = new[]
            {
                new Claim(IdentityClaims.Subject, "blocked-subject"),
                new Claim(IdentityClaims.PreferredUsername, "blocked-user"),
                new Claim(IdentityClaims.Email, "blocked@example.test")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            var properties = new AuthenticationProperties();
            properties.StoreTokens([
                new AuthenticationToken
                {
                    Name = "access_token",
                    Value = "test-access-token"
                }
            ]);

            var ticket = new AuthenticationTicket(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme);
            var cookieValue = options.TicketDataFormat.Protect(ticket);

            return $"{options.Cookie.Name}={cookieValue}";
        }
    }

    private sealed class IdentityStateHttpMessageHandler(string identityState) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new EnsureIdentityResponse(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "blocked-subject",
                "blocked-user",
                "blocked@example.test",
                ["user"],
                Created: false,
                identityState,
                DownstreamBootstrapPending: false);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });
        }
    }
}
