using System.Linq.Expressions;
using FoodSocial.UserProfile.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodSocial.UserProfile.Infrastructure.Persistence;

public sealed partial class UserProfileStore
{
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

        return new UserSavedItemsSnapshot(posts, dishes, places);
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
        Expression<Func<TEntity, bool>> existsPredicate,
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
        Expression<Func<TEntity, bool>> predicate,
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

    private static Expression<Func<TEntity, bool>> CombineWithShell<TEntity>(
        Expression<Func<TEntity, bool>> itemPredicate,
        Guid shellId)
        where TEntity : class
    {
        var parameter = itemPredicate.Parameters.Single();
        var shellProperty = Expression.Property(parameter, nameof(SavedPost.UserProfileShellId));
        var shellEquals = Expression.Equal(shellProperty, Expression.Constant(shellId));
        var combinedBody = Expression.AndAlso(shellEquals, itemPredicate.Body);
        return Expression.Lambda<Func<TEntity, bool>>(combinedBody, parameter);
    }

    private static string ToPascalTargetType(string targetType) =>
        string.Equals(targetType, "post", StringComparison.Ordinal)
            ? "Post"
            : string.Equals(targetType, "dish", StringComparison.Ordinal)
                ? "Dish"
                : string.Equals(targetType, "place", StringComparison.Ordinal)
                    ? "Place"
                    : targetType;
}
