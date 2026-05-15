using FoodSocial.UserProfile.Application.DTOs;
using FoodSocial.UserProfile.Application.Interfaces.Repositories;
using FoodSocial.UserProfile.Application.UseCases.Profiles;

namespace FoodSocial.UserProfile.Application.UseCases.SavedItems;

/// <summary>
/// Handles owner-scoped saved-item operations.
/// </summary>
public sealed class UserSavedItemsService(IUserProfileStore userProfileStore, UserProfileOwnershipPolicy ownershipPolicy)
{
    private readonly IUserProfileStore _userProfileStore = userProfileStore ?? throw new ArgumentNullException(nameof(userProfileStore));
    private readonly UserProfileOwnershipPolicy _ownershipPolicy = ownershipPolicy ?? throw new ArgumentNullException(nameof(ownershipPolicy));

    /// <summary>
    /// Gets saved items for the supplied owner-scoped profile.
    /// </summary>
    public async Task<UserSavedItemsResult> GetSavedItemsAsync(
        string authenticatedSubject,
        Guid identityId,
        CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.AuthorizeOwnerAsync(authenticatedSubject, identityId, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserSavedItemsResult(authorization.Status, null, authorization.Error);
        }

        var savedItems = await _userProfileStore.GetSavedItemsAsync(identityId, cancellationToken);
        return savedItems is null
            ? new UserSavedItemsResult(UserProfileOperationStatus.NotFound, null, "The requested user profile was not found.")
            : new UserSavedItemsResult(UserProfileOperationStatus.Success, savedItems, null);
    }

    /// <summary>
    /// Saves a post idempotently for the owner-scoped profile.
    /// </summary>
    public async Task<UserSavedItemMutationResult> SavePostAsync(string authenticatedSubject, Guid identityId, Guid postId, CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.AuthorizeOwnerAsync(authenticatedSubject, identityId, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserSavedItemMutationResult(authorization.Status, identityId, postId, "post", false, authorization.Error);
        }

        return await _userProfileStore.SavePostAsync(identityId, postId, cancellationToken);
    }

    /// <summary>
    /// Unsaves a post idempotently for the owner-scoped profile.
    /// </summary>
    public async Task<UserSavedItemMutationResult> UnsavePostAsync(string authenticatedSubject, Guid identityId, Guid postId, CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.AuthorizeOwnerAsync(authenticatedSubject, identityId, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserSavedItemMutationResult(authorization.Status, identityId, postId, "post", false, authorization.Error);
        }

        return await _userProfileStore.UnsavePostAsync(identityId, postId, cancellationToken);
    }

    /// <summary>
    /// Saves a dish idempotently for the owner-scoped profile.
    /// </summary>
    public async Task<UserSavedItemMutationResult> SaveDishAsync(string authenticatedSubject, Guid identityId, Guid dishId, CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.AuthorizeOwnerAsync(authenticatedSubject, identityId, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserSavedItemMutationResult(authorization.Status, identityId, dishId, "dish", false, authorization.Error);
        }

        return await _userProfileStore.SaveDishAsync(identityId, dishId, cancellationToken);
    }

    /// <summary>
    /// Unsaves a dish idempotently for the owner-scoped profile.
    /// </summary>
    public async Task<UserSavedItemMutationResult> UnsaveDishAsync(string authenticatedSubject, Guid identityId, Guid dishId, CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.AuthorizeOwnerAsync(authenticatedSubject, identityId, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserSavedItemMutationResult(authorization.Status, identityId, dishId, "dish", false, authorization.Error);
        }

        return await _userProfileStore.UnsaveDishAsync(identityId, dishId, cancellationToken);
    }

    /// <summary>
    /// Saves a place idempotently for the owner-scoped profile.
    /// </summary>
    public async Task<UserSavedItemMutationResult> SavePlaceAsync(string authenticatedSubject, Guid identityId, Guid placeId, CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.AuthorizeOwnerAsync(authenticatedSubject, identityId, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserSavedItemMutationResult(authorization.Status, identityId, placeId, "place", false, authorization.Error);
        }

        return await _userProfileStore.SavePlaceAsync(identityId, placeId, cancellationToken);
    }

    /// <summary>
    /// Unsaves a place idempotently for the owner-scoped profile.
    /// </summary>
    public async Task<UserSavedItemMutationResult> UnsavePlaceAsync(string authenticatedSubject, Guid identityId, Guid placeId, CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.AuthorizeOwnerAsync(authenticatedSubject, identityId, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserSavedItemMutationResult(authorization.Status, identityId, placeId, "place", false, authorization.Error);
        }

        return await _userProfileStore.UnsavePlaceAsync(identityId, placeId, cancellationToken);
    }
}
