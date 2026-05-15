using FoodSocial.UserProfile.Application.DTOs;
using FoodSocial.UserProfile.Application.Interfaces.Repositories;
using FoodSocial.UserProfile.Application.Policies;

namespace FoodSocial.UserProfile.Application.UseCases.Notifications;

public sealed class UserNotificationService(IUserProfileStore userProfileStore, UserProfileOwnershipPolicy ownershipPolicy)
{
    private const int DefaultListLimit = 50;
    private readonly IUserProfileStore _userProfileStore = userProfileStore ?? throw new ArgumentNullException(nameof(userProfileStore));
    private readonly UserProfileOwnershipPolicy _ownershipPolicy = ownershipPolicy ?? throw new ArgumentNullException(nameof(ownershipPolicy));

    public async Task<UserNotificationListResult> GetNotificationsAsync(string authenticatedSubject, Guid identityId, CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.AuthorizeOwnerAsync(authenticatedSubject, identityId, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserNotificationListResult(authorization.Status, null, authorization.Error);
        }

        var notifications = await _userProfileStore.GetNotificationsAsync(identityId, DefaultListLimit, cancellationToken);
        return notifications is null
            ? new UserNotificationListResult(UserProfileOperationStatus.NotFound, null, "The requested user profile was not found.")
            : new UserNotificationListResult(UserProfileOperationStatus.Success, notifications, null);
    }

    public async Task<UserNotificationResult> MarkNotificationReadAsync(string authenticatedSubject, Guid identityId, Guid notificationId, CancellationToken cancellationToken)
    {
        var authorization = await _ownershipPolicy.AuthorizeOwnerAsync(authenticatedSubject, identityId, cancellationToken);
        if (authorization.Status != UserProfileOperationStatus.Success)
        {
            return new UserNotificationResult(authorization.Status, null, authorization.Error);
        }

        var notification = await _userProfileStore.MarkNotificationReadAsync(identityId, notificationId, cancellationToken);
        return notification is null
            ? new UserNotificationResult(UserProfileOperationStatus.NotFound, null, "The requested notification was not found for the authenticated user.")
            : new UserNotificationResult(UserProfileOperationStatus.Success, notification, null);
    }
}
