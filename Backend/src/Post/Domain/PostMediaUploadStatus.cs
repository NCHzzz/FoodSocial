namespace FoodSocial.Post.Domain;

/// <summary>
/// Represents the lifecycle state of an uploaded media asset before it is attached to a post.
/// </summary>
public enum PostMediaUploadStatus
{
    StoredPendingAttachment = 0,
    Attached = 1,
    CleanupDue = 2,
    Deleted = 3
}
