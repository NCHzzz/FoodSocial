using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace FoodSocial.Identity.Api.Tests;

public sealed class EnsureIdentityEndpointTests
{
    [Fact]
    public async Task AssignRole_UserWithoutAdminRole_IsDenied()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "standard-user-subject");

        using var response = await client.PostAsJsonAsync(
            "/identity/admin/roles/assign",
            new
            {
                subject = "target-subject",
                role = "moderator"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_AdminRole_IsAllowed()
    {
        await using var factory = new IdentityApiFactory();
        factory.SeedRolesForSubject("admin-subject", "admin");
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "admin-subject");

        using var response = await client.PostAsJsonAsync(
            "/identity/admin/roles/assign",
            new
            {
                subject = "target-subject",
                role = "moderator"
            });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task ModeratorAuditRoute_ModeratorRole_IsAllowed()
    {
        await using var factory = new IdentityApiFactory();
        factory.SeedRolesForSubject("moderator-subject", "moderator");
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "moderator-subject");

        using var response = await client.GetAsync("/identity/moderation/audit");

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task InternalTrustRoute_MissingTrustCredential_IsRejected()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "any-subject");

        using var response = await client.GetAsync("/identity/internal/service-trust-check");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CurrentUser_UsesIdentityOwnedRoleAssignments_NotTokenRealmRoles()
    {
        await using var factory = new IdentityApiFactory();
        factory.SeedRolesForSubject("identity-rbac-subject", "moderator");
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "identity-rbac-subject");
        client.DefaultRequestHeaders.Add("X-Test-Realm-Access", "{\"roles\":[\"admin\"]}");

        using var response = await client.GetAsync("/identity/current-user");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var payload = JsonNode.Parse(body) as JsonObject;
        payload.Should().NotBeNull();

        var roleValues = payload!["user"]!["roles"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
        roleValues.Should().ContainSingle().Which.Should().Be("moderator");
        roleValues.Should().NotContain("admin");
    }

    [Fact]
    public async Task CurrentUser_ReturnsTruthfulLocalIdentitySessionState()
    {
        await using var factory = new IdentityApiFactory();
        factory.SeedIdentity("active-user-subject", "active-user", "active@example.com", FoodSocial.Identity.Domain.IdentityLifecycleState.Active);
        factory.SeedRolesForSubject("active-user-subject", "moderator");
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "active-user-subject");
        client.DefaultRequestHeaders.Add("X-Test-PreferredUsername", "active-user");
        client.DefaultRequestHeaders.Add("X-Test-Email", "active@example.com");

        using var response = await client.GetAsync("/identity/current-user");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var payload = JsonNode.Parse(body) as JsonObject;
        payload.Should().NotBeNull();

        payload!["user"]!["subject"]!.GetValue<string>().Should().Be("active-user-subject");
        payload["user"]!["roles"]!.AsArray().Select(node => node!.GetValue<string>()).Should().Equal("moderator");

        var identityId = Guid.Parse(payload["identity"]!["identityId"]!.GetValue<string>());
        identityId.Should().NotBe(Guid.Empty);
        payload["identity"]!["state"]!.GetValue<string>().Should().Be("Active");
        payload["identity"]!["bootstrapPending"]!.GetValue<bool>().Should().BeFalse();
        payload["identity"]!["downstreamBootstrapPending"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task EnsureIdentity_ValidToken_DerivesSubjectAndBootstrapStatusFromClaims()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "keycloak-subject-123");
        client.DefaultRequestHeaders.Add("X-Test-PreferredUsername", "demo-user");
        client.DefaultRequestHeaders.Add("X-Test-Email", "demo@example.com");

        using var firstResponse = await client.PostAsJsonAsync("/identity/current-user/ensure", new { });

        var firstBody = await firstResponse.Content.ReadAsStringAsync();

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK, firstBody);

        var firstPayload = JsonNode.Parse(firstBody) as JsonObject;

        firstPayload.Should().NotBeNull();
        Guid.Parse(firstPayload!["identityId"]!.GetValue<string>()).Should().NotBe(Guid.Empty);
        firstPayload["subject"]!.GetValue<string>().Should().Be("keycloak-subject-123");
        firstPayload["preferredUsername"]!.GetValue<string>().Should().Be("demo-user");
        firstPayload["email"]!.GetValue<string>().Should().Be("demo@example.com");
        firstPayload["created"]!.GetValue<bool>().Should().BeTrue();
        firstPayload["state"]!.GetValue<string>().Should().Be("PendingBootstrap");
        firstPayload["downstreamBootstrapPending"]!.GetValue<bool>().Should().BeTrue();

        using var secondResponse = await client.PostAsJsonAsync("/identity/current-user/ensure", new { });

        var secondBody = await secondResponse.Content.ReadAsStringAsync();

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK, secondBody);

        var secondPayload = JsonNode.Parse(secondBody) as JsonObject;

        secondPayload.Should().NotBeNull();
        secondPayload!["identityId"]!.GetValue<string>().Should().Be(firstPayload["identityId"]!.GetValue<string>());
        secondPayload["created"]!.GetValue<bool>().Should().BeFalse();
        secondPayload["state"]!.GetValue<string>().Should().Be("PendingBootstrap");
        secondPayload["downstreamBootstrapPending"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task EnsureIdentity_MissingSubject_ReturnsUnauthorizedProblemDetails()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-PreferredUsername", "demo-user");
        client.DefaultRequestHeaders.Add("X-Test-Email", "demo@example.com");

        using var response = await client.PostAsJsonAsync("/identity/current-user/ensure", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var payload = await response.Content.ReadFromJsonAsync<JsonObject>();

        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Missing subject claim");
        payload["detail"]!.GetValue<string>().Should().Be("The authenticated token does not contain a subject claim.");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task EnsureIdentity_NameIdentifierFallback_DerivesSubjectWhenMappedClaimIsPresent()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-NameIdentifier", "mapped-subject-123");
        client.DefaultRequestHeaders.Add("X-Test-PreferredUsername", "demo-user");
        client.DefaultRequestHeaders.Add("X-Test-Email", "demo@example.com");

        using var response = await client.PostAsJsonAsync("/identity/current-user/ensure", new { });
        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);

        var payload = JsonNode.Parse(responseBody) as JsonObject;

        payload.Should().NotBeNull();
        payload!["subject"]!.GetValue<string>().Should().Be("mapped-subject-123");
        payload["created"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task EnsureIdentity_CallerProvidedSubjectAndProfileFields_AreIgnored()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "trusted-subject");
        client.DefaultRequestHeaders.Add("X-Test-PreferredUsername", "trusted-user");
        client.DefaultRequestHeaders.Add("X-Test-Email", "trusted@example.com");

        using var response = await client.PostAsJsonAsync(
            "/identity/current-user/ensure",
            new
            {
                subject = "tampered-subject",
                preferredUsername = "tampered-user",
                email = "tampered@example.com"
            });

        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);

        var payload = JsonNode.Parse(responseBody) as JsonObject;

        payload.Should().NotBeNull();
        payload!["subject"]!.GetValue<string>().Should().Be("trusted-subject");
        payload["preferredUsername"]!.GetValue<string>().Should().Be("trusted-user");
        payload["email"]!.GetValue<string>().Should().Be("trusted@example.com");
    }

    [Fact]
    public async Task EnsureIdentity_InternalRoute_BearerCallerWithoutTrustedServiceHeaders_IsForbidden()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "public-bearer-subject");
        client.DefaultRequestHeaders.Add("X-Test-PreferredUsername", "public-bearer-user");
        client.DefaultRequestHeaders.Add("X-Test-Email", "public-bearer@example.com");

        using var response = await client.PostAsJsonAsync(
            "/identity/current-user/internal/ensure",
            new
            {
                subject = "caller-supplied-subject",
                preferredUsername = "caller-supplied-user",
                email = "caller-supplied@example.com"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EnsureIdentity_InternalRoute_TrustedCaller_SucceedsForAuthenticatedUser()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "trusted-bff-subject");
        client.DefaultRequestHeaders.Add("X-Test-PreferredUsername", "trusted-bff-user");
        client.DefaultRequestHeaders.Add("X-Test-Email", "trusted-bff@example.com");
        client.DefaultRequestHeaders.Add("X-Test-Azp", "foodsocial-bff");
        client.DefaultRequestHeaders.Add("X-Internal-Service-Id", "foodsocial-bff");
        client.DefaultRequestHeaders.Add("X-Internal-Service-Secret", "foodsocial-bff-test-shared-secret");

        using var response = await client.PostAsJsonAsync("/identity/current-user/internal/ensure", new { });
        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);

        var payload = JsonNode.Parse(responseBody) as JsonObject;
        payload.Should().NotBeNull();
        payload!["subject"]!.GetValue<string>().Should().Be("trusted-bff-subject");
        payload["preferredUsername"]!.GetValue<string>().Should().Be("trusted-bff-user");
        payload["email"]!.GetValue<string>().Should().Be("trusted-bff@example.com");
    }

    [Fact]
    public async Task EnsureIdentity_InternalRoute_TrustedCaller_CannotTamperSubjectOrProfileClaims()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "authenticated-bff-subject");
        client.DefaultRequestHeaders.Add("X-Test-PreferredUsername", "authenticated-bff-user");
        client.DefaultRequestHeaders.Add("X-Test-Email", "authenticated-bff@example.com");
        client.DefaultRequestHeaders.Add("X-Test-Azp", "foodsocial-bff");
        client.DefaultRequestHeaders.Add("X-Internal-Service-Id", "foodsocial-bff");
        client.DefaultRequestHeaders.Add("X-Internal-Service-Secret", "foodsocial-bff-test-shared-secret");

        using var response = await client.PostAsJsonAsync(
            "/identity/current-user/internal/ensure",
            new
            {
                subject = "tampered-subject",
                preferredUsername = "tampered-user",
                email = "tampered@example.com"
            });

        var responseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);

        var payload = JsonNode.Parse(responseBody) as JsonObject;
        payload.Should().NotBeNull();
        payload!["subject"]!.GetValue<string>().Should().Be("authenticated-bff-subject");
        payload["preferredUsername"]!.GetValue<string>().Should().Be("authenticated-bff-user");
        payload["email"]!.GetValue<string>().Should().Be("authenticated-bff@example.com");
    }

    [Fact]
    public async Task EnsureIdentity_InternalRoute_TrustedHeadersWithoutMatchingAuthorizedParty_AreForbidden()
    {
        await using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "trusted-bff-subject");
        client.DefaultRequestHeaders.Add("X-Test-PreferredUsername", "trusted-bff-user");
        client.DefaultRequestHeaders.Add("X-Test-Email", "trusted-bff@example.com");
        client.DefaultRequestHeaders.Add("X-Test-Azp", "foodsocial-cli");
        client.DefaultRequestHeaders.Add("X-Internal-Service-Id", "foodsocial-bff");
        client.DefaultRequestHeaders.Add("X-Internal-Service-Secret", "foodsocial-bff-test-shared-secret");

        using var response = await client.PostAsJsonAsync("/identity/current-user/internal/ensure", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_BlankSubject_ReturnsBadRequestProblemDetails()
    {
        await using var factory = new IdentityApiFactory();
        factory.SeedRolesForSubject("admin-subject", "admin");
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "admin-subject");

        using var response = await client.PostAsJsonAsync(
            "/identity/admin/roles/assign",
            new
            {
                subject = "   ",
                role = "moderator"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var payload = await response.Content.ReadFromJsonAsync<JsonObject>();
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Invalid role assignment request");
        payload["detail"]!.GetValue<string>().Should().Be("The target subject is required.");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task AssignRole_UnsupportedRole_ReturnsBadRequestProblemDetails()
    {
        await using var factory = new IdentityApiFactory();
        factory.SeedRolesForSubject("admin-subject", "admin");
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Test-Sub", "admin-subject");

        using var response = await client.PostAsJsonAsync(
            "/identity/admin/roles/assign",
            new
            {
                subject = "target-subject",
                role = "super-admin"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var payload = await response.Content.ReadFromJsonAsync<JsonObject>();
        payload.Should().NotBeNull();
        payload!["title"]!.GetValue<string>().Should().Be("Invalid role assignment request");
        payload["detail"]!.GetValue<string>().Should().Be("The requested role is not supported.");
        payload["status"]!.GetValue<int>().Should().Be(StatusCodes.Status400BadRequest);
    }
}
