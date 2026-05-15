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
public sealed class UserProfileStore(UserProfileDbContext dbContext) : IUserProfileStore
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

    public async Task<UserNotificationListSnapshot?> GetNotificationsAsync(Guid identityId, int limit, CancellationToken cancellationToken)
    {
        var ownership = await FindOwnershipByIdentityIdAsync(identityId, cancellationToken);
        if (ownership is null) return null;
        var normalizedLimit = NormalizeLimit(limit);
        var query = _dbContext.UserNotifications.AsNoTracking().Where(notification => notification.UserProfileShellId == ownership.ShellId);
        var unreadCount = await query.CountAsync(notification => notification.Status == UserNotificationStatus.Unread, cancellationToken);
        IReadOnlyList<UserNotification> rawItems = IsSqliteProvider()
            ? (await query.ToListAsync(cancellationToken))
                .OrderByDescending(notification => notification.CreatedAtUtc)
                .ThenBy(notification => notification.Id)
                .Take(normalizedLimit)
                .ToArray()
            : await query
                .OrderByDescending(notification => notification.CreatedAtUtc)
                .ThenBy(notification => notification.Id)
                .Take(normalizedLimit)
                .ToListAsync(cancellationToken);
        var items = rawItems.Select(MapNotification).ToArray();
        return new UserNotificationListSnapshot(items, normalizedLimit, unreadCount);
    }

    public async Task<UserNotificationSnapshot?> MarkNotificationReadAsync(Guid identityId, Guid notificationId, CancellationToken cancellationToken)
    {
        var ownership = await FindOwnershipByIdentityIdAsync(identityId, cancellationToken);
        if (ownership is null) return null;
        var notification = await _dbContext.UserNotifications.SingleOrDefaultAsync(item => item.UserProfileShellId == ownership.ShellId && item.Id == notificationId, cancellationToken);
        if (notification is null) return null;
        notification.MarkRead();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapNotification(notification);
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

    /// <inheritdoc />
    public async Task<UserSavedItemsSnapshot?> GetSavedItemsAsync(Guid identityId, CancellationToken cancellationToken)
    {
        var ownership = await FindOwnershipByIdentityIdAsync(identityId, cancellationToken);
        if (ownership is null)
        {
            return null;
        }

        var postsQuery = _dbContext.SavedPosts
            .AsNoTracking()
            .Where(savedPost => savedPost.UserProfileShellId == ownership.ShellId)
            .Select(savedPost => new SavedPostSnapshot(savedPost.PostId, savedPost.CreatedAtUtc));
        var posts = IsSqliteProvider()
            ? (await postsQuery.ToListAsync(cancellationToken)).OrderByDescending(item => item.SavedAtUtc).ToArray()
            : await postsQuery.OrderByDescending(item => item.SavedAtUtc).ToArrayAsync(cancellationToken);

        var dishesQuery = _dbContext.SavedDishes
            .AsNoTracking()
            .Where(savedDish => savedDish.UserProfileShellId == ownership.ShellId)
            .Select(savedDish => new SavedDishSnapshot(savedDish.DishId, savedDish.CreatedAtUtc));
        var dishes = IsSqliteProvider()
            ? (await dishesQuery.ToListAsync(cancellationToken)).OrderByDescending(item => item.SavedAtUtc).ToArray()
            : await dishesQuery.OrderByDescending(item => item.SavedAtUtc).ToArrayAsync(cancellationToken);

        var placesQuery = _dbContext.SavedPlaces
            .AsNoTracking()
            .Where(savedPlace => savedPlace.UserProfileShellId == ownership.ShellId)
            .Select(savedPlace => new SavedPlaceSnapshot(savedPlace.PlaceId, savedPlace.CreatedAtUtc));
        var places = IsSqliteProvider()
            ? (await placesQuery.ToListAsync(cancellationToken)).OrderByDescending(item => item.SavedAtUtc).ToArray()
            : await placesQuery.OrderByDescending(item => item.SavedAtUtc).ToArrayAsync(cancellationToken);

        return new UserSavedItemsSnapshot(
            posts,
            dishes,
            places);
    }

    /// <inheritdoc />
    public Task<UserSavedItemMutationResult> SavePostAsync(Guid identityId, Guid postId, CancellationToken cancellationToken) =>
        SaveItemAsync(identityId, postId, "post", shellId => new SavedPost(shellId, postId), savedPost => savedPost.PostId == postId, cancellationToken);

    /// <inheritdoc />
    public Task<UserSavedItemMutationResult> UnsavePostAsync(Guid identityId, Guid postId, CancellationToken cancellationToken) =>
        UnsaveItemAsync(identityId, postId, "post", postId, _dbContext.SavedPosts, savedPost => savedPost.PostId == postId, cancellationToken);

    /// <inheritdoc />
    public Task<UserSavedItemMutationResult> SaveDishAsync(Guid identityId, Guid dishId, CancellationToken cancellationToken) =>
        SaveItemAsync(identityId, dishId, "dish", shellId => new SavedDish(shellId, dishId), savedDish => savedDish.DishId == dishId, cancellationToken);

    /// <inheritdoc />
    public Task<UserSavedItemMutationResult> UnsaveDishAsync(Guid identityId, Guid dishId, CancellationToken cancellationToken) =>
        UnsaveItemAsync(identityId, dishId, "dish", dishId, _dbContext.SavedDishes, savedDish => savedDish.DishId == dishId, cancellationToken);

    /// <inheritdoc />
    public Task<UserSavedItemMutationResult> SavePlaceAsync(Guid identityId, Guid placeId, CancellationToken cancellationToken) =>
        SaveItemAsync(identityId, placeId, "place", shellId => new SavedPlace(shellId, placeId), savedPlace => savedPlace.PlaceId == placeId, cancellationToken);

    /// <inheritdoc />
    public Task<UserSavedItemMutationResult> UnsavePlaceAsync(Guid identityId, Guid placeId, CancellationToken cancellationToken) =>
        UnsaveItemAsync(identityId, placeId, "place", placeId, _dbContext.SavedPlaces, savedPlace => savedPlace.PlaceId == placeId, cancellationToken);

    private async Task<UserSavedItemMutationResult> SaveItemAsync<TEntity>(
        Guid identityId,
        Guid targetId,
        string targetType,
        Func<Guid, TEntity> entityFactory,
        System.Linq.Expressions.Expression<Func<TEntity, bool>> existsPredicate,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var ownership = await FindOwnershipByIdentityIdAsync(identityId, cancellationToken);
        if (ownership is null)
        {
            return new UserSavedItemMutationResult(UserProfileOperationStatus.NotFound, identityId, targetId, targetType, false, "The requested user profile was not found.");
        }

        _dbContext.Add(entityFactory(ownership.ShellId));
        _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(identityId, $"Saved{ToPascalTargetType(targetType)}", targetId, targetType));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _dbContext.ChangeTracker.Clear();

            var duplicateExists = await _dbContext.Set<TEntity>()
                .AnyAsync(CombineWithShell(existsPredicate, ownership.ShellId), cancellationToken);

            if (!duplicateExists)
            {
                throw;
            }
        }

        return new UserSavedItemMutationResult(UserProfileOperationStatus.Success, identityId, targetId, targetType, true, null);
    }

    private async Task<UserSavedItemMutationResult> UnsaveItemAsync<TEntity>(
        Guid identityId,
        Guid targetId,
        string targetType,
        Guid itemId,
        DbSet<TEntity> dbSet,
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var ownership = await FindOwnershipByIdentityIdAsync(identityId, cancellationToken);
        if (ownership is null)
        {
            return new UserSavedItemMutationResult(UserProfileOperationStatus.NotFound, identityId, targetId, targetType, false, "The requested user profile was not found.");
        }

        var entity = await dbSet.SingleOrDefaultAsync(CombineWithShell(predicate, ownership.ShellId), cancellationToken);
        if (entity is not null)
        {
            dbSet.Remove(entity);
            _dbContext.IntegrationOutboxMessages.Add(CreateOutboxMessage(identityId, $"Unsaved{ToPascalTargetType(targetType)}", targetId, targetType));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new UserSavedItemMutationResult(UserProfileOperationStatus.Success, identityId, itemId, targetType, false, null);
    }

    private static System.Linq.Expressions.Expression<Func<TEntity, bool>> CombineWithShell<TEntity>(
        System.Linq.Expressions.Expression<Func<TEntity, bool>> itemPredicate,
        Guid shellId)
        where TEntity : class
    {
        var parameter = itemPredicate.Parameters.Single();
        var shellProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(SavedPost.UserProfileShellId));
        var shellEquals = System.Linq.Expressions.Expression.Equal(shellProperty, System.Linq.Expressions.Expression.Constant(shellId));
        var combinedBody = System.Linq.Expressions.Expression.AndAlso(shellEquals, itemPredicate.Body);
        return System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(combinedBody, parameter);
    }

    private static UserNotificationSnapshot MapNotification(UserNotification notification) =>
        new(notification.Id, notification.RecipientIdentityId, notification.ActorIdentityId, notification.Type, notification.Message, notification.Status == UserNotificationStatus.Read, notification.CreatedAtUtc, notification.ReadAtUtc);

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

    private static string ToPascalTargetType(string targetType) =>
        string.Equals(targetType, "post", StringComparison.Ordinal)
            ? "Post"
            : string.Equals(targetType, "dish", StringComparison.Ordinal)
                ? "Dish"
                : string.Equals(targetType, "place", StringComparison.Ordinal)
                    ? "Place"
                    : targetType;

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
