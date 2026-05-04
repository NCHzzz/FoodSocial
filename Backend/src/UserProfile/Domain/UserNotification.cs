namespace FoodSocial.UserProfile.Domain;

public enum UserNotificationStatus { Unread, Read }

public sealed class UserNotification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserProfileShellId { get; private set; }
    public Guid RecipientIdentityId { get; private set; }
    public Guid ActorIdentityId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public UserNotificationStatus Status { get; private set; } = UserNotificationStatus.Unread;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAtUtc { get; private set; }

    private UserNotification() { }

    public UserNotification(Guid userProfileShellId, Guid recipientIdentityId, Guid actorIdentityId, string type, string message)
    {
        if (userProfileShellId == Guid.Empty) throw new ArgumentException("User profile shell id is required.", nameof(userProfileShellId));
        if (recipientIdentityId == Guid.Empty) throw new ArgumentException("Recipient identity id is required.", nameof(recipientIdentityId));
        if (actorIdentityId == Guid.Empty) throw new ArgumentException("Actor identity id is required.", nameof(actorIdentityId));
        UserProfileShellId = userProfileShellId;
        RecipientIdentityId = recipientIdentityId;
        ActorIdentityId = actorIdentityId;
        Type = string.IsNullOrWhiteSpace(type) ? throw new ArgumentException("Notification type is required.", nameof(type)) : type.Trim();
        Message = string.IsNullOrWhiteSpace(message) ? throw new ArgumentException("Notification message is required.", nameof(message)) : message.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkRead()
    {
        if (Status == UserNotificationStatus.Read) return;
        Status = UserNotificationStatus.Read;
        ReadAtUtc = DateTimeOffset.UtcNow;
    }
}
