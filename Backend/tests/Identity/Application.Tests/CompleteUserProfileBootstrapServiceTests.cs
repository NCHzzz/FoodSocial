using FluentAssertions;
using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Application.UseCases.Bootstrap;
using FoodSocial.Identity.Domain;
using Xunit;

namespace FoodSocial.Identity.Application.Tests;

public sealed class CompleteUserProfileBootstrapServiceTests
{
    [Fact]
    public async Task CompleteAsync_ActivatesPendingBootstrapIdentity_WhenSubjectMatches()
    {
        var identityUser = new IdentityUser("subject-123", "demo-user", "demo@example.com");
        var store = new BootstrapIdentityUserStore(identityUser);
        var service = new CompleteUserProfileBootstrapService(store);

        var changed = await service.CompleteAsync(CreateEvent(identityUser.Id, identityUser.Subject), CancellationToken.None);

        changed.Should().BeTrue();
        identityUser.State.Should().Be(IdentityLifecycleState.Active);
        store.SaveChangesCount.Should().Be(1);
    }

    [Fact]
    public async Task CompleteAsync_ReturnsFalse_WhenIdentityAlreadyActive()
    {
        var identityUser = new IdentityUser("subject-123", "demo-user", "demo@example.com");
        identityUser.TransitionTo(IdentityLifecycleTransition.Activate);
        var store = new BootstrapIdentityUserStore(identityUser);
        var service = new CompleteUserProfileBootstrapService(store);

        var changed = await service.CompleteAsync(CreateEvent(identityUser.Id, identityUser.Subject), CancellationToken.None);

        changed.Should().BeFalse();
        identityUser.State.Should().Be(IdentityLifecycleState.Active);
        store.SaveChangesCount.Should().Be(0);
    }

    [Fact]
    public async Task CompleteAsync_ReturnsFalse_WhenIdentityDoesNotExist()
    {
        var store = new BootstrapIdentityUserStore();
        var service = new CompleteUserProfileBootstrapService(store);

        var changed = await service.CompleteAsync(CreateEvent(Guid.NewGuid(), "subject-123"), CancellationToken.None);

        changed.Should().BeFalse();
        store.SaveChangesCount.Should().Be(0);
    }

    [Fact]
    public async Task CompleteAsync_Throws_WhenSubjectDoesNotMatchIdentity()
    {
        var identityUser = new IdentityUser("subject-123", "demo-user", "demo@example.com");
        var store = new BootstrapIdentityUserStore(identityUser);
        var service = new CompleteUserProfileBootstrapService(store);

        var act = () => service.CompleteAsync(CreateEvent(identityUser.Id, "other-subject"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Bootstrap completion subject 'other-subject' did not match identity '{identityUser.Id}'.");
        identityUser.State.Should().Be(IdentityLifecycleState.PendingBootstrap);
        store.SaveChangesCount.Should().Be(0);
    }

    private static UserProfileBootstrapCompletedIntegrationEvent CreateEvent(Guid identityId, string subject) =>
        new(
            EventId: Guid.NewGuid(),
            IdentityId: identityId,
            Subject: subject,
            ProfileShellId: Guid.NewGuid(),
            OccurredAtUtc: DateTimeOffset.UtcNow,
            EventVersion: 1,
            CorrelationId: null,
            CausationId: null);

    private sealed class BootstrapIdentityUserStore(params IdentityUser[] users) : IIdentityUserStore
    {
        private readonly List<IdentityUser> _users = [.. users];

        public int SaveChangesCount { get; private set; }

        public Task<IdentityUser?> FindByIdAsync(Guid identityId, CancellationToken cancellationToken) =>
            Task.FromResult(_users.SingleOrDefault(user => user.Id == identityId));

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken) =>
            operation(cancellationToken);

        public Task<IdentityUser?> FindBySubjectAsync(string subject, CancellationToken cancellationToken) =>
            Task.FromResult(_users.SingleOrDefault(user => string.Equals(user.Subject, subject, StringComparison.Ordinal)));

        public Task AddAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> GetRoleNamesBySubjectAsync(string subject, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([IdentityAppRoles.User]);

        public Task<bool> EnsureRoleAssignedAsync(string subject, string role, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task AddOutboxMessageAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> HasPendingUserProvisionedOutboxMessageAsync(string subject, CancellationToken cancellationToken) => Task.FromResult(false);
    }
}
