using System.Net;
using System.Net.Http.Json;
using FoodSocial.Contracts.Identity;
using FluentAssertions;
using Xunit;

namespace FoodSocial.AppHost.Tests;

[Collection(AppHostIntegrationTestCollection.Name)]
public sealed class UserProfileBootstrapIntegrationTests(AppHostFixture fixture)
{
    private const string UserProvisionedDeadLetterQueueName = "userprofile.bootstrap.user-provisioned.dead-letter.v2";
    private const string BootstrapCompletedDeadLetterQueueName = "identity.bootstrap.completed.dead-letter.v2";
    private static readonly string[] BootstrapQueueNames =
    [
        "userprofile.bootstrap.user-provisioned.v2",
        "userprofile.bootstrap.user-provisioned.retry.v2",
        UserProvisionedDeadLetterQueueName,
        "identity.bootstrap.completed.v2",
        "identity.bootstrap.completed.retry.v2",
        BootstrapCompletedDeadLetterQueueName,
    ];

    [Fact]
    public async Task UserProfileBootstrap_EnsureIdentityRemainsPendingUntilDownstreamSuccess()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await ResetBootstrapArtifactsAsync(cts.Token);

        using var bffClient = await fixture.Auth.CreateAuthenticatedBffBearerClientAsync(cts.Token);
        using var response = await bffClient.GetAsync("/api/auth/session", cts.Token);
        var payload = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(cts.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.User.Subject.Should().NotBeNullOrWhiteSpace();
        payload.Identity.State.Should().Be("PendingBootstrap");
        payload.Identity.DownstreamBootstrapPending.Should().BeTrue();

        var userProfileShellCount = await GetUserProfileShellCountAsync(payload.Identity.IdentityId, cts.Token);

        userProfileShellCount.Should().Be(0);
    }

    [Fact]
    public async Task UserProfileBootstrap_FirstEventCreatesProfileAndActivatesIdentity()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        var ensureResponse = await EnsureIdentityAsync(cts.Token);

        await WaitForAsync(
            async () => await GetUserProfileShellCountAsync(ensureResponse.Identity.IdentityId, cts.Token),
            count => count == 1,
            cts.Token);

        await WaitForAsync(
            async () => await GetIdentityStateAsync(ensureResponse.User.Subject, cts.Token),
            state => string.Equals(state, "Active", StringComparison.Ordinal),
            cts.Token);

        var shellCount = await GetUserProfileShellCountAsync(ensureResponse.Identity.IdentityId, cts.Token);
        var finalState = await GetIdentityStateAsync(ensureResponse.User.Subject, cts.Token);

        shellCount.Should().Be(1);
        finalState.Should().Be("Active");
    }

    [Fact]
    public async Task UserProfileBootstrap_DuplicateEventIgnored()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        var ensureResponse = await EnsureIdentityAsync(cts.Token);

        await WaitForAsync(
            async () => await GetUserProfileShellCountAsync(ensureResponse.Identity.IdentityId, cts.Token),
            count => count == 1,
            cts.Token);

        var replayEvent = new UserProvisionedIntegrationEvent(
            EventId: Guid.NewGuid(),
            IdentityId: ensureResponse.Identity.IdentityId,
            Subject: ensureResponse.User.Subject,
            PreferredUsername: ensureResponse.User.PreferredUsername,
            Email: ensureResponse.User.Email,
            Roles: ensureResponse.User.Roles,
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

        var shellCount = await GetUserProfileShellCountAsync(ensureResponse.Identity.IdentityId, cts.Token);
        var finalState = await GetIdentityStateAsync(ensureResponse.User.Subject, cts.Token);

        shellCount.Should().Be(1);
        finalState.Should().Be("Active");
    }

    [Fact]
    public async Task UserProfileBootstrap_PoisonProvisionedMessage_DeadLettersAndDoesNotStarveLaterValidMessage()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await ResetBootstrapArtifactsAsync(cts.Token);

        await using (var rabbitMq = new AppHostRabbitMqHelper(fixture.RabbitMqConnectionString))
        {
            await rabbitMq.PublishUserProvisionedAsync(
                new UserProvisionedIntegrationEvent(
                    EventId: Guid.NewGuid(),
                    IdentityId: Guid.NewGuid(),
                    Subject: string.Empty,
                    PreferredUsername: null,
                    Email: null,
                    Roles: [],
                    LifecycleState: "PendingBootstrap",
                    DownstreamBootstrapPending: true,
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    EventVersion: 1,
                    CorrelationId: "poison-provisioned",
                    CausationId: null),
                cts.Token);
        }

        var ensureResponse = await EnsureIdentityAsync(cts.Token);

        await WaitForAsync(
            async () => await GetUserProfileShellCountAsync(ensureResponse.Identity.IdentityId, cts.Token),
            count => count == 1,
            cts.Token);

        await WaitForAsync(
            async () => await GetIdentityStateAsync(ensureResponse.User.Subject, cts.Token),
            state => string.Equals(state, "Active", StringComparison.Ordinal),
            cts.Token);

        await using (var rabbitMq = new AppHostRabbitMqHelper(fixture.RabbitMqConnectionString))
        {
            await WaitForAsync(
                async () => await rabbitMq.GetMessageCountAsync(
                    UserProvisionedDeadLetterQueueName,
                    cts.Token),
                count => count >= 1,
                cts.Token);

            var deadLetterMessage = await rabbitMq.ReadSingleMessageAsync<UserProvisionedIntegrationEvent>(
                UserProvisionedDeadLetterQueueName,
                cts.Token);

            deadLetterMessage.Should().NotBeNull();
            deadLetterMessage!.CorrelationId.Should().Be("poison-provisioned");
        }
    }

    [Fact]
    public async Task UserProfileBootstrap_PoisonCompletionMessage_DeadLettersAndDoesNotStarveLaterValidMessage()
    {
        using var cts = fixture.CreateDefaultCancellationTokenSource();
        await ResetBootstrapArtifactsAsync(cts.Token);

        var identityId = Guid.NewGuid();
        var validSubject = "valid-completion-subject";

        _ = await fixture.Databases.Identity.ExecuteNonQueryAsync(
            "INSERT INTO \"identity_users\" (\"Id\", \"Subject\", \"PreferredUsername\", \"Email\", \"State\", \"CreatedAtUtc\") VALUES (@id, @subject, @preferredUsername, @email, @state, @createdAtUtc);",
            new Dictionary<string, object?>
            {
                ["id"] = identityId,
                ["subject"] = validSubject,
                ["preferredUsername"] = "completion-user",
                ["email"] = "completion@example.com",
                ["state"] = "PendingBootstrap",
                ["createdAtUtc"] = DateTimeOffset.UtcNow
            },
            cts.Token);

        await using (var rabbitMq = new AppHostRabbitMqHelper(fixture.RabbitMqConnectionString))
        {
            await rabbitMq.PublishUserProfileBootstrapCompletedAsync(
                new UserProfileBootstrapCompletedIntegrationEvent(
                    EventId: Guid.NewGuid(),
                    IdentityId: identityId,
                    Subject: "wrong-subject",
                    ProfileShellId: Guid.NewGuid(),
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    EventVersion: 1,
                    CorrelationId: "poison-completion",
                    CausationId: null),
                cts.Token);

            await rabbitMq.PublishUserProfileBootstrapCompletedAsync(
                new UserProfileBootstrapCompletedIntegrationEvent(
                    EventId: Guid.NewGuid(),
                    IdentityId: identityId,
                    Subject: validSubject,
                    ProfileShellId: Guid.NewGuid(),
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    EventVersion: 1,
                    CorrelationId: "valid-completion",
                    CausationId: null),
                cts.Token);
        }

        await WaitForAsync(
            async () => await GetIdentityStateAsync(validSubject, cts.Token),
            state => string.Equals(state, "Active", StringComparison.Ordinal),
            cts.Token);

        await using (var rabbitMq = new AppHostRabbitMqHelper(fixture.RabbitMqConnectionString))
        {
            await WaitForAsync(
                async () => await rabbitMq.GetMessageCountAsync(
                    BootstrapCompletedDeadLetterQueueName,
                    cts.Token),
                count => count >= 1,
                cts.Token);

            var deadLetterMessage = await rabbitMq.ReadSingleMessageAsync<UserProfileBootstrapCompletedIntegrationEvent>(
                BootstrapCompletedDeadLetterQueueName,
                cts.Token);

            deadLetterMessage.Should().NotBeNull();
            deadLetterMessage!.CorrelationId.Should().Be("poison-completion");
        }
    }

    private async Task<CurrentUserResponse> EnsureIdentityAsync(CancellationToken cancellationToken)
    {
        await ResetBootstrapArtifactsAsync(cancellationToken);

        using var bffClient = await fixture.Auth.CreateAuthenticatedBffBearerClientAsync(cancellationToken);
        using var response = await bffClient.GetAsync("/api/auth/session", cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.User.Subject.Should().NotBeNullOrWhiteSpace();
        payload.Identity.State.Should().Be("PendingBootstrap");
        payload.Identity.DownstreamBootstrapPending.Should().BeTrue();
        return payload;
    }

    private async Task ResetBootstrapArtifactsAsync(CancellationToken cancellationToken)
    {
        await using (var rabbitMq = new AppHostRabbitMqHelper(fixture.RabbitMqConnectionString))
        {
            await rabbitMq.PurgeQueuesIfPresentAsync(BootstrapQueueNames, cancellationToken);
        }

        if (await fixture.Databases.UserProfile.TableExistsAsync("user_profile_shells", cancellationToken: cancellationToken))
        {
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

    private async Task<string?> GetIdentityStateAsync(string subject, CancellationToken cancellationToken) =>
        await fixture.Databases.Identity.ExecuteScalarAsync<string>(
            "SELECT \"State\" FROM \"identity_users\" WHERE \"Subject\" = @subject LIMIT 1;",
            new Dictionary<string, object?>
            {
                ["subject"] = subject
            },
            cancellationToken);

    private static async Task WaitForAsync<T>(
        Func<Task<T>> action,
        Func<T, bool> predicate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(predicate);

        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
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
        throw new Xunit.Sdk.XunitException($"Condition was not satisfied before timeout. Last observed value: {finalValue}");
    }
}
