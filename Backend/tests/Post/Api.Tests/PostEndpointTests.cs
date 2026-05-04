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
