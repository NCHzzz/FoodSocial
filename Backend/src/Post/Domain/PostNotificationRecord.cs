namespace FoodSocial.Post.Domain;

/// <summary>
/// Represents a post-owned notification surfaced to a recipient (follow, like, comment, repost, moderation).
/// </summary>
public sealed class PostNotificationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipientId { get; set; }
    public Guid ActorId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public Guid? PostId { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAtUtc { get; set; }

    public void MarkRead()
    {
        if (Status == NotificationStatus.Read) return;
        Status = NotificationStatus.Read;
        ReadAtUtc = DateTimeOffset.UtcNow;
    }
}
