namespace FoodSocial.Post.Domain;

/// <summary>
/// Represents an uploaded media asset before it is attached to a post.
/// Owns the upload lifecycle (pending -> attached -> cleanup-due -> deleted).
/// </summary>
public sealed class PostMediaUploadRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; } = PostRecord.DefaultTenantId;
    public Guid OwnerId { get; set; }
    public Guid? AttachedPostId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public PostMediaUploadStatus Status { get; set; } = PostMediaUploadStatus.StoredPendingAttachment;
    public bool ModerationHold { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CleanupDueAtUtc { get; set; }
    public DateTimeOffset? AttachedAtUtc { get; set; }

    public void MarkAttached(Guid postId)
    {
        if (postId == Guid.Empty) throw new ArgumentException("Post id is required.", nameof(postId));
        AttachedPostId = postId;
        Status = PostMediaUploadStatus.Attached;
        AttachedAtUtc = DateTimeOffset.UtcNow;
    }
}
