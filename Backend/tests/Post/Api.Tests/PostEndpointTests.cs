using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FoodSocial.Post.Api.Tests;

public sealed class PostEndpointTests
{
    [Fact]
    public async Task CreateAndReadPost_ValidFoodPost_PersistsDetails()
    {
        await using var factory = new PostApiFactory(); await factory.EnsureDatabaseCreatedAsync(); using var client = factory.CreateTrustedClient();
        var authorId = Guid.NewGuid(); var dishId = Guid.NewGuid(); factory.RegisterActiveDish(dishId);
        using var create = await client.PostAsJsonAsync("/api/posts", new CreatePostRequest(authorId, dishId, "Best phở", [new PostMediaRequest("https://cdn.example/pho.jpg", IsPrimary: true)], Rating: 5, Tags: ["pho"]));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<PostDetailResponse>())!;

        using var read = await client.GetAsync($"/api/posts/{created.Id}");
        var detail = (await read.Content.ReadFromJsonAsync<PostDetailResponse>())!;
        detail.AuthorId.Should().Be(PostApiFactory.TestCallerIdentityId); detail.DishId.Should().Be(dishId); detail.Media.Should().ContainSingle(m => m.IsPrimary); detail.Caption.Should().Be("Best phở");
    }

    [Fact]
    public async Task UploadMedia_StoresOriginalAndDerivativeReference()
    {
        await using var factory = new PostApiFactory();
        using var client = factory.CreateTrustedClient();
        using var form = new MultipartFormDataContent();
        byte[] jpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4];
        using var file = new ByteArrayContent(jpegBytes);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "file", "pho.jpg");

        using var upload = await client.PostAsync("/api/media/uploads", form);

        upload.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = (await upload.Content.ReadFromJsonAsync<MediaUploadResponse>())!;
        payload.Url.Should().Contain($"/api/media/uploads/{payload.UploadId}/original");
        payload.ThumbnailUrl.Should().Contain($"/api/media/uploads/{payload.UploadId}/thumbnail");

        using var read = await client.GetAsync(payload.Url);
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        read.Content.Headers.ContentType?.MediaType.Should().Be("image/jpeg");
        (await read.Content.ReadAsByteArrayAsync()).Should().Equal(jpegBytes);
    }

    [Fact]
    public async Task UploadMedia_RejectsUnsafeImageTypesAndMismatchedBytes()
    {
        await using var factory = new PostApiFactory();
        using var client = factory.CreateTrustedClient();

        using var svgForm = new MultipartFormDataContent();
        using var svgFile = new ByteArrayContent("<svg><script>alert(1)</script></svg>"u8.ToArray());
        svgFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/svg+xml");
        svgForm.Add(svgFile, "file", "payload.svg");

        using var svgUpload = await client.PostAsync("/api/media/uploads", svgForm);
        svgUpload.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var mismatchedForm = new MultipartFormDataContent();
        using var mismatchedFile = new ByteArrayContent("not-a-real-jpeg"u8.ToArray());
        mismatchedFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        mismatchedForm.Add(mismatchedFile, "file", "payload.jpg");

        using var mismatchedUpload = await client.PostAsync("/api/media/uploads", mismatchedForm);
        mismatchedUpload.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadMedia_RejectsMissingFileWithoutServerError()
    {
        await using var factory = new PostApiFactory();
        using var client = factory.CreateTrustedClient();
        using var form = new MultipartFormDataContent();

        using var upload = await client.PostAsync("/api/media/uploads", form);

        upload.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        upload.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData("dish")]
    [InlineData("caption")]
    [InlineData("media")]
    public async Task CreatePost_InvalidRequiredFields_ReturnsProblemDetails(string missing)
    {
        await using var factory = new PostApiFactory(); await factory.EnsureDatabaseCreatedAsync(); using var client = factory.CreateTrustedClient();
        var validDishId = Guid.NewGuid(); factory.RegisterActiveDish(validDishId);
        var request = new CreatePostRequest(Guid.NewGuid(), missing == "dish" ? Guid.Empty : validDishId, missing == "caption" ? " " : "caption", missing == "media" ? [] : [new PostMediaRequest("https://cdn.example/a.jpg", IsPrimary: true)]);
        using var response = await client.PostAsJsonAsync("/api/posts", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest); response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData("unknown-dish")]
    [InlineData("deprecated-dish")]
    [InlineData("unknown-place")]
    [InlineData("closed-place")]
    public async Task CreatePost_InvalidCatalogReferences_ReturnsProblemDetails(string scenario)
    {
        await using var factory = new PostApiFactory();
        await factory.EnsureDatabaseCreatedAsync();
        using var client = factory.CreateTrustedClient();
        var dishId = Guid.NewGuid();
        var placeId = Guid.NewGuid();

        if (scenario != "unknown-dish")
        {
            if (scenario == "deprecated-dish") factory.RegisterDeprecatedDish(dishId); else factory.RegisterActiveDish(dishId);
        }

        if (scenario == "closed-place") factory.RegisterClosedPlace(placeId);
        if (scenario != "unknown-place" && scenario != "closed-place") placeId = Guid.Empty;

        var request = new CreatePostRequest(
            Guid.NewGuid(),
            dishId,
            "Catalog-backed post",
            [new PostMediaRequest("https://cdn.example/a.jpg", IsPrimary: true)],
            PlaceId: placeId == Guid.Empty ? null : placeId);

        using var response = await client.PostAsJsonAsync("/api/posts", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task SearchAndDiscover_ReturnBoundedPublishedPosts()
    {
        await using var factory = new PostApiFactory(); using var client = factory.CreateTrustedClient();
        var dishId = Guid.NewGuid(); var first = await factory.CreatePostAsync(client, dishId: dishId, caption: "Bún bò spicy", tag: "bunbo"); _ = await factory.CreatePostAsync(client, caption: "Bánh mì", tag: "banhmi");
        using var search = await client.GetAsync($"/api/posts/search?dishId={dishId}&tag=bunbo&query=spicy&limit=1");
        search.StatusCode.Should().Be(HttpStatusCode.OK, await search.Content.ReadAsStringAsync());
        var searchBody = (await search.Content.ReadFromJsonAsync<PostSearchResponse>())!;
        searchBody.Items.Should().ContainSingle().Which.Id.Should().Be(first);
        using var feed = await client.GetAsync("/api/feed/discover?limit=10");
        var feedBody = (await feed.Content.ReadFromJsonAsync<DiscoverFeedResponse>())!;
        feedBody.Items.Should().HaveCount(2); feedBody.Items.Should().OnlyContain(item => !item.IsHidden);
    }

    [Fact]
    public async Task Likes_AreIdempotent()
    {
        await using var factory = new PostApiFactory(); using var client = factory.CreateTrustedClient(); var postId = await factory.CreatePostAsync(client); var actorId = Guid.NewGuid();
        _ = await client.PostAsJsonAsync($"/api/posts/{postId}/likes", new ActorRequest(actorId));
        using var second = await client.PostAsJsonAsync($"/api/posts/{postId}/likes", new ActorRequest(actorId));
        (await second.Content.ReadFromJsonAsync<PostLikeResponse>())!.LikeCount.Should().Be(1);
        _ = await client.DeleteAsync($"/api/posts/{postId}/likes?actorId={actorId}");
        using var secondDelete = await client.DeleteAsync($"/api/posts/{postId}/likes?actorId={actorId}");
        (await secondDelete.Content.ReadFromJsonAsync<PostLikeResponse>())!.LikeCount.Should().Be(0);
    }

    [Fact]
    public async Task Comments_CreateAndOwnerSoftDelete()
    {
        await using var factory = new PostApiFactory(); using var client = factory.CreateTrustedClient(); var postId = await factory.CreatePostAsync(client); var authorId = Guid.NewGuid();
        using var create = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", new CreatePostCommentRequest(authorId, "Looks good"));
        create.StatusCode.Should().Be(HttpStatusCode.Created); var comment = (await create.Content.ReadFromJsonAsync<PostCommentResponse>())!;
        using var delete = await client.DeleteAsync($"/api/comments/{comment.Id}?actorId={authorId}");
        (await delete.Content.ReadFromJsonAsync<PostCommentResponse>())!.Status.Should().Be("Deleted");
    }

    [Fact]
    public async Task RepostAndNotifications_RecordOwnerVisibleActivity()
    {
        await using var factory = new PostApiFactory();
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var actorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        using var ownerClient = factory.CreateTrustedClient(ownerId);
        var postId = await factory.CreatePostAsync(ownerClient);
        using var actorClient = factory.CreateTrustedClient(actorId);

        using var repostResponse = await actorClient.PostAsJsonAsync($"/api/posts/{postId}/repost", new CreatePostRepostRequest(Guid.NewGuid(), "Try this place too."));
        repostResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        (await repostResponse.Content.ReadFromJsonAsync<PostRepostResponse>())!.ActorId.Should().Be(actorId);
        using var likeResponse = await actorClient.PostAsJsonAsync($"/api/posts/{postId}/likes", new ActorRequest(Guid.NewGuid()));
        likeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var commentResponse = await actorClient.PostAsJsonAsync($"/api/posts/{postId}/comments", new CreatePostCommentRequest(Guid.NewGuid(), "Looks great."));
        commentResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var notificationsResponse = await ownerClient.GetAsync("/api/notifications?limit=10");
        notificationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = (await notificationsResponse.Content.ReadFromJsonAsync<NotificationListResponse>())!;
        notifications.UnreadCount.Should().Be(3);
        notifications.Items.Select(item => item.Type).Should().BeEquivalentTo(["PostReposted", "PostLiked", "PostCommented"]);

        var firstNotification = notifications.Items[0];
        using var readResponse = await ownerClient.PatchAsJsonAsync($"/api/notifications/{firstNotification.Id}/read", new { });
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await readResponse.Content.ReadFromJsonAsync<NotificationResponse>())!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task ReportResolveHide_StoresReportAndHidesPostWithoutAutoHideBeforeResolve()
    {
        await using var factory = new PostApiFactory(); using var client = factory.CreateTrustedClient(); var postId = await factory.CreatePostAsync(client);
        using var reportResponse = await client.PostAsJsonAsync($"/api/posts/{postId}/report", new CreateContentReportRequest(Guid.NewGuid(), "spam", "seeded"));
        reportResponse.StatusCode.Should().Be(HttpStatusCode.Created); var report = (await reportResponse.Content.ReadFromJsonAsync<ContentReportResponse>())!;
        (await client.GetFromJsonAsync<PostDetailResponse>($"/api/posts/{postId}"))!.IsHidden.Should().BeFalse();
        using var moderatorClient = factory.CreateTrustedClient(moderator: true);
        using var resolve = await moderatorClient.PostAsJsonAsync($"/api/admin/reports/{report.Id}/resolve", new ResolveContentReportRequest(Guid.NewGuid(), "hide", "manual review confirmed"));
        resolve.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetFromJsonAsync<PostDetailResponse>($"/api/posts/{postId}"))!.IsHidden.Should().BeTrue();
    }

    [Fact]
    public async Task PersistenceRoundtrip_SurvivesNewScope()
    {
        await using var factory = new PostApiFactory(); using var client = factory.CreateTrustedClient(); var postId = await factory.CreatePostAsync(client);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PostDbContext>();
        (await dbContext.Posts.FindAsync(postId)).Should().NotBeNull();
    }
}
