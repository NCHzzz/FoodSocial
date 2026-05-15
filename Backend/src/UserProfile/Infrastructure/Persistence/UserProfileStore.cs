using FoodSocial.Contracts.UserProfiles;
using FoodSocial.Messaging;
using FoodSocial.UserProfile.Application;
using FoodSocial.UserProfile.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodSocial.UserProfile.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core implementation of the application-owned user profile graph store.
/// </summary>
public sealed partial class UserProfileStore(UserProfileDbContext dbContext) : IUserProfileStore
{
    private readonly UserProfileDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public Task<UserProfileOwnership?> FindOwnershipByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(identityId, Guid.Empty);

        return _dbContext.UserProfileShells
            .AsNoTracking()
            .Where(shell => shell.TenantId == UserProfileShell.DefaultTenantId && shell.IdentityId == identityId)
            .Select(shell => new UserProfileOwnership(shell.Id, shell.IdentityId, shell.Subject))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserProfileOwnership?> FindOwnershipBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        return _dbContext.UserProfileShells
            .AsNoTracking()
            .Where(shell => shell.TenantId == UserProfileShell.DefaultTenantId && shell.Subject == subject)
            .Select(shell => new UserProfileOwnership(shell.Id, shell.IdentityId, shell.Subject))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserProfileSnapshot?> GetProfileAsync(Guid identityId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(identityId, Guid.Empty);

        var shell = await _dbContext.UserProfileShells
            .AsNoTracking()
            .Include(candidate => candidate.Detail)
            .SingleOrDefaultAsync(candidate => candidate.IdentityId == identityId, cancellationToken);

        return shell is null ? null : await BuildSnapshotAsync(shell, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserProfileSnapshot?> UpdateProfileAsync(Guid identityId, UpdateUserProfileCommand command, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(identityId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(command);

        var shell = await _dbContext.UserProfileShells
            .Include(candidate => candidate.Detail)
            .SingleOrDefaultAsync(candidate => candidate.IdentityId == identityId, cancellationToken);

        if (shell is null)
        {
            return null;
        }

        var detail = shell.EnsureDetail(command.DisplayName);
        detail.Update(
            command.DisplayName,
            command.AvatarUrl,
            command.Bio,
            command.PreferredCuisines,
            command.DislikedIngredients,
            command.DietaryRestrictions);

        _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(identityId, "ProfileUpdated"));
        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.ChangeTracker.Clear();

        return await GetProfileAsync(identityId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserProfileConnectionsSnapshot?> GetFollowersAsync(Guid identityId, int limit, CancellationToken cancellationToken)
    {
        var ownership = await FindOwnershipByIdentityIdAsync(identityId, cancellationToken);
        if (ownership is null)
        {
            return null;
        }

        var normalizedLimit = NormalizeLimit(limit);
        var totalCount = await _dbContext.UserFollows
            .AsNoTracking()
            .LongCountAsync(follow => follow.FollowedUserProfileShellId == ownership.ShellId, cancellationToken);

        var followersQuery =
            from follow in _dbContext.UserFollows.AsNoTracking()
            join shell in _dbContext.UserProfileShells.AsNoTracking() on follow.FollowerUserProfileShellId equals shell.Id
            join detail in _dbContext.UserProfileDetails.AsNoTracking() on shell.Id equals detail.UserProfileShellId into detailGroup
            from detail in detailGroup.DefaultIfEmpty()
            where follow.FollowedUserProfileShellId == ownership.ShellId
            select new
            {
                follow.CreatedAtUtc,
                shell.IdentityId,
                DisplayName = detail != null ? detail.DisplayName : null,
                AvatarUrl = detail != null ? detail.AvatarUrl : null
            };

        var rawItems = IsSqliteProvider()
            ? (await followersQuery.ToListAsync(cancellationToken))
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenBy(item => item.IdentityId)
                .Take(normalizedLimit)
                .ToList()
            : await followersQuery
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenBy(item => item.IdentityId)
                .Take(normalizedLimit)
                .ToListAsync(cancellationToken);

        var boundedItems = rawItems
            .Select(item => new UserProfileSummary(item.IdentityId, item.DisplayName, item.AvatarUrl))
            .ToArray();

        return new UserProfileConnectionsSnapshot(boundedItems, totalCount);
    }

    /// <inheritdoc />
    public async Task<UserProfileConnectionsSnapshot?> GetFollowingAsync(Guid identityId, int limit, CancellationToken cancellationToken)
    {
        var ownership = await FindOwnershipByIdentityIdAsync(identityId, cancellationToken);
        if (ownership is null)
        {
            return null;
        }

        var normalizedLimit = NormalizeLimit(limit);
        var totalCount = await _dbContext.UserFollows
            .AsNoTracking()
            .LongCountAsync(follow => follow.FollowerUserProfileShellId == ownership.ShellId, cancellationToken);

        var followingQuery =
            from follow in _dbContext.UserFollows.AsNoTracking()
            join shell in _dbContext.UserProfileShells.AsNoTracking() on follow.FollowedUserProfileShellId equals shell.Id
            join detail in _dbContext.UserProfileDetails.AsNoTracking() on shell.Id equals detail.UserProfileShellId into detailGroup
            from detail in detailGroup.DefaultIfEmpty()
            where follow.FollowerUserProfileShellId == ownership.ShellId
            select new
            {
                follow.CreatedAtUtc,
                shell.IdentityId,
                DisplayName = detail != null ? detail.DisplayName : null,
                AvatarUrl = detail != null ? detail.AvatarUrl : null
            };

        var rawItems = IsSqliteProvider()
            ? (await followingQuery.ToListAsync(cancellationToken))
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenBy(item => item.IdentityId)
                .Take(normalizedLimit)
                .ToList()
            : await followingQuery
                .OrderByDescending(item => item.CreatedAtUtc)
                .ThenBy(item => item.IdentityId)
                .Take(normalizedLimit)
                .ToListAsync(cancellationToken);

        var boundedItems = rawItems
            .Select(item => new UserProfileSummary(item.IdentityId, item.DisplayName, item.AvatarUrl))
            .ToArray();

        return new UserProfileConnectionsSnapshot(boundedItems, totalCount);
    }

    /// <inheritdoc />
    public async Task<UserProfileMutationResult> FollowAsync(Guid followerIdentityId, Guid followedIdentityId, CancellationToken cancellationToken)
    {
        var follower = await FindOwnershipByIdentityIdAsync(followerIdentityId, cancellationToken);
        var followed = await FindOwnershipByIdentityIdAsync(followedIdentityId, cancellationToken);
        if (follower is null || followed is null)
        {
            return new UserProfileMutationResult(UserProfileOperationStatus.NotFound, followedIdentityId, false, "The requested user profile was not found.");
        }

        if (follower.ShellId == followed.ShellId)
        {
            return new UserProfileMutationResult(UserProfileOperationStatus.Invalid, followedIdentityId, false, "A user cannot follow their own profile.");
        }

        var exists = await _dbContext.UserFollows.AnyAsync(
            follow => follow.FollowerUserProfileShellId == follower.ShellId && follow.FollowedUserProfileShellId == followed.ShellId,
            cancellationToken);

        if (exists)
        {
            return new UserProfileMutationResult(UserProfileOperationStatus.Success, followedIdentityId, true, null);
        }

        _dbContext.UserFollows.Add(new UserFollow(follower.ShellId, followed.ShellId));
        _dbContext.UserNotifications.Add(new UserNotification(followed.ShellId, followed.IdentityId, follower.IdentityId, "UserFollowed", "followed you"));
        _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(follower.IdentityId, "Followed", followed.IdentityId, "UserProfile"));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();

            var duplicateExists = await _dbContext.UserFollows.AnyAsync(
                follow => follow.FollowerUserProfileShellId == follower.ShellId && follow.FollowedUserProfileShellId == followed.ShellId,
                cancellationToken);

            if (!duplicateExists)
            {
                throw;
            }
        }

        return new UserProfileMutationResult(UserProfileOperationStatus.Success, followedIdentityId, true, null);
    }

    /// <inheritdoc />
    public async Task<UserProfileMutationResult> UnfollowAsync(Guid followerIdentityId, Guid followedIdentityId, CancellationToken cancellationToken)
    {
        var follower = await FindOwnershipByIdentityIdAsync(followerIdentityId, cancellationToken);
        var followed = await FindOwnershipByIdentityIdAsync(followedIdentityId, cancellationToken);
        if (follower is null || followed is null)
        {
            return new UserProfileMutationResult(UserProfileOperationStatus.NotFound, followedIdentityId, false, "The requested user profile was not found.");
        }

        var relationship = await _dbContext.UserFollows.SingleOrDefaultAsync(
            follow => follow.FollowerUserProfileShellId == follower.ShellId && follow.FollowedUserProfileShellId == followed.ShellId,
            cancellationToken);

        if (relationship is null)
        {
            return new UserProfileMutationResult(UserProfileOperationStatus.Success, followedIdentityId, false, null);
        }

        _dbContext.UserFollows.Remove(relationship);
        _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(follower.IdentityId, "Unfollowed", followed.IdentityId, "UserProfile"));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new UserProfileMutationResult(UserProfileOperationStatus.Success, followedIdentityId, false, null);
    }

    private static IntegrationOutboxMessage CreateOutboxMessage(Guid identityId, string changeType, Guid? targetId = null, string? targetType = null)
    {
        var integrationEvent = UserProfileChangedIntegrationEvent.Create(
            identityId,
            UserProfileShell.DefaultTenantId,
            changeType,
            targetId,
            targetType);

        return IntegrationOutboxMessage.Create(
            integrationEvent,
            integrationEvent.EventId,
            integrationEvent.EventVersion,
            integrationEvent.OccurredAtUtc,
            integrationEvent.CorrelationId,
            "userprofile.events",
            $"userprofile.profile.{changeType.ToLowerInvariant()}.v1",
            identityId,
            "UserProfile");
    }

    private async Task<UserProfileSnapshot> BuildSnapshotAsync(UserProfileShell shell, CancellationToken cancellationToken)
    {
        var followerCount = await _dbContext.UserFollows
            .AsNoTracking()
            .LongCountAsync(follow => follow.FollowedUserProfileShellId == shell.Id, cancellationToken);

        var followingCount = await _dbContext.UserFollows
            .AsNoTracking()
            .LongCountAsync(follow => follow.FollowerUserProfileShellId == shell.Id, cancellationToken);

        return new UserProfileSnapshot(
            shell.IdentityId,
            shell.Subject,
            shell.Detail?.DisplayName,
            shell.Detail?.AvatarUrl,
            shell.Detail?.Bio,
            shell.Detail?.PreferredCuisines ?? [],
            shell.Detail?.DislikedIngredients ?? [],
            shell.Detail?.DietaryRestrictions ?? [],
            followerCount,
            followingCount);
    }

    private static int NormalizeLimit(int limit) => limit <= 0 ? 50 : Math.Min(limit, 100);

    private bool IsSqliteProvider() =>
        string.Equals(_dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal);

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        if (exception.InnerException is PostgresException postgresException)
        {
            return postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
        }

        if (exception.InnerException is null
            || !string.Equals(exception.InnerException.GetType().FullName, "Microsoft.Data.Sqlite.SqliteException", StringComparison.Ordinal))
        {
            return false;
        }

        var sqliteErrorCodeProperty = exception.InnerException.GetType().GetProperty("SqliteErrorCode");
        var sqliteErrorCode = sqliteErrorCodeProperty?.GetValue(exception.InnerException);
        return sqliteErrorCode is int code && code == 19;
    }
}
