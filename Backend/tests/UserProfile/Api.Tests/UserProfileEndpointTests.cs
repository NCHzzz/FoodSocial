using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace FoodSocial.UserProfile.Api.Tests;

public sealed class UserProfileEndpointTests
{
    [Fact]
    public async Task GetProfile_ReturnsPublicProfileWithCounts()
    {
        await using var factory = new UserProfileApiFactory();
        var targetIdentityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var followerIdentityId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var targetShellId = await factory.SeedProfileAsync(targetIdentityId, "target-subject", displayName: "Target User", bio: "Food explorer", preferredCuisines: ["pho"]);
        var followerShellId = await factory.SeedProfileAsync(followerIdentityId, "follower-subject", displayName: "Follower User");
        await factory.SeedFollowAsync(followerShellId, targetShellId);

        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/users/{targetIdentityId}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var payload = JsonNode.Parse(body) as JsonObject;
        payload.Should().NotBeNull();
        payload!["identityId"]!.GetValue<string>().Should().Be(targetIdentityId.ToString());
        payload["displayName"]!.GetValue<string>().Should().Be("Target User");
        payload["bio"]!.GetValue<string>().Should().Be("Food explorer");
        payload["preferredCuisines"]!.AsArray().Select(node => node!.GetValue<string>()).Should().ContainSingle().Which.Should().Be("pho");
        payload["followerCount"]!.GetValue<long>().Should().Be(1);
        payload["followingCount"]!.GetValue<long>().Should().Be(0);
    }

    [Fact]
    public async Task PatchProfile_OwnerUpdatesProfileFields()
    {
        await using var factory = new UserProfileApiFactory();
        var identityId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        _ = await factory.SeedProfileAsync(identityId, "owner-subject", displayName: "Original Name");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", "owner-subject");
        client.DefaultRequestHeaders.Add("X-Test-PreferredUsername", "owner-user");
        client.DefaultRequestHeaders.Add("X-Test-Email", "owner@example.com");

        using var response = await client.PatchAsJsonAsync(
            $"/api/users/{identityId}",
            new
            {
                displayName = "Updated Name",
                bio = "Updated bio",
                avatarUrl = "https://cdn.example.com/avatar.png",
                preferredCuisines = new[] { "bun-bo", "com-tam" },
                dislikedIngredients = new[] { "cilantro" },
                dietaryRestrictions = new[] { "halal" }
            });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var payload = JsonNode.Parse(body) as JsonObject;
        payload.Should().NotBeNull();
        payload!["displayName"]!.GetValue<string>().Should().Be("Updated Name");
        payload["bio"]!.GetValue<string>().Should().Be("Updated bio");
        payload["avatarUrl"]!.GetValue<string>().Should().Be("https://cdn.example.com/avatar.png");
    }

    [Fact]
    public async Task PatchProfile_NonOwnerIsForbidden()
    {
        await using var factory = new UserProfileApiFactory();
        var identityId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        _ = await factory.SeedProfileAsync(identityId, "actual-owner-subject", displayName: "Protected User");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", "different-subject");

        using var response = await client.PatchAsJsonAsync(
            $"/api/users/{identityId}",
            new
            {
                displayName = "Tampered Name"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task FollowAndFollowingEndpoints_CreateAndReturnRelationship()
    {
        await using var factory = new UserProfileApiFactory();
        var ownerIdentityId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var targetIdentityId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        _ = await factory.SeedProfileAsync(ownerIdentityId, "owner-subject", displayName: "Owner User");
        _ = await factory.SeedProfileAsync(targetIdentityId, "target-subject", displayName: "Target User");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", "owner-subject");

        using var followResponse = await client.PostAsync($"/api/users/{targetIdentityId}/follow", content: null);
        var followBody = await followResponse.Content.ReadAsStringAsync();
        followResponse.StatusCode.Should().Be(HttpStatusCode.OK, followBody);

        using var listResponse = await client.GetAsync($"/api/users/{ownerIdentityId}/following");
        var listBody = await listResponse.Content.ReadAsStringAsync();
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK, listBody);

        var payload = JsonNode.Parse(listBody) as JsonObject;
        payload.Should().NotBeNull();
        payload!["items"]!.AsArray().Should().ContainSingle();
        payload["items"]![0]!["identityId"]!.GetValue<string>().Should().Be(targetIdentityId.ToString());
    }

    [Fact]
    public async Task FollowEndpoint_SelfFollowIsRejected()
    {
        await using var factory = new UserProfileApiFactory();
        var ownerIdentityId = Guid.Parse("12121212-1212-1212-1212-121212121212");
        _ = await factory.SeedProfileAsync(ownerIdentityId, "owner-subject", displayName: "Owner User");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", "owner-subject");

        using var response = await client.PostAsync($"/api/users/{ownerIdentityId}/follow", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task NotificationsEndpoints_ReturnFollowActivityForOwnerOnly()
    {
        await using var factory = new UserProfileApiFactory();
        var followerIdentityId = Guid.Parse("18181818-1818-1818-1818-181818181818");
        var targetIdentityId = Guid.Parse("19191919-1919-1919-1919-191919191919");
        _ = await factory.SeedProfileAsync(followerIdentityId, "follower-subject", displayName: "Follower User");
        _ = await factory.SeedProfileAsync(targetIdentityId, "target-subject", displayName: "Target User");

        using var followerClient = factory.CreateClient();
        followerClient.DefaultRequestHeaders.Add("X-Test-Sub", "follower-subject");
        using var followResponse = await followerClient.PostAsync($"/api/users/{targetIdentityId}/follow", content: null);
        followResponse.StatusCode.Should().Be(HttpStatusCode.OK, await followResponse.Content.ReadAsStringAsync());

        using var targetClient = factory.CreateClient();
        targetClient.DefaultRequestHeaders.Add("X-Test-Sub", "target-subject");
        using var notificationsResponse = await targetClient.GetAsync($"/api/users/{targetIdentityId}/notifications");
        var notificationsBody = await notificationsResponse.Content.ReadAsStringAsync();
        notificationsResponse.StatusCode.Should().Be(HttpStatusCode.OK, notificationsBody);

        var payload = JsonNode.Parse(notificationsBody) as JsonObject;
        payload.Should().NotBeNull();
        payload!["unreadCount"]!.GetValue<int>().Should().Be(1);
        var notification = payload["items"]!.AsArray().Should().ContainSingle().Subject!;
        notification["type"]!.GetValue<string>().Should().Be("UserFollowed");
        notification["actorIdentityId"]!.GetValue<string>().Should().Be(followerIdentityId.ToString());
        notification["isRead"]!.GetValue<bool>().Should().BeFalse();

        var notificationId = notification["id"]!.GetValue<string>();
        using var readResponse = await targetClient.PatchAsync($"/api/users/{targetIdentityId}/notifications/{notificationId}/read", content: null);
        var readBody = await readResponse.Content.ReadAsStringAsync();
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK, readBody);
        (JsonNode.Parse(readBody) as JsonObject)!["isRead"]!.GetValue<bool>().Should().BeTrue();

        using var forbiddenResponse = await followerClient.GetAsync($"/api/users/{targetIdentityId}/notifications");
        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SavedItemsEndpoints_AreOwnerScoped()
    {
        await using var factory = new UserProfileApiFactory();
        var ownerIdentityId = Guid.Parse("13131313-1313-1313-1313-131313131313");
        var strangerIdentityId = Guid.Parse("14141414-1414-1414-1414-141414141414");
        var ownerShellId = await factory.SeedProfileAsync(ownerIdentityId, "owner-subject", displayName: "Owner User");
        _ = await factory.SeedProfileAsync(strangerIdentityId, "stranger-subject", displayName: "Stranger User");
        var postId = Guid.Parse("15151515-1515-1515-1515-151515151515");
        var dishId = Guid.Parse("16161616-1616-1616-1616-161616161616");
        var placeId = Guid.Parse("17171717-1717-1717-1717-171717171717");
        await factory.SeedSavedPostAsync(ownerShellId, postId);
        await factory.SeedSavedDishAsync(ownerShellId, dishId);
        await factory.SeedSavedPlaceAsync(ownerShellId, placeId);

        using var ownerClient = factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Add("X-Test-Sub", "owner-subject");
        using var ownerResponse = await ownerClient.GetAsync($"/api/users/{ownerIdentityId}/saved");
        var ownerBody = await ownerResponse.Content.ReadAsStringAsync();
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.OK, ownerBody);

        var ownerPayload = JsonNode.Parse(ownerBody) as JsonObject;
        ownerPayload.Should().NotBeNull();
        ownerPayload!["posts"]!.AsArray().Should().ContainSingle();
        ownerPayload["dishes"]!.AsArray().Should().ContainSingle();
        ownerPayload["places"]!.AsArray().Should().ContainSingle();

        using var strangerClient = factory.CreateClient();
        strangerClient.DefaultRequestHeaders.Add("X-Test-Sub", "stranger-subject");
        using var strangerResponse = await strangerClient.GetAsync($"/api/users/{ownerIdentityId}/saved");
        strangerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
