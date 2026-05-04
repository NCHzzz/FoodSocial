using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FoodSocial.Contracts.Identity;
using FoodSocial.Contracts.Posts;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace FoodSocial.Bff.Tests;

public sealed class BffPostEndpointTests
{
    [Fact]
    public async Task CreatePost_WithoutAuthenticatedSession_ReturnsUnauthorizedWithoutForwarding()
    {
        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.Created, new { }));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.PostAsJsonAsync("/api/posts", CreatePostRequestWithAuthor(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        factory.PostHandler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePost_WithAuthenticatedCookieButMissingCsrf_ReturnsBadRequestWithoutForwarding()
    {
        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.Created, new { }));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("post-access-token"));

        using var response = await client.PostAsJsonAsync("/api/posts", CreatePostRequestWithAuthor(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        factory.PostHandler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePost_WithAuthenticatedCookie_ReplacesCallerAuthorIdWithSessionIdentity()
    {
        var spoofedAuthorId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var expectedSessionIdentityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var upstreamResponse = new PostDetailResponse(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            expectedSessionIdentityId,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            null,
            "Great pho",
            5,
            "$$",
            ["pho"],
            null,
            "Published",
            IsHidden: false,
            LikeCount: 0,
            CommentCount: 0,
            [],
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.Created, upstreamResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("post-access-token"));
        await factory.AddCsrfHeaderAsync(client);

        using var response = await client.PostAsJsonAsync("/api/posts", CreatePostRequestWithAuthor(spoofedAuthorId));
        var payload = await response.Content.ReadFromJsonAsync<PostDetailResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        payload.Should().BeEquivalentTo(upstreamResponse);
        var forwardedRequest = factory.PostHandler.Requests.Should().ContainSingle().Subject;
        forwardedRequest.Method.Should().Be(HttpMethod.Post);
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be("/api/posts");

        var forwardedBody = await forwardedRequest.Content!.ReadFromJsonAsync<CreatePostRequest>();
        forwardedBody.Should().NotBeNull();
        forwardedBody!.AuthorId.Should().Be(expectedSessionIdentityId);
        forwardedBody.AuthorId.Should().NotBe(spoofedAuthorId);
        forwardedRequest.Headers.GetValues("X-FoodSocial-Internal-Secret").Should().ContainSingle().Which.Should().Be("test-post-shared-secret");
        forwardedRequest.Headers.GetValues("X-FoodSocial-Identity-Id").Should().ContainSingle().Which.Should().Be(expectedSessionIdentityId.ToString());
    }

    [Fact]
    public async Task SearchPosts_StripsIncludeHiddenBeforeForwardingPublicRequest()
    {
        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, new PostSearchResponse([], 20)));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/posts/search?query=pho&includeHidden=true&limit=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var forwardedRequest = factory.PostHandler.Requests.Should().ContainSingle().Subject;
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be("/api/posts/search?query=pho&limit=5&includeHidden=False");
    }

    [Fact]
    public async Task CreatePost_WithMalformedJson_ReturnsBadRequestWithoutForwarding()
    {
        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.Created, new { }));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("post-access-token"));
        await factory.AddCsrfHeaderAsync(client);
        using var content = new StringContent("{not-json", System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/posts", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        factory.PostHandler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task ReportPost_WithAuthenticatedCookie_ReplacesCallerReporterIdWithSessionIdentity()
    {
        var spoofedReporterId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var expectedSessionIdentityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var postId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var upstreamResponse = new ContentReportResponse(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "Post",
            postId,
            expectedSessionIdentityId,
            "spam",
            "Looks seeded",
            "Submitted",
            null,
            null,
            DateTimeOffset.UtcNow,
            null);

        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.Created, upstreamResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("post-access-token"));
        await factory.AddCsrfHeaderAsync(client);

        using var response = await client.PostAsJsonAsync($"/api/posts/{postId}/report", new CreateContentReportRequest(spoofedReporterId, "spam", "Looks seeded"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var forwardedRequest = factory.PostHandler.Requests.Should().ContainSingle().Subject;
        var forwardedBody = await forwardedRequest.Content!.ReadFromJsonAsync<CreateContentReportRequest>();
        forwardedBody.Should().NotBeNull();
        forwardedBody!.ReporterId.Should().Be(expectedSessionIdentityId);
        forwardedBody.ReporterId.Should().NotBe(spoofedReporterId);
    }

    private static CreatePostRequest CreatePostRequestWithAuthor(Guid authorId) =>
        new(
            authorId,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Great pho",
            [new PostMediaRequest("https://cdn.example.test/pho.jpg", IsPrimary: true)],
            Rating: 5,
            PriceRange: "$$",
            Tags: ["pho"]);

    private static HttpResponseMessage JsonResponse<TValue>(HttpStatusCode statusCode, TValue value) =>
        new(statusCode)
        {
            Content = JsonContent.Create(value)
        };

    private sealed class BffPostTestApplicationFactory(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : WebApplicationFactory<Program>
    {
        private static readonly string FullTypedPostClientName = typeof(Program).Assembly
            .GetType("FoodSocial.Bff.PostHttpClient", throwOnError: true)!
            .FullName!;
        private static readonly string FullTypedIdentityClientName = typeof(Program).Assembly
            .GetType("FoodSocial.Bff.IdentityOrchestrationHttpClient", throwOnError: true)!
            .FullName!;
        private const string ShortTypedPostClientName = "PostHttpClient";
        private const string ShortTypedIdentityClientName = "IdentityOrchestrationHttpClient";
        private readonly IdentityStateHttpMessageHandler _identityHandler = new();

        internal PostCaptureHttpMessageHandler PostHandler { get; } = new(responseFactory);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Keycloak:Audience", "foodsocial-api");
            builder.UseSetting("Keycloak:Realm", "foodsocial");
            builder.UseSetting("Keycloak:Authority", "https://identity.example.test/realms/foodsocial");
            builder.UseSetting("Keycloak:BffClientId", "foodsocial-bff");
            builder.UseSetting("Keycloak:BffClientSecret", "test-client-secret");
            builder.UseSetting("Services:identity-api:https:0", "https://identity-api.example.test");
            builder.UseSetting("Services:post-api:https:0", "https://post-api.example.test");
            builder.UseSetting("Identity:InternalTrust:SharedSecret", "test-shared-secret");
            builder.UseSetting("Post:InternalTrust:SharedSecret", "test-post-shared-secret");

            builder.ConfigureTestServices(services =>
            {
                services.Configure<HttpClientFactoryOptions>(FullTypedPostClientName, ConfigurePostClient);
                services.Configure<HttpClientFactoryOptions>(ShortTypedPostClientName, ConfigurePostClient);
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
                new Claim(IdentityClaims.Subject, "post-subject"),
                new Claim(IdentityClaims.PreferredUsername, "post-user"),
                new Claim(IdentityClaims.Email, "post@example.test")
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

        private void ConfigurePostClient(HttpClientFactoryOptions options)
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.PrimaryHandler = PostHandler;
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

    private sealed class IdentityStateHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new EnsureIdentityResponse(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "post-subject",
                "post-user",
                "post@example.test",
                ["user"],
                Created: false,
                "Active",
                DownstreamBootstrapPending: false);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            });
        }
    }

    private sealed class PostCaptureHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
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

            if (request.Content is not null)
            {
                var contentBytes = request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                clone.Content = new ByteArrayContent(contentBytes);
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
        }
    }
}
