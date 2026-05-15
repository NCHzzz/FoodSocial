using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FoodSocial.Contracts.Identity;
using FoodSocial.Contracts.Posts;
using FoodSocial.Contracts.UserProfiles;
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
    public async Task SearchAssist_WithQuery_ReturnsDeterministicFallbackSuggestions()
    {
        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, new { }));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.PostAsJsonAsync("/api/search/assist", new { query = "spicy Vietnamese noodles", location = "District 1" });
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.GetProperty("assistStatus").GetString().Should().Be("ai-disabled-fallback");
        payload.GetProperty("fallbackUsed").GetBoolean().Should().BeTrue();
        payload.GetProperty("filters").GetProperty("location").GetString().Should().Be("District 1");
        payload.GetProperty("suggestions").GetArrayLength().Should().BeGreaterThan(0);
    }

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
            RepostCount: 0,
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
        AssertTrustedPostHeaders(forwardedRequest, expectedSessionIdentityId);
    }

    [Fact]
    public async Task UploadMedia_WithAuthenticatedCookie_ForwardsMultipartWithTrustedIdentity()
    {
        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.Created, new MediaUploadResponse(Guid.NewGuid(), "/api/media/uploads/id/original", "/api/media/uploads/id/thumbnail", "image/jpeg", 4, DateTimeOffset.UtcNow.AddDays(7), "Stored")));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("post-access-token"));
        await factory.AddCsrfHeaderAsync(client);
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent([1, 2, 3, 4]);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "file", "pho.jpg");

        using var response = await client.PostAsync("/api/media/uploads", form);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var forwardedRequest = factory.PostHandler.Requests.Should().ContainSingle().Subject;
        forwardedRequest.Method.Should().Be(HttpMethod.Post);
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be("/api/media/uploads");
        AssertTrustedPostHeaders(forwardedRequest, Guid.Parse("11111111-1111-1111-1111-111111111111"));
        forwardedRequest.Content!.Headers.ContentType!.MediaType.Should().Be("multipart/form-data");
    }

    [Fact]
    public async Task ReadMedia_WithAuthenticatedCookie_ForwardsTrustedIdentityForPendingUploadAccess()
    {
        var uploadId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        await using var factory = new BffPostTestApplicationFactory(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3, 4])
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("post-access-token"));

        using var response = await client.GetAsync($"/api/media/uploads/{uploadId}/original");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var forwardedRequest = factory.PostHandler.Requests.Should().ContainSingle().Subject;
        forwardedRequest.Method.Should().Be(HttpMethod.Get);
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be($"/api/media/uploads/{uploadId}/original");
        AssertTrustedPostHeaders(forwardedRequest, Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task ReadMedia_WithoutAuthenticatedCookie_ForwardsAsPublicRead()
    {
        var uploadId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        await using var factory = new BffPostTestApplicationFactory(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3, 4])
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync($"/api/media/uploads/{uploadId}/original");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var forwardedRequest = factory.PostHandler.Requests.Should().ContainSingle().Subject;
        forwardedRequest.Method.Should().Be(HttpMethod.Get);
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be($"/api/media/uploads/{uploadId}/original");
        forwardedRequest.Headers.Contains("X-FoodSocial-Internal-Service-Id").Should().BeFalse();
        forwardedRequest.Headers.Contains("X-FoodSocial-Identity-Id").Should().BeFalse();
        forwardedRequest.Headers.Contains("X-FoodSocial-Internal-Timestamp").Should().BeFalse();
        forwardedRequest.Headers.Contains("X-FoodSocial-Internal-Signature").Should().BeFalse();
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
    public async Task FollowingFeed_WithoutAuthenticatedSession_ReturnsUnauthorizedWithoutCallingServices()
    {
        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, new PostSearchResponse([], 20)));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/feed/following");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        factory.UserProfileHandler.Requests.Should().BeEmpty();
        factory.PostHandler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task FollowingFeed_WithEmptyFollowGraph_ReturnsEmptyFeedWithoutCallingPostApi()
    {
        var sessionIdentityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await using var factory = new BffPostTestApplicationFactory(
            _ => JsonResponse(HttpStatusCode.OK, new PostSearchResponse([], 20)),
            userProfileResponseFactory: _ => JsonResponse(HttpStatusCode.OK, new UserProfileListResponse([], TotalCount: 0)));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("following-access-token"));

        using var response = await client.GetAsync("/api/feed/following?limit=5");
        var payload = await response.Content.ReadFromJsonAsync<FollowingFeedResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().BeEquivalentTo(new FollowingFeedResponse([], 5));
        var graphRequest = factory.UserProfileHandler.Requests.Should().ContainSingle().Subject;
        graphRequest.Method.Should().Be(HttpMethod.Get);
        graphRequest.RequestUri!.PathAndQuery.Should().Be($"/api/users/{sessionIdentityId}/following");
        factory.PostHandler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task FollowingFeed_WithFollowGraph_UsesSingleBatchedPostSearch()
    {
        var firstAuthorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var secondAuthorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var olderPost = CreatePostSummary(Guid.Parse("10000000-0000-0000-0000-000000000000"), firstAuthorId, DateTimeOffset.Parse("2026-05-01T00:00:00Z"));
        var newestPost = CreatePostSummary(Guid.Parse("20000000-0000-0000-0000-000000000000"), firstAuthorId, DateTimeOffset.Parse("2026-05-03T00:00:00Z"));
        var middlePost = CreatePostSummary(Guid.Parse("30000000-0000-0000-0000-000000000000"), secondAuthorId, DateTimeOffset.Parse("2026-05-02T00:00:00Z"));

        await using var factory = new BffPostTestApplicationFactory(
            _ => JsonResponse(HttpStatusCode.OK, new PostSearchResponse([olderPost, newestPost, middlePost], 2)),
            userProfileResponseFactory: _ => JsonResponse(
                HttpStatusCode.OK,
                new UserProfileListResponse(
                    [
                        new UserProfileSummaryDto(firstAuthorId, "First", null),
                        new UserProfileSummaryDto(secondAuthorId, "Second", null)
                    ],
                    TotalCount: 2)));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("following-access-token"));

        using var response = await client.GetAsync("/api/feed/following?limit=2");
        var payload = await response.Content.ReadFromJsonAsync<FollowingFeedResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Limit.Should().Be(2);
        payload.Items.Select(item => item.Id).Should().Equal(newestPost.Id, middlePost.Id);
        factory.UserProfileHandler.Requests.Should().ContainSingle();
        var postRequest = factory.PostHandler.Requests.Should().ContainSingle().Subject;
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(postRequest.RequestUri!.Query);
        postRequest.RequestUri!.AbsolutePath.Should().Be("/api/posts/search");
        query["authorId"].Should().BeEquivalentTo(firstAuthorId.ToString(), secondAuthorId.ToString());
        query["limit"].Should().ContainSingle().Which.Should().Be("2");
        query["includeHidden"].Should().ContainSingle().Which.Should().Be(bool.FalseString);
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

    [Fact]
    public async Task Repost_WithAuthenticatedCookie_ReplacesCallerActorIdWithSessionIdentity()
    {
        var spoofedActorId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var expectedSessionIdentityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var postId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var upstreamResponse = new PostRepostResponse(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            postId,
            expectedSessionIdentityId,
            "Worth trying.",
            1,
            DateTimeOffset.UtcNow);

        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.Created, upstreamResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("post-access-token"));
        await factory.AddCsrfHeaderAsync(client);

        using var response = await client.PostAsJsonAsync($"/api/posts/{postId}/repost", new CreatePostRepostRequest(spoofedActorId, "Worth trying."));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var forwardedRequest = factory.PostHandler.Requests.Should().ContainSingle().Subject;
        var forwardedBody = await forwardedRequest.Content!.ReadFromJsonAsync<CreatePostRepostRequest>();
        forwardedBody.Should().NotBeNull();
        forwardedBody!.ActorId.Should().Be(expectedSessionIdentityId);
        forwardedBody.ActorId.Should().NotBe(spoofedActorId);
    }

    [Fact]
    public async Task GetNotifications_WithAuthenticatedCookie_ForwardsToPostNotificationsForSessionIdentity()
    {
        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, new NotificationListResponse([], 10, 0)));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("post-access-token"));

        using var response = await client.GetAsync("/api/me/notifications?limit=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var forwardedRequest = factory.PostHandler.Requests.Should().ContainSingle().Subject;
        forwardedRequest.Method.Should().Be(HttpMethod.Get);
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be("/api/notifications?limit=10");
        AssertTrustedPostHeaders(forwardedRequest, Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task GetAdminReports_WithNonModeratorRole_ReturnsForbiddenWithoutForwarding()
    {
        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, new AdminReportsResponse([], 20)));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("post-access-token"));

        using var response = await client.GetAsync("/api/admin/reports?limit=10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        factory.PostHandler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAdminReports_WithModeratorRole_ForwardsTrustedIdentityAndRoles()
    {
        await using var factory = new BffPostTestApplicationFactory(
            _ => JsonResponse(HttpStatusCode.OK, new AdminReportsResponse([], 20)),
            ["user", "moderator"]);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("post-access-token"));

        using var response = await client.GetAsync("/api/admin/reports?limit=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var forwardedRequest = factory.PostHandler.Requests.Should().ContainSingle().Subject;
        forwardedRequest.Method.Should().Be(HttpMethod.Get);
        forwardedRequest.RequestUri!.PathAndQuery.Should().Be("/api/admin/reports?limit=10");
        AssertTrustedPostHeaders(forwardedRequest, Guid.Parse("11111111-1111-1111-1111-111111111111"), "moderator,user");
    }

    [Fact]
    public async Task ResolveReport_WithModeratorRole_ReplacesCallerModeratorIdWithSessionIdentity()
    {
        var spoofedModeratorId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var expectedSessionIdentityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var reportId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var upstreamResponse = new ContentReportResponse(
            reportId,
            "Post",
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "spam",
            null,
            "ResolvedNoAction",
            expectedSessionIdentityId,
            "Reviewed manually.",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        await using var factory = new BffPostTestApplicationFactory(_ => JsonResponse(HttpStatusCode.OK, upstreamResponse), ["admin"]);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", factory.CreateAuthenticatedSessionCookie("post-access-token"));
        await factory.AddCsrfHeaderAsync(client);

        using var response = await client.PostAsJsonAsync($"/api/admin/reports/{reportId}/resolve", new ResolveContentReportRequest(spoofedModeratorId, "dismiss", "Reviewed manually."));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var forwardedRequest = factory.PostHandler.Requests.Should().ContainSingle().Subject;
        AssertTrustedPostHeaders(forwardedRequest, Guid.Parse("11111111-1111-1111-1111-111111111111"), "admin");
        var forwardedBody = await forwardedRequest.Content!.ReadFromJsonAsync<ResolveContentReportRequest>();
        forwardedBody.Should().NotBeNull();
        forwardedBody!.ModeratorId.Should().Be(expectedSessionIdentityId);
        forwardedBody.ModeratorId.Should().NotBe(spoofedModeratorId);
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

    private static PostSummaryResponse CreatePostSummary(Guid postId, Guid authorId, DateTimeOffset createdAtUtc) =>
        new(
            postId,
            authorId,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            PlaceId: null,
            "Food post",
            Rating: 5,
            PriceRange: "$$",
            Tags: ["pho"],
            PrimaryMediaUrl: "https://cdn.example.test/pho.jpg",
            IsHidden: false,
            LikeCount: 0,
            RepostCount: 0,
            CommentCount: 0,
            createdAtUtc);

    private static void AssertTrustedPostHeaders(HttpRequestMessage request, Guid identityId, string? rolesHeader = null)
    {
        request.Headers.Contains("X-FoodSocial-Internal-Secret").Should().BeFalse();
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be("test-service-token");
        request.Headers.Contains("X-FoodSocial-Internal-Service-Id").Should().BeFalse();
        request.Headers.GetValues("X-FoodSocial-Identity-Id").Should().ContainSingle().Which.Should().Be(identityId.ToString());
        request.Headers.Contains("X-FoodSocial-Internal-Timestamp").Should().BeFalse();
        request.Headers.Contains("X-FoodSocial-Internal-Signature").Should().BeFalse();
        if (rolesHeader is not null)
        {
            request.Headers.GetValues("X-FoodSocial-Identity-Roles").Should().ContainSingle().Which.Should().Be(rolesHeader);
        }
    }

    private static HttpResponseMessage JsonResponse<TValue>(HttpStatusCode statusCode, TValue value) =>
        new(statusCode)
        {
            Content = JsonContent.Create(value)
        };

    private sealed class BffPostTestApplicationFactory(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        IReadOnlyList<string>? roles = null,
        Func<HttpRequestMessage, HttpResponseMessage>? userProfileResponseFactory = null) : WebApplicationFactory<Program>
    {
        private static readonly string FullTypedPostClientName = typeof(Program).Assembly
            .GetType("FoodSocial.Bff.PostHttpClient", throwOnError: true)!
            .FullName!;
        private static readonly string FullTypedUserProfileClientName = typeof(Program).Assembly
            .GetType("FoodSocial.Bff.UserProfileHttpClient", throwOnError: true)!
            .FullName!;
        private static readonly string FullTypedIdentityClientName = typeof(Program).Assembly
            .GetType("FoodSocial.Bff.IdentityOrchestrationHttpClient", throwOnError: true)!
            .FullName!;
        private const string ShortTypedPostClientName = "PostHttpClient";
        private const string ShortTypedUserProfileClientName = "UserProfileHttpClient";
        private const string ShortTypedIdentityClientName = "IdentityOrchestrationHttpClient";
        private readonly IdentityStateHttpMessageHandler _identityHandler = new(roles ?? ["user"]);

        internal PostCaptureHttpMessageHandler PostHandler { get; } = new(responseFactory);
        internal UserProfileCaptureHttpMessageHandler UserProfileHandler { get; } = new(userProfileResponseFactory ?? (_ => JsonResponse(HttpStatusCode.OK, new UserProfileListResponse([], TotalCount: 0))));

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Keycloak:Audience", "foodsocial-api");
            builder.UseSetting("Keycloak:Realm", "foodsocial");
            builder.UseSetting("Keycloak:Authority", "https://identity.example.test/realms/foodsocial");
            builder.UseSetting("Keycloak:BffClientId", "foodsocial-bff");
            builder.UseSetting("Keycloak:BffClientSecret", "test-client-secret");
            builder.UseSetting("Keycloak:ServiceClientAccessToken", "test-service-token");
            builder.UseSetting("Services:identity-api:https:0", "https://identity-api.example.test");
            builder.UseSetting("Services:post-api:https:0", "https://post-api.example.test");
            builder.UseSetting("Services:userprofile-api:https:0", "https://userprofile-api.example.test");

            builder.ConfigureTestServices(services =>
            {
                services.Configure<HttpClientFactoryOptions>(FullTypedPostClientName, ConfigurePostClient);
                services.Configure<HttpClientFactoryOptions>(ShortTypedPostClientName, ConfigurePostClient);
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

    private sealed class IdentityStateHttpMessageHandler(IReadOnlyList<string> roles) : HttpMessageHandler
    {
        private readonly IReadOnlyList<string> _roles = roles;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new EnsureIdentityResponse(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "post-subject",
                "post-user",
                "post@example.test",
                _roles,
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
