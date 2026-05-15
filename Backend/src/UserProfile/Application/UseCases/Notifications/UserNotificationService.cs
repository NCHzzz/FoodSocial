using FoodSocial.UserProfile.Application.DTOs;
using FoodSocial.UserProfile.Application.Interfaces.Repositories;

namespace FoodSocial.UserProfile.Application.UseCases.Notifications;

public sealed class UserNotificationService(IUserProfileStore userProfileStore)
{
    private const int DefaultListLimit = 50;
    private readonly IUserProfileStore _userProfileStore = userProfileStore ?? throw new ArgumentNullException(nameof(userProfileStore));

    public async Task<UserNotificationListSnapshot?> GetNotificationsAsync(string authenticatedSubject, Guid identityId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticatedSubject);
        var owner = await _userProfileStore.FindOwnershipBySubjectAsync(authenticatedSubject, cancellationToken);
        if (owner is null || owner.IdentityId != identityId) return null;
        return await _userProfileStore.GetNotificationsAsync(identityId, DefaultListLimit, cancellationToken);
    }

    public async Task<UserNotificationSnapshot?> MarkNotificationReadAsync(string authenticatedSubject, Guid identityId, Guid notificationId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticatedSubject);
        var owner = await _userProfileStore.FindOwnershipBySubjectAsync(authenticatedSubject, cancellationToken);
        if (owner is null || owner.IdentityId != identityId) return null;
        return await _userProfileStore.MarkNotificationReadAsync(identityId, notificationId, cancellationToken);
    }
}
