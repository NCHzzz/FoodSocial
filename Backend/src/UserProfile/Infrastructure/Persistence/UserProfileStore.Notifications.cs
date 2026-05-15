using FoodSocial.UserProfile.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodSocial.UserProfile.Infrastructure.Persistence;

public sealed partial class UserProfileStore
{
    /// <inheritdoc />
    public async Task<UserNotificationListSnapshot?> GetNotificationsAsync(Guid identityId, int limit, CancellationToken cancellationToken)
    {
        var ownership = await FindOwnershipByIdentityIdAsync(identityId, cancellationToken);
        if (ownership is null)
        {
            return null;
        }

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

    /// <inheritdoc />
    public async Task<UserNotificationSnapshot?> MarkNotificationReadAsync(Guid identityId, Guid notificationId, CancellationToken cancellationToken)
    {
        var ownership = await FindOwnershipByIdentityIdAsync(identityId, cancellationToken);
        if (ownership is null)
        {
            return null;
        }

        var notification = await _dbContext.UserNotifications.SingleOrDefaultAsync(
            item => item.UserProfileShellId == ownership.ShellId && item.Id == notificationId,
            cancellationToken);
        if (notification is null)
        {
            return null;
        }

        notification.MarkRead();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapNotification(notification);
    }

    private static UserNotificationSnapshot MapNotification(UserNotification notification) =>
        new(
            notification.Id,
            notification.RecipientIdentityId,
            notification.ActorIdentityId,
            notification.Type,
            notification.Message,
            notification.Status == UserNotificationStatus.Read,
            notification.CreatedAtUtc,
            notification.ReadAtUtc);
}
