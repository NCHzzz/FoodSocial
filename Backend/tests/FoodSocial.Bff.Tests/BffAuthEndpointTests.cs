using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
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
        await factory.AddCsrfHeaderAsync(client);

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
    public async Task Refresh_WithAuthenticatedCookieButMissingCsrfHeader_ReturnsBadRequestProblemDetails()
    {
        await using var factory = new BffTestApplicationFactory("Active");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie());

        using var response = await client.PostAsJsonAsync("/api/auth/refresh", new { });
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("CSRF token validation failed");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status400BadRequest);
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

    [Fact]
    public async Task Session_WithCookiePrincipalMissingSubjectButAccessTokenPayloadHasSubject_CompletesIdentityOrchestration()
    {
        var sessionClaims = new[]
        {
            new Claim(IdentityClaims.PreferredUsername, "token-backed-user"),
            new Claim(IdentityClaims.Email, "token-backed@example.test")
        };

        await using var factory = new BffTestApplicationFactory(
            "Active",
            sessionClaims,
            CreateUnsignedAccessToken(
                subject: "token-backed-subject",
                preferredUsername: "token-backed-user",
                email: "token-backed@example.test"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie());

        using var response = await client.GetAsync("/api/auth/session");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        factory.IdentityEnsureRequestCount.Should().Be(1);
    }

    [Fact]
    public async Task Session_WithMissingSubjectAndUnreadableAccessToken_ReturnsProblemDetailsWithoutCallingIdentity()
    {
        var sessionClaims = new[]
        {
            new Claim(IdentityClaims.PreferredUsername, "missing-subject-user"),
            new Claim(IdentityClaims.Email, "missing-subject@example.test")
        };

        await using var factory = new BffTestApplicationFactory(
            "Active",
            sessionClaims,
            accessToken: "header.not-base64.signature");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie());

        using var response = await client.GetAsync("/api/auth/session");
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Identity orchestration failed");
        factory.IdentityEnsureRequestCount.Should().Be(0);
    }

    private static string CreateUnsignedAccessToken(string subject, string preferredUsername, string email)
    {
        var header = Base64UrlEncode("""{"alg":"none","typ":"JWT"}"""u8.ToArray());
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes($$"""
            {
                "{{IdentityClaims.Subject}}": "{{subject}}",
                "{{IdentityClaims.PreferredUsername}}": "{{preferredUsername}}",
                "{{IdentityClaims.Email}}": "{{email}}"
            }
            """));

        return $"{header}.{payload}.";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private sealed class BffTestApplicationFactory : WebApplicationFactory<Program>
    {
        private static readonly string FullTypedIdentityClientName = typeof(Program).Assembly
            .GetType("FoodSocial.Bff.IdentityOrchestrationHttpClient", throwOnError: true)!
            .FullName!;
        private const string ShortTypedIdentityClientName = "IdentityOrchestrationHttpClient";
        private readonly IdentityStateHttpMessageHandler _identityHandler;
        private readonly Claim[] _sessionClaims;
        private readonly string _accessToken;

        internal BffTestApplicationFactory(string identityState, Claim[]? sessionClaims = null, string? accessToken = null)
        {
            _identityHandler = new IdentityStateHttpMessageHandler(identityState);
            _sessionClaims = sessionClaims ??
            [
                new Claim(IdentityClaims.Subject, "blocked-subject"),
                new Claim(IdentityClaims.PreferredUsername, "blocked-user"),
                new Claim(IdentityClaims.Email, "blocked@example.test")
            ];
            _accessToken = accessToken ?? "test-access-token";
        }

        internal int IdentityEnsureRequestCount => _identityHandler.RequestCount;

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Keycloak:Audience", "foodsocial-api");
            builder.UseSetting("Keycloak:Realm", "foodsocial");
            builder.UseSetting("Keycloak:Authority", "https://identity.example.test/realms/foodsocial");
            builder.UseSetting("Keycloak:BffClientId", "foodsocial-bff");
            builder.UseSetting("Keycloak:BffClientSecret", "test-client-secret");
            builder.UseSetting("Keycloak:ServiceClientAccessToken", "test-service-token");
            builder.UseSetting("Services:identity-api:https:0", "https://identity-api.example.test");

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

            var principal = new ClaimsPrincipal(new ClaimsIdentity(_sessionClaims, CookieAuthenticationDefaults.AuthenticationScheme));
            var properties = new AuthenticationProperties();
            properties.StoreTokens([
                new AuthenticationToken
                {
                    Name = "access_token",
                    Value = _accessToken
                }
            ]);

            var ticket = new AuthenticationTicket(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme);
            var cookieValue = options.TicketDataFormat.Protect(ticket);

            return $"{options.Cookie.Name}={cookieValue}";
        }

        internal async Task AddCsrfHeaderAsync(HttpClient client)
        {
            using var response = await client.GetAsync("/api/auth/csrf");
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<CsrfTokenResponse>();
            payload.Should().NotBeNull();
            client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", payload!.RequestToken);
        }
    }

    private sealed record CsrfTokenResponse(string RequestToken, string HeaderName);

    private sealed class IdentityStateHttpMessageHandler(string identityState) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;

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
