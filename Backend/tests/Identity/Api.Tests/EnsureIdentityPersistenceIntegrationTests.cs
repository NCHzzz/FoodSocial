using FluentAssertions;
using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Application;
using FoodSocial.Identity.Infrastructure.Messaging;
using FoodSocial.Identity.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FoodSocial.Identity.Api.Tests;

public sealed class EnsureIdentityPersistenceIntegrationTests
{
    [Fact]
    public async Task EnsureIdentity_RealSqliteStore_FirstEnsureCreatesOneRow()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var store = new IdentityUserStore(dbContext);
        var handler = new EnsureIdentityCommandHandler(store, NoOpLogger.Instance);

        var response = await handler.HandleAsync(
            new EnsureIdentityCommand("persistent-subject", "demo-user", "demo@example.com"),
            CancellationToken.None);

        response.Created.Should().BeTrue();
        response.State.Should().Be("PendingBootstrap");
        response.DownstreamBootstrapPending.Should().BeTrue();

        dbContext.ChangeTracker.Clear();

        var persistedIdentity = await dbContext.IdentityUsers.SingleAsync(user => user.Subject == "persistent-subject");

        persistedIdentity.PreferredUsername.Should().Be("demo-user");
        persistedIdentity.Email.Should().Be("demo@example.com");
        persistedIdentity.State.Should().Be(FoodSocial.Identity.Domain.IdentityLifecycleState.PendingBootstrap);

        var rowCount = await dbContext.IdentityUsers.LongCountAsync(user => user.Subject == "persistent-subject");
        rowCount.Should().Be(1);

        var roleAssignments = await dbContext.IdentityRoleAssignments
            .Where(assignment => assignment.Subject == "persistent-subject")
            .Select(assignment => assignment.Role)
            .ToArrayAsync();

        roleAssignments.Should().ContainSingle().Which.Should().Be("user");

        var outboxRecords = await dbContext.IdentityOutboxMessages
            .Where(message => message.Subject == "persistent-subject")
            .ToListAsync();

        outboxRecords.Should().ContainSingle();
        outboxRecords[0].DeliveryState.Should().Be(IdentityOutboxDeliveryState.Pending);
        outboxRecords[0].AttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task EnsureIdentity_RealSqliteStore_SecondEnsureReturnsCreatedFalseAndKeepsOneRow()
    {
        await using var connection = await OpenDatabaseAsync();

        await using (var setupContext = CreateDbContext(connection))
        {
            var setupStore = new IdentityUserStore(setupContext);
            var setupHandler = new EnsureIdentityCommandHandler(setupStore, NoOpLogger.Instance);

            await setupHandler.HandleAsync(
                new EnsureIdentityCommand("persistent-repeat-subject", "demo-user", "demo@example.com"),
                CancellationToken.None);
        }

        await using var dbContext = CreateDbContext(connection);
        var store = new IdentityUserStore(dbContext);
        var handler = new EnsureIdentityCommandHandler(store, NoOpLogger.Instance);

        var response = await handler.HandleAsync(
            new EnsureIdentityCommand("persistent-repeat-subject", "demo-user-2", "demo2@example.com"),
            CancellationToken.None);

        response.Created.Should().BeFalse();
        response.State.Should().Be("PendingBootstrap");
        response.PreferredUsername.Should().Be("demo-user-2");
        response.Email.Should().Be("demo2@example.com");

        dbContext.ChangeTracker.Clear();

        var persistedIdentity = await dbContext.IdentityUsers.SingleAsync(user => user.Subject == "persistent-repeat-subject");

        persistedIdentity.PreferredUsername.Should().Be("demo-user-2");
        persistedIdentity.Email.Should().Be("demo2@example.com");

        var rowCount = await dbContext.IdentityUsers.LongCountAsync(user => user.Subject == "persistent-repeat-subject");
        rowCount.Should().Be(1);

        var outboxCount = await dbContext.IdentityOutboxMessages
            .LongCountAsync(message => message.Subject == "persistent-repeat-subject");

        outboxCount.Should().Be(2);
    }

    [Fact]
    public async Task EnsureIdentity_RealSqliteStore_ConcurrentSameSubjectCreatesSingleRow()
    {
        await using var connection = await OpenDatabaseAsync();

        var firstTask = EnsureIdentityAsync(connection, "persistent-concurrency-subject", "demo-user", "demo@example.com");
        var secondTask = EnsureIdentityAsync(connection, "persistent-concurrency-subject", "demo-user", "demo@example.com");

        await Task.WhenAll(firstTask, secondTask);

        var firstResponse = await firstTask;
        var secondResponse = await secondTask;

        firstResponse.IdentityId.Should().Be(secondResponse.IdentityId);
        new[] { firstResponse.Created, secondResponse.Created }.Count(created => created).Should().Be(1);

        await using var verificationContext = CreateDbContext(connection);
        var rowCount = await verificationContext.IdentityUsers.LongCountAsync(user => user.Subject == "persistent-concurrency-subject");
        rowCount.Should().Be(1);

        var outboxCount = await verificationContext.IdentityOutboxMessages
            .LongCountAsync(message => message.Subject == "persistent-concurrency-subject");

        outboxCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Outbox_DrainSuccess_MarksMessageAsDelivered()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var store = new IdentityUserStore(dbContext);
        var handler = new EnsureIdentityCommandHandler(store, NoOpLogger.Instance);

        _ = await handler.HandleAsync(
            new EnsureIdentityCommand("drain-success-subject", "demo-user", "demo@example.com"),
            CancellationToken.None);

        var publisher = new CapturingIdentityIntegrationEventPublisher();
        var drainWorker = new IdentityOutboxDrainWorker(dbContext, publisher, NullLogger<IdentityOutboxDrainWorker>.Instance);
        var processedCount = await drainWorker.DrainOnceAsync(CancellationToken.None);

        processedCount.Should().Be(1);
        publisher.PublishedEvents.Should().ContainSingle();

        dbContext.ChangeTracker.Clear();

        var persisted = await dbContext.IdentityOutboxMessages
            .SingleAsync(message => message.Subject == "drain-success-subject");

        persisted.DeliveryState.Should().Be(IdentityOutboxDeliveryState.Delivered);
        persisted.AttemptCount.Should().Be(1);
        persisted.LastAttemptAtUtc.Should().NotBeNull();
        persisted.DeliveredAtUtc.Should().NotBeNull();
        persisted.LastError.Should().BeNull();
    }

    [Fact]
    public async Task Outbox_PublishFailureRetained_LeavesRetryableFailedState()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var store = new IdentityUserStore(dbContext);
        var handler = new EnsureIdentityCommandHandler(store, NoOpLogger.Instance);

        _ = await handler.HandleAsync(
            new EnsureIdentityCommand("drain-failure-subject", "demo-user", "demo@example.com"),
            CancellationToken.None);

        var publisher = new ThrowingIdentityIntegrationEventPublisher(new InvalidOperationException("Simulated publish failure."));
        var drainWorker = new IdentityOutboxDrainWorker(dbContext, publisher, NullLogger<IdentityOutboxDrainWorker>.Instance);
        var processedCount = await drainWorker.DrainOnceAsync(CancellationToken.None);

        processedCount.Should().Be(0);

        dbContext.ChangeTracker.Clear();

        var persisted = await dbContext.IdentityOutboxMessages
            .SingleAsync(message => message.Subject == "drain-failure-subject");

        persisted.DeliveryState.Should().Be(IdentityOutboxDeliveryState.FailedRetryable);
        persisted.AttemptCount.Should().Be(1);
        persisted.LastAttemptAtUtc.Should().NotBeNull();
        persisted.DeliveredAtUtc.Should().BeNull();
        persisted.LastError.Should().Contain("Simulated publish failure");
    }

    [Fact]
    public async Task Outbox_MissingRabbitMqConfigurationPublisher_RetainsRetryableFailure()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var store = new IdentityUserStore(dbContext);
        var handler = new EnsureIdentityCommandHandler(store, NoOpLogger.Instance);

        _ = await handler.HandleAsync(
            new EnsureIdentityCommand("missing-rabbitmq-subject", "demo-user", "demo@example.com"),
            CancellationToken.None);

        var publisher = new MissingRabbitMqConfigurationIdentityIntegrationEventPublisher();
        var drainWorker = new IdentityOutboxDrainWorker(dbContext, publisher, NullLogger<IdentityOutboxDrainWorker>.Instance);

        var processedCount = await drainWorker.DrainOnceAsync(CancellationToken.None);

        processedCount.Should().Be(0);

        dbContext.ChangeTracker.Clear();

        var persisted = await dbContext.IdentityOutboxMessages
            .SingleAsync(message => message.Subject == "missing-rabbitmq-subject");

        persisted.DeliveryState.Should().Be(IdentityOutboxDeliveryState.FailedRetryable);
        persisted.AttemptCount.Should().Be(1);
        persisted.DeliveredAtUtc.Should().BeNull();
        persisted.LastError.Should().Contain("RabbitMQ connection string is not configured");
    }

    private static async Task<EnsureIdentityResponse> EnsureIdentityAsync(
        SqliteConnection connection,
        string subject,
        string preferredUsername,
        string email)
    {
        await using var dbContext = CreateDbContext(connection);
        var store = new IdentityUserStore(dbContext);
        var handler = new EnsureIdentityCommandHandler(store, NoOpLogger.Instance);

        return await handler.HandleAsync(
            new EnsureIdentityCommand(subject, preferredUsername, email),
            CancellationToken.None);
    }

    private static IdentityDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(connection)
            .Options;

        return new IdentityDbContext(options);
    }

    private static async Task<SqliteConnection> OpenDatabaseAsync()
    {
        var connection = new SqliteConnection($"Data Source={Path.Combine(Path.GetTempPath(), $"identity-tests-{Guid.NewGuid():N}.db")}");
        await connection.OpenAsync();

        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.ChangeTracker.Clear();

        return connection;
    }

    private sealed class CapturingIdentityIntegrationEventPublisher : IIdentityIntegrationEventPublisher
    {
        public List<UserProvisionedIntegrationEvent> PublishedEvents { get; } = [];

        public Task PublishUserProvisionedAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            PublishedEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingIdentityIntegrationEventPublisher(Exception exception) : IIdentityIntegrationEventPublisher
    {
        private readonly Exception _exception = exception ?? throw new ArgumentNullException(nameof(exception));

        public Task PublishUserProvisionedAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
            Task.FromException(_exception);
    }

    private sealed class NoOpLogger : ILogger<EnsureIdentityCommandHandler>
    {
        public static NoOpLogger Instance { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}
