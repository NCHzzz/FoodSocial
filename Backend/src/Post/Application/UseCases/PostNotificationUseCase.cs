using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.Mapping;

namespace FoodSocial.Post.Application;

public sealed class PostNotificationUseCase(IPostStore postStore, PostResponseMapper mapper)
{
    private readonly IPostStore _postStore = postStore ?? throw new ArgumentNullException(nameof(postStore));
    private readonly PostResponseMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<NotificationListResponse> GetNotificationsAsync(Guid recipientId, int? limit, CancellationToken cancellationToken)
    {
        if (recipientId == Guid.Empty)
        {
            return new NotificationListResponse([], 0, 0);
        }

        var boundedLimit = Math.Clamp(limit ?? PostSearchCriteria.DefaultLimit, 1, PostSearchCriteria.MaxLimit);
        var notifications = await _postStore.GetNotificationsAsync(recipientId, boundedLimit, cancellationToken);
        var unreadCount = await _postStore.CountUnreadNotificationsAsync(recipientId, cancellationToken);
        return new NotificationListResponse(notifications.Select(_mapper.MapNotification).ToArray(), boundedLimit, unreadCount);
    }

    public async Task<NotificationMutationResult> MarkNotificationReadAsync(Guid notificationId, Guid recipientId, CancellationToken cancellationToken)
    {
        if (notificationId == Guid.Empty || recipientId == Guid.Empty)
        {
            return new NotificationMutationResult(PostOperationStatus.Invalid, null, "Notification id and recipient id are required.");
        }

        var notification = await _postStore.MarkNotificationReadAsync(notificationId, recipientId, cancellationToken);
        return notification is null
            ? new NotificationMutationResult(PostOperationStatus.NotFound, null, "The requested notification was not found.")
            : new NotificationMutationResult(PostOperationStatus.Updated, _mapper.MapNotification(notification), null);
    }
}
