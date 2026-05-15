using FoodSocial.Post.Domain;

namespace FoodSocial.Post.Application.Interfaces.Repositories;

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
    Task<IReadOnlyList<PostMediaUploadRecord>> ClaimMediaUploadsDueForCleanupAsync(DateTimeOffset utcNow, int limit, CancellationToken cancellationToken);
    Task MarkMediaUploadDeletedAsync(Guid uploadId, CancellationToken cancellationToken);
    Task<IReadOnlySet<string>> FindAttachableMediaUploadUrlsAsync(Guid ownerId, IReadOnlyList<PostMediaUploadAttachmentRequest> mediaUploads, CancellationToken cancellationToken);
}
