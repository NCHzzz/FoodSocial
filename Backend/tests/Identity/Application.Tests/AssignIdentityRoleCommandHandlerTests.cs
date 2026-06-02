using FluentAssertions;
using FoodSocial.Contracts.Identity;
using FoodSocial.Identity.Domain;
using Xunit;

namespace FoodSocial.Identity.Application.Tests;

public sealed class AssignIdentityRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_AssignsNormalizedRoleAndSavesChanges()
    {
        var store = new RoleAssignmentIdentityUserStore();
        var handler = new AssignIdentityRoleCommandHandler(store);

        var result = await handler.HandleAsync(
            new AssignIdentityRoleCommand("subject-123", "Admin"),
            CancellationToken.None);

        result.Subject.Should().Be("subject-123");
        result.Role.Should().Be(IdentityAppRoles.Admin);
        result.Created.Should().BeTrue();
        store.AssignedRoles.Should().ContainSingle().Which.Should().Be(("subject-123", IdentityAppRoles.Admin));
        store.SaveChangesCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ReturnsCreatedFalse_WhenRoleAlreadyAssigned()
    {
        var store = new RoleAssignmentIdentityUserStore(("subject-123", IdentityAppRoles.Moderator));
        var handler = new AssignIdentityRoleCommandHandler(store);

        var result = await handler.HandleAsync(
            new AssignIdentityRoleCommand("subject-123", "moderator"),
            CancellationToken.None);

        result.Created.Should().BeFalse();
        store.AssignedRoles.Should().ContainSingle().Which.Should().Be(("subject-123", IdentityAppRoles.Moderator));
        store.SaveChangesCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Throws_WhenRoleIsUnsupported()
    {
        var store = new RoleAssignmentIdentityUserStore();
        var handler = new AssignIdentityRoleCommandHandler(store);

        var act = () => handler.HandleAsync(
            new AssignIdentityRoleCommand("subject-123", "super-admin"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        store.AssignedRoles.Should().BeEmpty();
        store.SaveChangesCount.Should().Be(0);
    }

    private sealed class RoleAssignmentIdentityUserStore(params (string Subject, string Role)[] roles) : IIdentityUserStore
    {
        public List<(string Subject, string Role)> AssignedRoles { get; } = [.. roles];

        public int SaveChangesCount { get; private set; }

        public Task<bool> EnsureRoleAssignedAsync(string subject, string role, CancellationToken cancellationToken)
        {
            var assignment = (subject, role);
            if (AssignedRoles.Contains(assignment))
            {
                return Task.FromResult(false);
            }

            AssignedRoles.Add(assignment);
            return Task.FromResult(true);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }

        public Task<IdentityUser?> FindBySubjectAsync(string subject, CancellationToken cancellationToken) => Task.FromResult<IdentityUser?>(null);

        public Task<IdentityUser?> FindByIdAsync(Guid identityId, CancellationToken cancellationToken) => Task.FromResult<IdentityUser?>(null);

        public Task AddAsync(IdentityUser user, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<string>> GetRoleNamesBySubjectAsync(string subject, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task AddOutboxMessageAsync(UserProvisionedIntegrationEvent integrationEvent, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> HasPendingUserProvisionedOutboxMessageAsync(string subject, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken) =>
            operation(cancellationToken);
    }
}
