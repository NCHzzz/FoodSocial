using System.Net;
using System.Net.Http.Json;
using FoodSocial.Contracts.Identity;
using FluentAssertions;
using Xunit;

namespace FoodSocial.AppHost.Tests;

[Collection(AppHostIntegrationTestCollection.Name)]
public sealed class ProductionPathVerificationIntegrationTests(AppHostFixture fixture)
{
    [Fact]
    public async Task IdentityFlow_HappyPath_CompletesBootstrapAndOutboxDelivery()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await ResetBootstrapArtifactsAsync(cts.Token);

        var accessToken = await fixture.Auth.GetDemoUserAccessTokenAsync(cts.Token);
        accessToken.Should().NotBeNullOrWhiteSpace();

        using var bffClient = await fixture.Auth.CreateAuthenticatedBffBearerClientAsync(cts.Token);
        var sessionPayload = await GetSessionAsync(bffClient, cts.Token);

        await WaitForAsync(
            async () => await GetUserProfileShellCountAsync(sessionPayload.Identity.IdentityId, cts.Token),
            count => count == 1,
            cts.Token);

        await WaitForAsync(
            async () => await GetIdentityStateAsync(sessionPayload.User.Subject, cts.Token),
            state => string.Equals(state, "Active", StringComparison.Ordinal),
            cts.Token);

        await WaitForAsync(
            async () => await GetOutboxCountByStateForSubjectAsync(sessionPayload.User.Subject, "Delivered", cts.Token),
            deliveredCount => deliveredCount >= 1,
            cts.Token);

        var finalSession = await GetSessionAsync(bffClient, cts.Token);

        finalSession.Identity.State.Should().Be("Active");
        finalSession.Identity.BootstrapPending.Should().BeFalse();
        finalSession.Identity.DownstreamBootstrapPending.Should().BeFalse();
    }

    [Fact]
    public async Task IdentityFlow_RepeatedAuthAndDuplicateBootstrap_RemainExactlyOnce()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await ResetBootstrapArtifactsAsync(cts.Token);

        using var bffClient = await fixture.Auth.CreateAuthenticatedBffBearerClientAsync(cts.Token);

        CurrentUserResponse? lastSession = null;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            lastSession = await GetSessionAsync(bffClient, cts.Token);
            lastSession.User.Subject.Should().NotBeNullOrWhiteSpace();
            lastSession.Identity.IdentityId.Should().NotBe(Guid.Empty);
        }

        lastSession.Should().NotBeNull();

        await WaitForAsync(
            async () => await GetUserProfileShellCountBySubjectAsync(lastSession!.User.Subject, cts.Token),
            count => count == 1,
            cts.Token);

        await WaitForAsync(
            async () => await GetIdentityStateAsync(lastSession!.User.Subject, cts.Token),
            state => string.Equals(state, "Active", StringComparison.Ordinal),
            cts.Token);

        var replayEvent = new UserProvisionedIntegrationEvent(
            EventId: Guid.NewGuid(),
            IdentityId: lastSession!.Identity.IdentityId,
            Subject: lastSession.User.Subject,
            PreferredUsername: lastSession.User.PreferredUsername,
            Email: lastSession.User.Email,
            Roles: lastSession.User.Roles,
            LifecycleState: "PendingBootstrap",
            DownstreamBootstrapPending: true,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            EventVersion: 1,
            CorrelationId: null,
            CausationId: null);

        await using (var rabbitMq = new AppHostRabbitMqHelper(fixture.RabbitMqConnectionString))
        {
            await rabbitMq.PublishUserProvisionedAsync(replayEvent, cts.Token);
        }

        await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);

        var identityRowCount = await CountIdentityUsersBySubjectAsync(lastSession.User.Subject, cts.Token);
        var shellCount = await GetUserProfileShellCountBySubjectAsync(lastSession.User.Subject, cts.Token);

        identityRowCount.Should().Be(1);
        shellCount.Should().Be(1);
    }

    [Fact]
    public async Task IdentityFlow_RbacAndRefreshSessionRemainCorrectInIntegratedState()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await ResetBootstrapArtifactsAsync(cts.Token);

        using var bffClient = await fixture.Auth.CreateAuthenticatedBffBearerClientAsync(cts.Token);
        var sessionPayload = await GetSessionAsync(bffClient, cts.Token);
        sessionPayload.User.Subject.Should().NotBeNullOrWhiteSpace();

        await WaitForAsync(
            async () => await GetIdentityStateAsync(sessionPayload.User.Subject, cts.Token),
            state => string.Equals(state, "Active", StringComparison.Ordinal),
            cts.Token);

        using var identityClient = await fixture.Auth.CreateIdentityApiClientAsync(cts.Token);

        using (var moderationDenied = await identityClient.GetAsync("/identity/moderation/audit", cts.Token))
        {
            moderationDenied.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        using (var adminDenied = await identityClient.PostAsJsonAsync(
                   "/identity/admin/roles/assign",
                   new { subject = sessionPayload.User.Subject, role = "moderator" },
                   cts.Token))
        {
            adminDenied.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        using (var refreshResponse = await bffClient.PostAsJsonAsync("/api/auth/refresh", new { }, cts.Token))
        {
            var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<RefreshSessionResponse>(cts.Token);

            refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            refreshPayload.Should().NotBeNull();
            refreshPayload!.SessionValid.Should().BeTrue();
            refreshPayload.ReauthenticationRequired.Should().BeFalse();
            refreshPayload.Refreshed.Should().BeFalse();
        }

        var finalSession = await GetSessionAsync(bffClient, cts.Token);
        finalSession.User.Subject.Should().Be(sessionPayload.User.Subject);
        finalSession.Identity.State.Should().Be("Active");
    }

    [Fact]
    public async Task IdentityFlow_PublishFailureRetainsOutboxAndRecoversAfterRabbitMqReturns()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await ResetBootstrapArtifactsAsync(cts.Token);

        using var bffClient = await fixture.Auth.CreateAuthenticatedBffBearerClientAsync(cts.Token);
        var sessionPayload = await GetSessionAsync(bffClient, cts.Token);

        await WaitForAsync(
            async () => await GetOutboxTotalCountForSubjectAsync(sessionPayload.User.Subject, cts.Token),
            count => count >= 1,
            cts.Token);

        await ForceOutboxFailedRetryableAsync(sessionPayload.User.Subject, cts.Token);

        await fixture.Databases.UserProfile.DeleteRowsByColumnValueAsync(
            "user_profile_shells",
            "IdentityId",
            sessionPayload.Identity.IdentityId,
            cancellationToken: cts.Token);

        _ = await fixture.Databases.Identity.ExecuteNonQueryAsync(
            "UPDATE \"identity_users\" SET \"State\" = 'PendingBootstrap' WHERE \"Subject\" = @subject;",
            new Dictionary<string, object?>
            {
                ["subject"] = sessionPayload.User.Subject
            },
            cts.Token);

        var failedCount = await GetOutboxCountByStateForSubjectAsync(sessionPayload.User.Subject, "FailedRetryable", cts.Token);
        var pendingState = await GetIdentityStateAsync(sessionPayload.User.Subject, cts.Token);
        var pendingShellCount = await GetUserProfileShellCountAsync(sessionPayload.Identity.IdentityId, cts.Token);

        failedCount.Should().BeGreaterThanOrEqualTo(1);
        pendingState.Should().Be("PendingBootstrap");
        pendingShellCount.Should().Be(0);

        _ = await fixture.Databases.Identity.ExecuteNonQueryAsync(
            """
            UPDATE "identity_outbox_messages"
            SET "DeliveryState" = 'Pending',
                "LastError" = NULL,
                "DeliveredAtUtc" = NULL
            WHERE "Subject" = @subject;
            """,
            new Dictionary<string, object?>
            {
                ["subject"] = sessionPayload.User.Subject
            },
            cts.Token);

        await WaitForAsync(
            async () => await GetUserProfileShellCountAsync(sessionPayload.Identity.IdentityId, cts.Token),
            shellCount => shellCount == 1,
            cts.Token);

        await WaitForAsync(
            async () => await GetIdentityStateAsync(sessionPayload.User.Subject, cts.Token),
            state => string.Equals(state, "Active", StringComparison.Ordinal),
            cts.Token);

        await WaitForAsync(
            async () => await GetOutboxCountByStateForSubjectAsync(sessionPayload.User.Subject, "Delivered", cts.Token),
            deliveredCount => deliveredCount >= 1,
            cts.Token);
    }

    [Fact]
    public async Task IdentityFlow_UserProfileOutagePreservesAuthAndLeavesBootstrapPending()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await ResetBootstrapArtifactsAsync(cts.Token);

        await EnableUserProfileShellInsertOutageAsync(cts.Token);

        try
        {
        using var bffClient = await fixture.Auth.CreateAuthenticatedBffBearerClientAsync(cts.Token);
            var sessionPayload = await GetSessionAsync(bffClient, cts.Token);

            sessionPayload.Identity.State.Should().Be("PendingBootstrap");
            sessionPayload.Identity.BootstrapPending.Should().BeTrue();
            sessionPayload.Identity.DownstreamBootstrapPending.Should().BeTrue();

            await Task.Delay(TimeSpan.FromSeconds(6), cts.Token);

            var shellCount = await GetUserProfileShellCountAsync(sessionPayload.Identity.IdentityId, cts.Token);
            var identityState = await GetIdentityStateAsync(sessionPayload.User.Subject, cts.Token);

            shellCount.Should().Be(0);
            identityState.Should().Be("PendingBootstrap");

            using var refreshResponse = await bffClient.PostAsJsonAsync("/api/auth/refresh", new { }, cts.Token);
            refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await DisableUserProfileShellInsertOutageAsync(cts.Token);
        }
    }

    private async Task<CurrentUserResponse> GetSessionAsync(HttpClient bffClient, CancellationToken cancellationToken)
    {
        using var response = await bffClient.GetAsync("/api/auth/session", cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();

        return payload!;
    }

    private async Task ResetBootstrapArtifactsAsync(CancellationToken cancellationToken)
    {
        if (await fixture.Databases.UserProfile.TableExistsAsync("user_profile_shells", cancellationToken: cancellationToken))
        {
            await DropUserProfileShellInsertOutageConstraintAsync(cancellationToken);

            _ = await fixture.Databases.UserProfile.ExecuteNonQueryAsync(
                "DELETE FROM \"user_profile_shells\";",
                parameters: null,
                cancellationToken);
        }

        if (await fixture.Databases.Identity.TableExistsAsync("identity_outbox_messages", cancellationToken: cancellationToken))
        {
            _ = await fixture.Databases.Identity.ExecuteNonQueryAsync(
                "DELETE FROM \"identity_outbox_messages\";",
                parameters: null,
                cancellationToken);
        }

        if (await fixture.Databases.Identity.TableExistsAsync("identity_role_assignments", cancellationToken: cancellationToken))
        {
            _ = await fixture.Databases.Identity.ExecuteNonQueryAsync(
                "DELETE FROM \"identity_role_assignments\";",
                parameters: null,
                cancellationToken);
        }

        if (await fixture.Databases.Identity.TableExistsAsync("identity_users", cancellationToken: cancellationToken))
        {
            _ = await fixture.Databases.Identity.ExecuteNonQueryAsync(
                "DELETE FROM \"identity_users\";",
                parameters: null,
                cancellationToken);
        }
    }

    private async Task<long> GetUserProfileShellCountAsync(Guid identityId, CancellationToken cancellationToken)
    {
        if (!await fixture.Databases.UserProfile.TableExistsAsync("user_profile_shells", cancellationToken: cancellationToken))
        {
            return 0;
        }

        return await fixture.Databases.UserProfile.CountRowsByColumnValueAsync(
            "user_profile_shells",
            "IdentityId",
            identityId,
            cancellationToken: cancellationToken);
    }

    private async Task<long> CountIdentityUsersBySubjectAsync(string subject, CancellationToken cancellationToken) =>
        await fixture.Databases.Identity.CountRowsByColumnValueAsync(
            "identity_users",
            "Subject",
            subject,
            cancellationToken: cancellationToken);

    private async Task<long> GetUserProfileShellCountBySubjectAsync(string subject, CancellationToken cancellationToken) =>
        await fixture.Databases.UserProfile.CountRowsByColumnValueAsync(
            "user_profile_shells",
            "Subject",
            subject,
            cancellationToken: cancellationToken);

    private async Task<string?> GetIdentityStateAsync(string subject, CancellationToken cancellationToken) =>
        await fixture.Databases.Identity.ExecuteScalarAsync<string>(
            "SELECT \"State\" FROM \"identity_users\" WHERE \"Subject\" = @subject LIMIT 1;",
            new Dictionary<string, object?>
            {
                ["subject"] = subject
            },
            cancellationToken);

    private async Task<long> GetOutboxCountByStateForSubjectAsync(string subject, string deliveryState, CancellationToken cancellationToken) =>
        await fixture.Databases.Identity.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM \"identity_outbox_messages\" WHERE \"Subject\" = @subject AND \"DeliveryState\" = @deliveryState;",
            new Dictionary<string, object?>
            {
                ["subject"] = subject,
                ["deliveryState"] = deliveryState
            },
            cancellationToken);

    private async Task<long> GetOutboxTotalCountForSubjectAsync(string subject, CancellationToken cancellationToken) =>
        await fixture.Databases.Identity.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM \"identity_outbox_messages\" WHERE \"Subject\" = @subject;",
            new Dictionary<string, object?>
            {
                ["subject"] = subject
            },
            cancellationToken);

    private async Task ForceOutboxFailedRetryableAsync(string subject, CancellationToken cancellationToken)
    {
        _ = await fixture.Databases.Identity.ExecuteNonQueryAsync(
            """
            UPDATE "identity_outbox_messages"
            SET "DeliveryState" = 'FailedRetryable',
                "LastError" = 'Simulated publish failure for recovery verification.',
                "DeliveredAtUtc" = NULL,
                "LastAttemptAtUtc" = now(),
                "AttemptCount" = CASE WHEN "AttemptCount" < 1 THEN 1 ELSE "AttemptCount" END
            WHERE "Subject" = @subject;
            """,
            new Dictionary<string, object?>
            {
                ["subject"] = subject
            },
            cancellationToken);
    }

    private async Task EnsureRoleAssignmentAsync(string subject, string role, CancellationToken cancellationToken)
    {
        _ = await fixture.Databases.Identity.ExecuteNonQueryAsync(
            """
            INSERT INTO "identity_role_assignments" ("Id", "Subject", "Role", "AssignedAtUtc")
            VALUES (@id, @subject, @role, @assignedAtUtc)
            ON CONFLICT ("Subject", "Role") DO NOTHING;
            """,
            new Dictionary<string, object?>
            {
                ["id"] = Guid.NewGuid(),
                ["subject"] = subject,
                ["role"] = role,
                ["assignedAtUtc"] = DateTimeOffset.UtcNow
            },
            cancellationToken);
    }

    private async Task EnableUserProfileShellInsertOutageAsync(CancellationToken cancellationToken)
    {
        _ = await fixture.Databases.UserProfile.ExecuteNonQueryAsync(
            "ALTER TABLE \"user_profile_shells\" DROP CONSTRAINT IF EXISTS \"ck_user_profile_shells_outage_block\";",
            parameters: null,
            cancellationToken);

        _ = await fixture.Databases.UserProfile.ExecuteNonQueryAsync(
            "ALTER TABLE \"user_profile_shells\" ADD CONSTRAINT \"ck_user_profile_shells_outage_block\" CHECK (false) NOT VALID;",
            parameters: null,
            cancellationToken);
    }

    private async Task DisableUserProfileShellInsertOutageAsync(CancellationToken cancellationToken)
        => await DropUserProfileShellInsertOutageConstraintAsync(cancellationToken);

    private async Task DropUserProfileShellInsertOutageConstraintAsync(CancellationToken cancellationToken)
    {
        _ = await fixture.Databases.UserProfile.ExecuteNonQueryAsync(
            "ALTER TABLE \"user_profile_shells\" DROP CONSTRAINT IF EXISTS \"ck_user_profile_shells_outage_block\";",
            parameters: null,
            cancellationToken);
    }

    private static async Task WaitForAsync<T>(
        Func<Task<T>> action,
        Func<T, bool> predicate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(predicate);

        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var value = await action();
            if (predicate(value))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        var finalValue = await action();
        if (predicate(finalValue))
        {
            return;
        }

        throw new Xunit.Sdk.XunitException($"Condition was not satisfied before timeout. Last observed value: {finalValue}");
    }
}
