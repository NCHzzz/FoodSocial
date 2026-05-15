using FluentAssertions;
using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Domain;
using Xunit;

namespace FoodSocial.Identity.Application.Tests;

public sealed class GetCurrentUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsUnknownSession_WhenIdentityDoesNotExist()
    {
        var store = new QueryIdentityUserStore();
        var handler = new GetCurrentUserQueryHandler(store);

        var response = await handler.HandleAsync(
            new CurrentUserQuery("subject-123", "token-user", "token@example.com"),
            CancellationToken.None);

        response.User.Subject.Should().Be("subject-123");
        response.User.PreferredUsername.Should().Be("token-user");
        response.User.Email.Should().Be("token@example.com");
        response.User.Roles.Should().ContainSingle().Which.Should().Be(IdentityAppRoles.User);
        response.Identity.IdentityId.Should().Be(Guid.Empty);
        response.Identity.State.Should().Be("Unknown");
        response.Identity.BootstrapPending.Should().BeFalse();
        response.Identity.DownstreamBootstrapPending.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsPersistedIdentityFieldsAndNormalizedRoles_WhenIdentityExists()
    {
        var identityUser = new IdentityUser("subject-123", "stored-user", "stored@example.com");
        var store = new QueryIdentityUserStore(identityUser, ["admin", "user", "Admin"]);
        var handler = new GetCurrentUserQueryHandler(store);

        var response = await handler.HandleAsync(
            new CurrentUserQuery("subject-123", "token-user", "token@example.com"),
            CancellationToken.None);

        response.User.Subject.Should().Be("subject-123");
        response.User.PreferredUsername.Should().Be("stored-user");
        response.User.Email.Should().Be("stored@example.com");
        response.User.Roles.Should().Equal(IdentityAppRoles.Admin, IdentityAppRoles.User);
        response.Identity.IdentityId.Should().Be(identityUser.Id);
        response.Identity.State.Should().Be(nameof(IdentityLifecycleState.PendingBootstrap));
        response.Identity.BootstrapPending.Should().BeTrue();
        response.Identity.DownstreamBootstrapPending.Should().BeTrue();
    }

    private sealed class QueryIdentityUserStore(IdentityUser? identityUser = null, IReadOnlyList<string>? roles = null) : IIdentityUserStore
    {
        public Task<IdentityUser?> FindBySubjectAsync(string subject, CancellationToken cancellationToken) =>
            Task.FromResult(identityUser is not null && string.Equals(identityUser.Subject, subject, StringComparison.Ordinal)
                ? identityUser
                : null);

        public Task<IdentityUser?> FindByIdAsync(Guid identityId, CancellationToken cancellationToken) =>
            Task.FromResult(identityUser is not null && identityUser.Id == identityId ? identityUser : null);

        public Task<IReadOnlyList<string>> GetRoleNamesBySubjectAsync(string subject, CancellationToken cancellationToken) =>
            Task.FromResult(roles ?? [IdentityAppRoles.User]);

        public Task AddAsync(IdentityUser user, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> EnsureRoleAssignedAsync(string subject, string role, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task AddOutboxMessageAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> HasPendingUserProvisionedOutboxMessageAsync(string subject, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken) =>
            operation(cancellationToken);
    }
}
