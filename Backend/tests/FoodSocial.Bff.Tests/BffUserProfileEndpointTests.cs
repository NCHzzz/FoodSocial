using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Nodes;
using FoodSocial.Contracts.Identity;
using FoodSocial.Contracts.UserProfiles;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace FoodSocial.Bff.Tests;

public sealed class BffUserProfileEndpointTests
{
    [Fact]
    public async Task GetProfile_PublicRead_ForwardsToUserProfileApi()
    {
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var upstreamProfile = new UserProfileResponse(
            userId,
            "Test User",
            "https://cdn.example.test/avatar.png",
            "Food explorer",
            ["Vietnamese"],
            ["Peanuts"],
            ["Vegetarian"],
            FollowerCount: 12,
            FollowingCount: 7);

        await using var factory = new BffUserProfileTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, upstreamProfile));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync($"/api/users/{userId}");
        var payload = await response.Content.ReadFromJsonAsync<UserProfileResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().BeEquivalentTo(upstreamProfile);
        factory.UserProfileHandler.Requests.Should().ContainSingle();
        var forwardedRequest = factory.UserProfileHandler.Requests.Single();
        forwardedRequest.Method.Should().Be(HttpMethod.Get);
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be($"/api/users/{userId}");
        forwardedRequest.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task GetProfile_WhenUserProfileApiIsUnavailable_ReturnsSafeProblemDetails()
    {
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await using var factory = new BffUserProfileTestApplicationFactory(_ => throw new HttpRequestException("network failure"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync($"/api/users/{userId}");
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("UserProfile service unavailable");
        payload["type"]!.GetValue<string>().Should().Be("https://foodsocial/errors/bff/user-profile-service-unavailable");
        body.Should().NotContain("network failure");
    }

    [Fact]
    public async Task UpdateProfile_WithoutAuthenticatedSessionOrBearer_ReturnsUnauthorizedWithoutRedirect()
    {
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var request = new UpdateUserProfileRequest("Updated", null, null, [], [], []);

        await using var factory = new BffUserProfileTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, new { }));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.PatchAsJsonAsync($"/api/users/{userId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Location.Should().BeNull();
        factory.UserProfileHandler.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Suspended")]
    [InlineData("Deleted")]
    public async Task UpdateProfile_BlockedIdentityState_ReturnsForbiddenWithoutForwarding(string blockedState)
    {
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var request = new UpdateUserProfileRequest("Blocked", null, null, [], [], []);

        await using var factory = new BffUserProfileTestApplicationFactory(
            _ => JsonResponse(HttpStatusCode.OK, new { }),
            identityState: blockedState);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("blocked-access-token"));
        await factory.AddCsrfHeaderAsync(client);

        using var response = await client.PatchAsJsonAsync($"/api/users/{userId}", request);
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Identity session is not allowed");
        payload["detail"]!.GetValue<string>().Should().Be("The current identity state is not allowed to use this session.");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status403Forbidden);
        payload["type"]!.GetValue<string>().Should().Be("https://foodsocial/errors/bff/identity-session-forbidden");
        body.Should().NotContain("blocked-subject");
        body.Should().NotContain(blockedState);
        factory.UserProfileHandler.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Suspended")]
    [InlineData("Deleted")]
    public async Task Follow_BlockedIdentityState_ReturnsForbiddenWithoutForwarding(string blockedState)
    {
        var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        await using var factory = new BffUserProfileTestApplicationFactory(
            _ => JsonResponse(HttpStatusCode.OK, new { }),
            identityState: blockedState);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("blocked-access-token"));
        await factory.AddCsrfHeaderAsync(client);

        using var response = await client.PostAsync($"/api/users/{userId}/follow", content: null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        body.Should().Contain("Identity session is not allowed");
        body.Should().NotContain("blocked-subject");
        body.Should().NotContain(blockedState);
        factory.UserProfileHandler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Follow_WithAuthenticatedCookie_ForwardsCookieAccessTokenAsBearerAndReturnsUpstreamResponse()
    {
        var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var upstreamResponse = new FollowUserResponse(userId, Following: true);

        await using var factory = new BffUserProfileTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, upstreamResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("cookie-access-token"));
        await factory.AddCsrfHeaderAsync(client);

        using var response = await client.PostAsync($"/api/users/{userId}/follow", content: null);
        var payload = await response.Content.ReadFromJsonAsync<FollowUserResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().BeEquivalentTo(upstreamResponse);
        factory.UserProfileHandler.Requests.Should().ContainSingle();
        var forwardedRequest = factory.UserProfileHandler.Requests.Single();
        forwardedRequest.Method.Should().Be(HttpMethod.Post);
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be($"/api/users/{userId}/follow");
        forwardedRequest.Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", "cookie-access-token"));
    }

    [Fact]
    public async Task Notifications_WithAuthenticatedCookie_ForwardsOwnerNotificationReadRoutes()
    {
        var userId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var notificationId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var upstreamList = new UserNotificationListResponse(
            [
                new UserNotificationResponse(
                    notificationId,
                    userId,
                    Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    "UserFollowed",
                    "followed you",
                    IsRead: false,
                    DateTimeOffset.Parse("2026-05-05T00:00:00Z"),
                    ReadAtUtc: null)
            ],
            Limit: 50,
            UnreadCount: 1);

        await using var factory = new BffUserProfileTestApplicationFactory(request =>
            request.Method == HttpMethod.Get
                ? JsonResponse(HttpStatusCode.OK, upstreamList)
                : JsonResponse(HttpStatusCode.OK, upstreamList.Items[0] with { IsRead = true, ReadAtUtc = DateTimeOffset.Parse("2026-05-05T00:01:00Z") }));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("notification-access-token"));

        using var listResponse = await client.GetAsync($"/api/users/{userId}/notifications");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await factory.AddCsrfHeaderAsync(client);
        using var readResponse = await client.PatchAsync($"/api/users/{userId}/notifications/{notificationId}/read", content: null);
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        factory.UserProfileHandler.Requests.Should().HaveCount(2);
        factory.UserProfileHandler.Requests[0].Method.Should().Be(HttpMethod.Get);
        factory.UserProfileHandler.Requests[0].RequestUri!.PathAndQuery.Should().Be($"/api/users/{userId}/notifications");
        factory.UserProfileHandler.Requests[0].Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", "notification-access-token"));
        factory.UserProfileHandler.Requests[1].Method.Should().Be(HttpMethod.Patch);
        factory.UserProfileHandler.Requests[1].RequestUri!.PathAndQuery.Should().Be($"/api/users/{userId}/notifications/{notificationId}/read");
        factory.UserProfileHandler.Requests[1].Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", "notification-access-token"));
    }

    [Fact]
    public async Task NotificationRead_WithAuthenticatedCookieButMissingCsrfHeader_ReturnsBadRequestWithoutForwarding()
    {
        var userId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var notificationId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        await using var factory = new BffUserProfileTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, new { }));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("cookie-access-token"));

        using var response = await client.PatchAsync($"/api/users/{userId}/notifications/{notificationId}/read", content: null);
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("CSRF token validation failed");
        factory.UserProfileHandler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateProfile_WhenUserProfileApiReturnsProblemDetails_PreservesStatusAndProblemBody()
    {
        var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var request = new UpdateUserProfileRequest("Intruder", null, null, [], [], []);
        var problem = new ProblemDetails
        {
            Type = "https://foodsocial/errors/user-profile/profile-ownership-required",
            Title = "Profile ownership required",
            Detail = "Only the profile owner can update this profile.",
            Status = StatusCodes.Status403Forbidden
        };

        await using var factory = new BffUserProfileTestApplicationFactory(_ => ProblemJsonResponse(HttpStatusCode.Forbidden, problem));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("patch-access-token"));
        await factory.AddCsrfHeaderAsync(client);

        using var response = await client.PatchAsJsonAsync($"/api/users/{userId}", request);
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["type"]!.GetValue<string>().Should().Be(problem.Type);
        payload["title"]!.GetValue<string>().Should().Be(problem.Title);
        payload["detail"]!.GetValue<string>().Should().Be(problem.Detail);
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status403Forbidden);

        var forwardedRequest = factory.UserProfileHandler.Requests.Should().ContainSingle().Subject;
        forwardedRequest.Method.Should().Be(HttpMethod.Patch);
        forwardedRequest.Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", "patch-access-token"));
    }

    [Fact]
    public async Task Follow_WithAuthenticatedCookieButMissingCsrfHeader_ReturnsBadRequestWithoutForwarding()
    {
        var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        await using var factory = new BffUserProfileTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, new { }));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("cookie-access-token"));

        using var response = await client.PostAsync($"/api/users/{userId}/follow", content: null);
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonNode.Parse(body) as JsonObject;

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("CSRF token validation failed");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status400BadRequest);
        factory.UserProfileHandler.Requests.Should().BeEmpty();
    }

    private static HttpResponseMessage JsonResponse<TValue>(HttpStatusCode statusCode, TValue value) =>
        new(statusCode)
        {
            Content = JsonContent.Create(value)
        };

    private static HttpResponseMessage ProblemJsonResponse(HttpStatusCode statusCode, ProblemDetails problem) =>
        new(statusCode)
        {
            Content = JsonContent.Create(problem, mediaType: new MediaTypeHeaderValue("application/problem+json"))
        };

    private sealed class BffUserProfileTestApplicationFactory(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        string identityState = "Active") : WebApplicationFactory<Program>
    {
        private const string FullTypedUserProfileClientName = "FoodSocial.Bff.UserProfileHttpClient";
        private const string ShortTypedUserProfileClientName = "UserProfileHttpClient";
        private static readonly string FullTypedIdentityClientName = typeof(Program).Assembly
            .GetType("FoodSocial.Bff.IdentityOrchestrationHttpClient", throwOnError: true)!
            .FullName!;
        private const string ShortTypedIdentityClientName = "IdentityOrchestrationHttpClient";
        private readonly IdentityStateHttpMessageHandler _identityHandler = new(identityState);

        internal UserProfileCaptureHttpMessageHandler UserProfileHandler { get; } = new(responseFactory);

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
            builder.UseSetting("Identity:InternalTrust:SharedSecret", "test-shared-secret");

            builder.ConfigureTestServices(services =>
            {
                services.Configure<HttpClientFactoryOptions>(FullTypedUserProfileClientName, ConfigureUserProfileClient);
                services.Configure<HttpClientFactoryOptions>(ShortTypedUserProfileClientName, ConfigureUserProfileClient);
                services.Configure<HttpClientFactoryOptions>(FullTypedIdentityClientName, ConfigureIdentityClient);
                services.Configure<HttpClientFactoryOptions>(ShortTypedIdentityClientName, ConfigureIdentityClient);
            });
        }

        internal string CreateAuthenticatedSessionCookie(string accessToken)
        {
            var options = Services
                .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(CookieAuthenticationDefaults.AuthenticationScheme);

            var claims = new[]
            {
                new Claim(IdentityClaims.Subject, "profile-subject"),
                new Claim(IdentityClaims.PreferredUsername, "profile-user"),
                new Claim(IdentityClaims.Email, "profile@example.test")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            var properties = new AuthenticationProperties();
            properties.StoreTokens([
                new AuthenticationToken
                {
                    Name = "access_token",
                    Value = accessToken
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

        private void ConfigureUserProfileClient(HttpClientFactoryOptions options)
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.PrimaryHandler = UserProfileHandler;
            });
        }

        private void ConfigureIdentityClient(HttpClientFactoryOptions options)
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.PrimaryHandler = _identityHandler;
            });
        }
    }

    private sealed record CsrfTokenResponse(string RequestToken, string HeaderName);

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

    private sealed class UserProfileCaptureHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
        private readonly List<HttpRequestMessage> _requests = [];

        internal IReadOnlyList<HttpRequestMessage> Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(CloneRequest(request));
            return Task.FromResult(_responseFactory(request));
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
