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
            .Where(shell => shell.IdentityId == identityId)
            .Select(shell => new UserProfileOwnership(shell.Id, shell.IdentityId, shell.Subject))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<UserProfileOwnership?> FindOwnershipBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        return _dbContext.UserProfileShells
            .AsNoTracking()
            .Where(shell => shell.Subject == subject)
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

        // NOTE: EF Core SQLite cannot translate DateTimeOffset in SQL ORDER BY, so we
        // materialize the full result set and apply sort+limit in memory. The result set is
        // bounded per user (NormalizeLimit caps at 50), making this acceptable for MVP.
        // For production PostgreSQL, consider pushing OrderByDescending + Take into SQL.
        var rawItems = await (
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
                })
            .ToListAsync(cancellationToken);

        var boundedItems = rawItems
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(normalizedLimit)
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

        // NOTE: EF Core SQLite cannot translate DateTimeOffset in SQL ORDER BY, so we
        // materialize the full result set and apply sort+limit in memory. The result set is
        // bounded per user (NormalizeLimit caps at 50), making this acceptable for MVP.
        // For production PostgreSQL, consider pushing OrderByDescending + Take into SQL.
        var rawItems = await (
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
                })
            .ToListAsync(cancellationToken);

        var boundedItems = rawItems
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(normalizedLimit)
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

        var posts = await _dbContext.SavedPosts
            .AsNoTracking()
            .Where(savedPost => savedPost.UserProfileShellId == ownership.ShellId)
            .Select(savedPost => new SavedPostSnapshot(savedPost.PostId, savedPost.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var dishes = await _dbContext.SavedDishes
            .AsNoTracking()
            .Where(savedDish => savedDish.UserProfileShellId == ownership.ShellId)
            .Select(savedDish => new SavedDishSnapshot(savedDish.DishId, savedDish.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var places = await _dbContext.SavedPlaces
            .AsNoTracking()
            .Where(savedPlace => savedPlace.UserProfileShellId == ownership.ShellId)
            .Select(savedPlace => new SavedPlaceSnapshot(savedPlace.PlaceId, savedPlace.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new UserSavedItemsSnapshot(
            posts.OrderByDescending(item => item.SavedAtUtc).ToArray(),
            dishes.OrderByDescending(item => item.SavedAtUtc).ToArray(),
            places.OrderByDescending(item => item.SavedAtUtc).ToArray());
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
