using FluentAssertions;
using FoodSocial.Contracts.Identity;
using FoodSocial.UserProfile.Application;
using FoodSocial.UserProfile.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FoodSocial.UserProfile.Api.Tests;

public sealed class UserProfilePersistenceIntegrationTests
{
    [Fact]
    public async Task Bootstrap_RealSqliteStore_FirstBootstrapCreatesShellAndProfileDetails()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);
        var store = new UserProfileShellStore(dbContext);
        var service = new UserProfileBootstrapService(store);
        var integrationEvent = CreateProvisionedEvent(identityId: Guid.NewGuid(), subject: "profile-subject", preferredUsername: "demo-user");

        var result = await service.EnsureShellAsync(integrationEvent, CancellationToken.None);

        result.Created.Should().BeTrue();
        dbContext.ChangeTracker.Clear();

        var shell = await dbContext.UserProfileShells.SingleAsync(candidate => candidate.IdentityId == integrationEvent.IdentityId);
        var details = await dbContext.UserProfileDetails.SingleAsync(candidate => candidate.UserProfileShellId == shell.Id);

        details.DisplayName.Should().Be("demo-user");
        details.Bio.Should().BeNull();
        details.AvatarUrl.Should().BeNull();
        details.PreferredCuisines.Should().BeEmpty();
        details.DislikedIngredients.Should().BeEmpty();
        details.DietaryRestrictions.Should().BeEmpty();
    }

    [Fact]
    public async Task Bootstrap_RealSqliteStore_DuplicateBootstrapKeepsSingleShellAndSingleProfileDetails()
    {
        await using var connection = await OpenDatabaseAsync();

        await using (var setupContext = CreateDbContext(connection))
        {
            var setupStore = new UserProfileShellStore(setupContext);
            var setupService = new UserProfileBootstrapService(setupStore);
            await setupService.EnsureShellAsync(CreateProvisionedEvent(Guid.Parse("11111111-1111-1111-1111-111111111111"), "duplicate-subject", "first-user"), CancellationToken.None);
        }

        await using var dbContext = CreateDbContext(connection);
        var store = new UserProfileShellStore(dbContext);
        var service = new UserProfileBootstrapService(store);

        var result = await service.EnsureShellAsync(
            CreateProvisionedEvent(Guid.Parse("11111111-1111-1111-1111-111111111111"), "duplicate-subject", "second-user"),
            CancellationToken.None);

        result.Created.Should().BeFalse();
        dbContext.ChangeTracker.Clear();

        var shellCount = await dbContext.UserProfileShells.LongCountAsync(candidate => candidate.IdentityId == Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var detailsCount = await dbContext.UserProfileDetails.LongCountAsync();

        shellCount.Should().Be(1);
        detailsCount.Should().Be(1);
    }

    [Fact]
    public async Task Persistence_RealSqliteStore_CanPersistFollowAndSavedRelationships()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);

        var followerShell = new FoodSocial.UserProfile.Domain.UserProfileShell(Guid.NewGuid(), "follower-subject");
        var followedShell = new FoodSocial.UserProfile.Domain.UserProfileShell(Guid.NewGuid(), "followed-subject");
        dbContext.UserProfileShells.AddRange(followerShell, followedShell);

        dbContext.UserFollows.Add(new FoodSocial.UserProfile.Domain.UserFollow(followerShell.Id, followedShell.Id));
        dbContext.SavedPosts.Add(new FoodSocial.UserProfile.Domain.SavedPost(followerShell.Id, Guid.NewGuid()));
        dbContext.SavedDishes.Add(new FoodSocial.UserProfile.Domain.SavedDish(followerShell.Id, Guid.NewGuid()));
        dbContext.SavedPlaces.Add(new FoodSocial.UserProfile.Domain.SavedPlace(followerShell.Id, Guid.NewGuid()));

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        (await dbContext.UserFollows.LongCountAsync()).Should().Be(1);
        (await dbContext.SavedPosts.LongCountAsync()).Should().Be(1);
        (await dbContext.SavedDishes.LongCountAsync()).Should().Be(1);
        (await dbContext.SavedPlaces.LongCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Persistence_RealSqliteStore_DuplicateSavedPostViolatesUniqueness()
    {
        await using var connection = await OpenDatabaseAsync();
        await using var dbContext = CreateDbContext(connection);

        var shell = new FoodSocial.UserProfile.Domain.UserProfileShell(Guid.NewGuid(), "saved-post-subject");
        var postId = Guid.NewGuid();
        dbContext.UserProfileShells.Add(shell);
        dbContext.SavedPosts.Add(new FoodSocial.UserProfile.Domain.SavedPost(shell.Id, postId));
        dbContext.SavedPosts.Add(new FoodSocial.UserProfile.Domain.SavedPost(shell.Id, postId));

        var action = async () => await dbContext.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    private static UserProvisionedIntegrationEvent CreateProvisionedEvent(Guid identityId, string subject, string preferredUsername) =>
        new(
            EventId: Guid.NewGuid(),
            IdentityId: identityId,
            Subject: subject,
            PreferredUsername: preferredUsername,
            Email: $"{preferredUsername}@example.com",
            Roles: ["user"],
            LifecycleState: "PendingBootstrap",
            DownstreamBootstrapPending: true,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            EventVersion: 1,
            CorrelationId: null,
            CausationId: null);

    private static UserProfileDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<UserProfileDbContext>()
            .UseSqlite(connection)
            .Options;

        return new UserProfileDbContext(options);
    }

    private static async Task<SqliteConnection> OpenDatabaseAsync()
    {
        var connection = new SqliteConnection($"Data Source={Path.Combine(Path.GetTempPath(), $"userprofile-tests-{Guid.NewGuid():N}.db")}");
        await connection.OpenAsync();

        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.ChangeTracker.Clear();

        return connection;
    }
}
