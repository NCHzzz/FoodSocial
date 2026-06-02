using FluentAssertions;
using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Application;
using FoodSocial.Identity.Domain;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FoodSocial.Identity.Application.Tests;

public sealed class EnsureIdentityCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesPendingBootstrapIdentity_WhenSubjectDoesNotExist()
    {
        var store = new InMemoryIdentityUserStore();
        var handler = new EnsureIdentityCommandHandler(store, NoOpLogger.Instance);
        var command = new EnsureIdentityCommand("subject-123", "demo-user", "demo@example.com");

        var response = await handler.HandleAsync(command, CancellationToken.None);

        response.IdentityId.Should().NotBe(Guid.Empty);
        response.Subject.Should().Be("subject-123");
        response.PreferredUsername.Should().Be("demo-user");
        response.Email.Should().Be("demo@example.com");
        response.Created.Should().BeTrue();
        response.State.Should().Be(nameof(IdentityLifecycleState.PendingBootstrap));
        response.DownstreamBootstrapPending.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UpdatesCachedClaimsWithoutTrustingCallerSuppliedSubjectChanges()
    {
        var existingIdentity = new IdentityUser("subject-123", "old-user", "old@example.com");
        var store = new InMemoryIdentityUserStore(existingIdentity);
        var handler = new EnsureIdentityCommandHandler(store, NoOpLogger.Instance);
        var command = new EnsureIdentityCommand("subject-123", "new-user", "new@example.com");

        var response = await handler.HandleAsync(command, CancellationToken.None);

        response.Created.Should().BeFalse();
        response.Subject.Should().Be("subject-123");
        response.PreferredUsername.Should().Be("new-user");
        response.Email.Should().Be("new@example.com");
        existingIdentity.Subject.Should().Be("subject-123");
        existingIdentity.PreferredUsername.Should().Be("new-user");
        existingIdentity.Email.Should().Be("new@example.com");
    }

    [Fact]
    public async Task Handle_DoesNotNullOutExistingProfileFields_WhenClaimsAreMissing()
    {
        var existingIdentity = new IdentityUser("subject-123", "existing-user", "existing@example.com");
        var store = new InMemoryIdentityUserStore(existingIdentity);
        var handler = new EnsureIdentityCommandHandler(store, NoOpLogger.Instance);
        var command = new EnsureIdentityCommand("subject-123", null, null);

        var response = await handler.HandleAsync(command, CancellationToken.None);

        response.Created.Should().BeFalse();
        response.PreferredUsername.Should().Be("existing-user");
        response.Email.Should().Be("existing@example.com");
        existingIdentity.PreferredUsername.Should().Be("existing-user");
        existingIdentity.Email.Should().Be("existing@example.com");
    }

    [Fact]
    public async Task Handle_ReturnsExistingIdentity_WhenDuplicateCreateRaceOccurs()
    {
        var existingIdentity = new IdentityUser("subject-123", "existing-user", "existing@example.com");
        var store = new DuplicateOnFirstSaveIdentityUserStore(existingIdentity);
        var handler = new EnsureIdentityCommandHandler(store, NoOpLogger.Instance);
        var command = new EnsureIdentityCommand("subject-123", "updated-user", "updated@example.com");

        var response = await handler.HandleAsync(command, CancellationToken.None);

        response.Created.Should().BeFalse();
        response.IdentityId.Should().Be(existingIdentity.Id);
        response.Subject.Should().Be("subject-123");
        response.PreferredUsername.Should().Be("updated-user");
        response.Email.Should().Be("updated@example.com");
        existingIdentity.PreferredUsername.Should().Be("updated-user");
        existingIdentity.Email.Should().Be("updated@example.com");
        store.SaveAttempts.Should().Be(3);
    }

    [Fact]
    public async Task Handle_Outbox_WritesWithIdentity_WhenSubjectDoesNotExist()
    {
        var store = new InMemoryIdentityUserStore();
        var handler = new EnsureIdentityCommandHandler(store, NoOpLogger.Instance);
        var command = new EnsureIdentityCommand("outbox-subject", "demo-user", "demo@example.com");

        var response = await handler.HandleAsync(command, CancellationToken.None);

        response.Created.Should().BeTrue();
        store.OutboxMessages.Should().ContainSingle();

        var outbox = store.OutboxMessages.Single();
        outbox.Subject.Should().Be("outbox-subject");
        outbox.IdentityId.Should().Be(response.IdentityId);
        outbox.PreferredUsername.Should().Be("demo-user");
        outbox.Email.Should().Be("demo@example.com");
        outbox.LifecycleState.Should().Be(nameof(IdentityLifecycleState.PendingBootstrap));
        outbox.DownstreamBootstrapPending.Should().BeTrue();
        outbox.EventVersion.Should().Be(1);
        outbox.Roles.Should().ContainSingle().Which.Should().Be(IdentityAppRoles.User);
    }

    [Fact]
    public async Task Handle_Outbox_DoesNotStageDuplicatePendingMessage_WhenPendingBootstrapIdentityAlreadyHasPendingOutbox()
    {
        var existingIdentity = new IdentityUser("outbox-repeat-subject", "demo-user", "demo@example.com");
        var store = new InMemoryIdentityUserStore(existingIdentity);
        store.SeedPendingUserProvisionedOutbox("outbox-repeat-subject");
        var handler = new EnsureIdentityCommandHandler(store, NoOpLogger.Instance);

        await handler.HandleAsync(
            new EnsureIdentityCommand("outbox-repeat-subject", "demo-user", "demo@example.com"),
            CancellationToken.None);

        store.OutboxMessages.Should().BeEmpty();
    }

    private sealed class InMemoryIdentityUserStore(params IdentityUser[] users) : IIdentityUserStore
    {
        private readonly List<IdentityUser> _users = [.. users];
        private readonly HashSet<string> _subjectsWithPendingUserProvisionedOutbox = new(StringComparer.Ordinal);

        public List<UserProvisionedIntegrationEvent> OutboxMessages { get; } = [];

        public void SeedPendingUserProvisionedOutbox(string subject) =>
            _subjectsWithPendingUserProvisionedOutbox.Add(subject);

        public Task<IdentityUser?> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
        {
            var user = _users.SingleOrDefault(candidate => string.Equals(candidate.Subject, subject, StringComparison.Ordinal));
            return Task.FromResult(user);
        }

        public Task<IdentityUser?> FindByIdAsync(Guid identityId, CancellationToken cancellationToken)
        {
            var user = _users.SingleOrDefault(candidate => candidate.Id == identityId);
            return Task.FromResult(user);
        }

        public Task AddAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<string>> GetRoleNamesBySubjectAsync(string subject, CancellationToken cancellationToken)
        {
            var roles = _users.Any(candidate => string.Equals(candidate.Subject, subject, StringComparison.Ordinal))
                ? (IReadOnlyList<string>)[IdentityAppRoles.User]
                : Array.Empty<string>();

            return Task.FromResult(roles);
        }

        public Task<bool> EnsureRoleAssignedAsync(string subject, string role, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task AddOutboxMessageAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            OutboxMessages.Add(integrationEvent);
            return Task.CompletedTask;
        }

        public Task<bool> HasPendingUserProvisionedOutboxMessageAsync(string subject, CancellationToken cancellationToken) =>
            Task.FromResult(_subjectsWithPendingUserProvisionedOutbox.Contains(subject));

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(operation);
            return operation(cancellationToken);
        }
    }

    private sealed class DuplicateOnFirstSaveIdentityUserStore(IdentityUser existingIdentity) : IIdentityUserStore
    {
        private readonly IdentityUser _existingIdentity = existingIdentity;
        private IdentityUser? _pendingUser;
        private bool _initialLookupCompleted;

        public int SaveAttempts { get; private set; }

        public Task<IdentityUser?> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
        {
            if (!_initialLookupCompleted)
            {
                _initialLookupCompleted = true;
                return Task.FromResult<IdentityUser?>(null);
            }

            return Task.FromResult<IdentityUser?>(string.Equals(_existingIdentity.Subject, subject, StringComparison.Ordinal)
                ? _existingIdentity
                : null);
        }

        public Task<IdentityUser?> FindByIdAsync(Guid identityId, CancellationToken cancellationToken) =>
            Task.FromResult<IdentityUser?>(_existingIdentity.Id == identityId ? _existingIdentity : null);

        public Task AddAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            _pendingUser = user;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveAttempts++;

            if (SaveAttempts == 1)
            {
                _pendingUser = null;
                throw new DuplicateIdentitySubjectException(_existingIdentity.Subject, new InvalidOperationException("Duplicate subject."));
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> GetRoleNamesBySubjectAsync(string subject, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([IdentityAppRoles.User]);

        public Task<bool> EnsureRoleAssignedAsync(string subject, string role, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task AddOutboxMessageAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> HasPendingUserProvisionedOutboxMessageAsync(string subject, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(operation);
            return operation(cancellationToken);
        }
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
