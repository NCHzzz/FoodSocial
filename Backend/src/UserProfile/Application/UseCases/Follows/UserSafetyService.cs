using FoodSocial.UserProfile.Application.DTOs;
using FoodSocial.UserProfile.Application.Interfaces.Repositories;
using FoodSocial.UserProfile.Application.UseCases.Profiles;
using FoodSocial.UserProfile.Domain;

namespace FoodSocial.UserProfile.Application.UseCases.Follows;

/// <summary>
/// Handles block and mute safety relationships within the UserProfile boundary.
/// </summary>
public sealed class UserSafetyService(
    IUserProfileStore userProfileStore,
    UserProfileOwnershipPolicy ownershipPolicy)
{
    private readonly IUserProfileStore _userProfileStore = userProfileStore ?? throw new ArgumentNullException(nameof(userProfileStore));
    private readonly UserProfileOwnershipPolicy _ownershipPolicy = ownershipPolicy ?? throw new ArgumentNullException(nameof(ownershipPolicy));

    /// <summary>
    /// Blocks the target user idempotently.
    /// </summary>
    public async Task<UserSafetyMutationResult> BlockAsync(string authenticatedSubject, Guid targetIdentityId, CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.RequireAuthenticatedOwnerAsync(authenticatedSubject, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserSafetyMutationResult(UserProfileOperationStatus.NotFound, targetIdentityId, RelationshipType.Block, false, "Your profile was not found.");
        }

        return await _userProfileStore.BlockAsync(authorization.Ownership!.IdentityId, targetIdentityId, cancellationToken);
    }

    /// <summary>
    /// Unblocks the target user idempotently.
    /// </summary>
    public async Task<UserSafetyMutationResult> UnblockAsync(string authenticatedSubject, Guid targetIdentityId, CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.RequireAuthenticatedOwnerAsync(authenticatedSubject, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserSafetyMutationResult(UserProfileOperationStatus.NotFound, targetIdentityId, RelationshipType.Block, false, "Your profile was not found.");
        }

        return await _userProfileStore.UnblockAsync(authorization.Ownership!.IdentityId, targetIdentityId, cancellationToken);
    }

    /// <summary>
    /// Mutes the target user idempotently.
    /// </summary>
    public async Task<UserSafetyMutationResult> MuteAsync(string authenticatedSubject, Guid targetIdentityId, CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.RequireAuthenticatedOwnerAsync(authenticatedSubject, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserSafetyMutationResult(UserProfileOperationStatus.NotFound, targetIdentityId, RelationshipType.Mute, false, "Your profile was not found.");
        }

        return await _userProfileStore.MuteAsync(authorization.Ownership!.IdentityId, targetIdentityId, cancellationToken);
    }

    /// <summary>
    /// Unmutes the target user idempotently.
    /// </summary>
    public async Task<UserSafetyMutationResult> UnmuteAsync(string authenticatedSubject, Guid targetIdentityId, CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.RequireAuthenticatedOwnerAsync(authenticatedSubject, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserSafetyMutationResult(UserProfileOperationStatus.NotFound, targetIdentityId, RelationshipType.Mute, false, "Your profile was not found.");
        }

        return await _userProfileStore.UnmuteAsync(authorization.Ownership!.IdentityId, targetIdentityId, cancellationToken);
    }
}
