using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Domain;

namespace FoodSocial.Post.Application;

public enum PostOperationStatus { Created, Updated, Deleted, NotFound, Invalid, Forbidden, Resolved }
public sealed record PostMutationResult(PostOperationStatus Status, PostDetailResponse? Post, string? Error);
public sealed record CommentMutationResult(PostOperationStatus Status, PostCommentResponse? Comment, string? Error);
public sealed record ReportMutationResult(PostOperationStatus Status, ContentReportResponse? Report, string? Error);
public sealed record RepostMutationResult(PostOperationStatus Status, PostRepostResponse? Repost, string? Error);
public sealed record NotificationMutationResult(PostOperationStatus Status, NotificationResponse? Notification, string? Error);
public sealed class PostMediaUploadAttachmentException(string message) : Exception(message);
public sealed record RegisterMediaUploadCommand(
    Guid UploadId,
    Guid OwnerId,
    string Url,
    string ThumbnailUrl,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CleanupDueAtUtc);
public sealed record PostMediaUploadAttachmentRequest(string Url, string ThumbnailUrl);

public sealed record PostSearchCriteria(Guid? AuthorId, Guid? DishId, Guid? PlaceId, string? Tag, string? Query, bool IncludeHidden, int Limit)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;
    public static PostSearchCriteria Create(Guid? authorId, Guid? dishId, Guid? placeId, string? tag, string? query, bool includeHidden, int? limit) =>
        new(authorId, dishId, placeId, Normalize(tag), Normalize(query), includeHidden, Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit));
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimStart('#').ToLowerInvariant();
}

public sealed record DiscoverFeedCriteria(int Limit)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;
    public static DiscoverFeedCriteria Create(int? limit) => new(Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit));
}

public interface IPostStore
{
    Task<PostRecord> CreatePostAsync(PostRecord post, CancellationToken cancellationToken);
    Task<PostRecord> CreatePostWithMediaUploadsAsync(PostRecord post, IReadOnlyList<PostMediaUploadAttachmentRequest> mediaUploadsToAttach, CancellationToken cancellationToken);
    Task<PostRecord?> GetPostAsync(Guid postId, CancellationToken cancellationToken);
    Task<PostMediaUploadRecord?> GetDeliverableMediaUploadAsync(Guid uploadId, Guid? callerIdentityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PostRecord>> SearchPostsAsync(PostSearchCriteria criteria, CancellationToken cancellationToken);
    Task<IReadOnlyList<PostRecord>> GetDiscoverFeedAsync(DiscoverFeedCriteria criteria, CancellationToken cancellationToken);
    Task<int?> AddLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken);
    Task<int?> RemoveLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken);
    Task<PostCommentRecord?> AddCommentAsync(Guid postId, Guid authorId, string body, CancellationToken cancellationToken);
    Task<PostCommentRecord?> GetCommentAsync(Guid commentId, CancellationToken cancellationToken);
    Task<PostCommentRecord?> SoftDeleteCommentAsync(Guid commentId, Guid actorId, CancellationToken cancellationToken);
    Task<PostRepostRecord?> AddRepostAsync(Guid postId, Guid actorId, string? caption, CancellationToken cancellationToken);
    Task<int> CountRepostsAsync(Guid postId, CancellationToken cancellationToken);
    Task<ContentReportRecord?> CreateReportAsync(ReportTargetType targetType, Guid targetId, Guid reporterId, string reason, string? description, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentReportRecord>> GetReportsAsync(int limit, CancellationToken cancellationToken);
    Task<ContentReportRecord?> ResolveReportAsync(Guid reportId, Guid moderatorId, string action, string auditNote, CancellationToken cancellationToken);
    Task<IReadOnlyList<PostNotificationRecord>> GetNotificationsAsync(Guid recipientId, int limit, CancellationToken cancellationToken);
    Task<int> CountUnreadNotificationsAsync(Guid recipientId, CancellationToken cancellationToken);
    Task<PostNotificationRecord?> MarkNotificationReadAsync(Guid notificationId, Guid recipientId, CancellationToken cancellationToken);
    Task RegisterMediaUploadAsync(RegisterMediaUploadCommand command, CancellationToken cancellationToken);
    Task<IReadOnlySet<string>> FindAttachableMediaUploadUrlsAsync(Guid ownerId, IReadOnlyList<PostMediaUploadAttachmentRequest> mediaUploads, CancellationToken cancellationToken);
}
