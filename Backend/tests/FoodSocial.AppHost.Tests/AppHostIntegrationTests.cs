using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace FoodSocial.AppHost.Tests;

[Collection(AppHostIntegrationTestCollection.Name)]
public sealed class AppHostIntegrationTests(AppHostFixture fixture)
{
    [Fact]
    public async Task AppHostBoot_ReadinessEndpointsBecomeHealthy()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();

        await fixture.WaitForHealthyAsync("bff", cts.Token);
        await fixture.WaitForHealthyAsync("api-gateway", cts.Token);
        await fixture.WaitForHealthyAsync("identity-api", cts.Token);

        var bffEndpoint = fixture.Resources.GetEndpoint("bff", "https");
        var apiGatewayEndpoint = fixture.Resources.GetEndpoint("api-gateway", "https");
        var identityEndpoint = fixture.Resources.GetEndpoint("identity-api", "https");

        bffEndpoint.Port.Should().BeGreaterThan(0);
        apiGatewayEndpoint.Port.Should().BeGreaterThan(0);
        identityEndpoint.Port.Should().BeGreaterThan(0);

        using var bffClient = fixture.CreateUnauthenticatedClient("bff", "https");
        using var apiGatewayClient = fixture.CreateUnauthenticatedClient("api-gateway", "https");
        using var identityClient = fixture.CreateUnauthenticatedClient("identity-api", "https");

        using var bffHealthResponse = await bffClient.GetAsync("/health", cts.Token);
        using var apiGatewayHealthResponse = await apiGatewayClient.GetAsync("/health", cts.Token);
        using var identityHealthResponse = await identityClient.GetAsync("/health", cts.Token);

        bffHealthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        apiGatewayHealthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        identityHealthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await fixture.Databases.Identity.CanConnectAsync(cts.Token)).Should().BeTrue();
        (await fixture.Databases.UserProfile.CanConnectAsync(cts.Token)).Should().BeTrue();
    }

    [Fact]
    public async Task TokenAcquisition_DemoUserAccessTokenAuthenticatesAgainstIdentityApi()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();

        var accessToken = await fixture.Auth.GetDemoUserAccessTokenAsync(cts.Token);
        var payload = JwtPayloadReader.Read(accessToken);
        using var identityClient = await fixture.Auth.CreateIdentityApiClientAsync(cts.Token);

        accessToken.Should().NotBeNullOrWhiteSpace();
        payload.Should().NotBeNull();

        var payloadJson = payload!.ToJsonString();

        payloadJson.Should().Contain("foodsocial-cli");
        payloadJson.Should().Contain("foodsocial-api");
        payload["sub"]?.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        HasAudienceClaim(payload, "foodsocial-api").Should().BeTrue();
        payload["azp"]!.GetValue<string>().Should().Be("foodsocial-cli");
        payload["scope"]!.GetValue<string>().Should().Contain("foodsocial-api");

        identityClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        identityClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        identityClient.DefaultRequestHeaders.Authorization.Parameter.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BffSession_AuthenticatedSessionEnsuresLocalIdentityAndReturnsBootstrapState()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await fixture.WaitForHealthyAsync("bff", cts.Token);

        using var bffClient = await fixture.Auth.CreateAuthenticatedBffBearerClientAsync(cts.Token);
        using var response = await bffClient.GetAsync("/api/auth/session", cts.Token);
        var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);

        var payload = JsonNode.Parse(responseBody) as JsonObject;
        var subject = payload?["user"]?["subject"]?.GetValue<string>();

        payload.Should().NotBeNull();
        subject.Should().NotBeNullOrWhiteSpace();
        payload["user"]!["roles"]!.AsArray().Should().NotBeEmpty();
        payload["identity"]!["identityId"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        payload["identity"]!["state"]!.GetValue<string>().Should().BeOneOf("PendingBootstrap", "Active");

        var sessionRoles = payload["user"]!["roles"]!.AsArray().Select(node => node!.GetValue<string>()).OrderBy(role => role, StringComparer.Ordinal).ToArray();
        var storedRolesCsv = await fixture.Databases.Identity.ExecuteScalarAsync<string>(
            "SELECT string_agg(\"Role\", ',' ORDER BY \"Role\") FROM \"identity_role_assignments\" WHERE \"Subject\" = @subject;",
            new Dictionary<string, object?>
            {
                ["subject"] = subject!
            },
            cts.Token);
        var storedRoles = string.IsNullOrWhiteSpace(storedRolesCsv)
            ? ["user"]
            : storedRolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        sessionRoles.Should().Equal(storedRoles.OrderBy(role => role, StringComparer.Ordinal));
        sessionRoles.Should().NotContain("account");
        sessionRoles.Should().NotContain("manage-account");
        sessionRoles.Should().NotContain("offline_access");
        sessionRoles.Should().NotContain("uma_authorization");

        var downstreamBootstrapPending = payload["identity"]!["downstreamBootstrapPending"]!.GetValue<bool>();
        var bootstrapPending = payload["identity"]!["bootstrapPending"]!.GetValue<bool>();
        var state = payload["identity"]!["state"]!.GetValue<string>();

        if (string.Equals(state, "PendingBootstrap", StringComparison.Ordinal))
        {
            downstreamBootstrapPending.Should().BeTrue();
            bootstrapPending.Should().BeTrue();
        }
        else
        {
            downstreamBootstrapPending.Should().BeFalse();
            bootstrapPending.Should().BeFalse();
        }
    }

    [Fact]
    public async Task BffRefresh_AuthenticatedCookieSessionReturnsDeterministicSessionRenewalContract()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await fixture.WaitForHealthyAsync("bff", cts.Token);

        using var bffClient = await fixture.Auth.CreateAuthenticatedBffBearerClientAsync(cts.Token);
        using var response = await bffClient.PostAsJsonAsync("/api/auth/refresh", new { }, cts.Token);
        var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);

        var payload = JsonNode.Parse(responseBody) as JsonObject;

        payload.Should().NotBeNull();
        payload!["refreshed"]!.GetValue<bool>().Should().BeFalse();
        payload["sessionValid"]!.GetValue<bool>().Should().BeTrue();
        payload["reauthenticationRequired"]!.GetValue<bool>().Should().BeFalse();
        payload["reason"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BffSession_PendingBootstrapStillReturnsUsableAuthenticatedResponse()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await fixture.WaitForHealthyAsync("bff", cts.Token);

        using var bffClient = await fixture.Auth.CreateAuthenticatedBffBearerClientAsync(cts.Token);
        using var firstResponse = await bffClient.GetAsync("/api/auth/session", cts.Token);
        var firstBody = await firstResponse.Content.ReadAsStringAsync(cts.Token);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK, firstBody);

        using var secondResponse = await bffClient.GetAsync("/api/auth/session", cts.Token);
        var secondBody = await secondResponse.Content.ReadAsStringAsync(cts.Token);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK, secondBody);

        var payload = JsonNode.Parse(secondBody) as JsonObject;

        payload.Should().NotBeNull();
        payload!["user"]!["subject"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        payload["identity"]!["state"]!.GetValue<string>().Should().BeOneOf("PendingBootstrap", "Active");

        var downstreamBootstrapPending = payload["identity"]!["downstreamBootstrapPending"]!.GetValue<bool>();
        var bootstrapPending = payload["identity"]!["bootstrapPending"]!.GetValue<bool>();
        var state = payload["identity"]!["state"]!.GetValue<string>();

        if (string.Equals(state, "PendingBootstrap", StringComparison.Ordinal))
        {
            bootstrapPending.Should().BeTrue();
            downstreamBootstrapPending.Should().BeTrue();
        }
        else
        {
            bootstrapPending.Should().BeFalse();
            downstreamBootstrapPending.Should().BeFalse();
        }
    }

    [Fact]
    public async Task TrustedEnsureTransport_UserBearerCannotCallInternalEnsureButBffSessionCan()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await fixture.WaitForHealthyAsync("bff", cts.Token);
        await fixture.WaitForHealthyAsync("identity-api", cts.Token);

        using var identityClient = await fixture.Auth.CreateIdentityApiClientAsync(cts.Token);

        using (var directInternalResponse = await identityClient.PostAsJsonAsync(
                   "/identity/current-user/internal/ensure",
                   new
                   {
                       subject = "tampered-internal-subject",
                       preferredUsername = "tampered-user",
                       email = "tampered@example.com"
                   },
                   cts.Token))
        {
            directInternalResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        using var bffClient = await fixture.Auth.CreateAuthenticatedBffBearerClientAsync(cts.Token);
        using var sessionResponse = await bffClient.GetAsync("/api/auth/session", cts.Token);
        var sessionBody = await sessionResponse.Content.ReadAsStringAsync(cts.Token);

        sessionResponse.StatusCode.Should().Be(HttpStatusCode.OK, sessionBody);

        var payload = JsonNode.Parse(sessionBody) as JsonObject;
        payload.Should().NotBeNull();
        var subject = payload!["user"]!["subject"]!.GetValue<string>();
        subject.Should().NotBeNullOrWhiteSpace();
        payload["identity"]!["identityId"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();

        var storedIdentityCount = await fixture.Databases.Identity.CountRowsByColumnValueAsync(
            "identity_users",
            "Subject",
            subject!,
            cancellationToken: cts.Token);

        storedIdentityCount.Should().Be(1);
    }

    [Fact]
    public async Task BffLogout_GetMethodIsNotAllowed_AndPostClearsBrowserSession()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await fixture.WaitForHealthyAsync("bff", cts.Token);

        using var unauthenticatedClient = fixture.CreateUnauthenticatedClient("bff", "https");
        using var logoutGetResponse = await unauthenticatedClient.GetAsync("/api/auth/logout", cts.Token);
        logoutGetResponse.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);

        using var browserClient = await fixture.Auth.CreateAuthenticatedBffBrowserClientAsync(cts.Token);
        await AppHostAuthenticationHelper.AddCsrfHeaderAsync(browserClient, cts.Token);
        using var logoutPostResponse = await browserClient.PostAsync(
            "/api/auth/logout",
            new FormUrlEncodedContent([]),
            cts.Token);

        logoutPostResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var sessionResponse = await browserClient.GetAsync("/api/auth/session", cts.Token);
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    private static bool HasAudienceClaim(JsonObject payload, string expectedAudience)
    {
        var audienceNode = payload["aud"];

        return audienceNode switch
        {
            JsonValue value => string.Equals(value.GetValue<string>(), expectedAudience, StringComparison.Ordinal),
            JsonArray array => array.Any(node => string.Equals(node?.GetValue<string>(), expectedAudience, StringComparison.Ordinal)),
            _ => false
        };
    }
}
