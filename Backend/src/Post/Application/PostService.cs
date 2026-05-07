using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Domain;

namespace FoodSocial.Post.Application;

public sealed class PostService(IPostStore postStore, IPostCatalogReferenceValidator catalogReferenceValidator)
{
    private readonly IPostStore _postStore = postStore ?? throw new ArgumentNullException(nameof(postStore));
    private readonly IPostCatalogReferenceValidator _catalogReferenceValidator = catalogReferenceValidator ?? throw new ArgumentNullException(nameof(catalogReferenceValidator));

    public async Task<PostMutationResult> CreatePostAsync(CreatePostRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var catalogValidation = await _catalogReferenceValidator.ValidateReferencesAsync(request.DishId, request.PlaceId, cancellationToken);
        if (!catalogValidation.IsValid)
        {
            return new PostMutationResult(PostOperationStatus.Invalid, null, catalogValidation.Error);
        }

        try
        {
            var media = request.Media.Select((item, index) => PostMediaRecord.Create(item.Url, item.Type, item.ThumbnailUrl, item.Caption, item.IsPrimary || index == 0, index)).ToArray();
            if (media.Length > 0 && media.All(item => !item.IsPrimary)) media[0] = PostMediaRecord.Create(media[0].Url, media[0].Type, media[0].ThumbnailUrl, media[0].Caption, true, media[0].SortOrder);
            var localUploadValidationError = ValidateLocalMediaUploadReferences(media, out var localUploadAttachments);
            if (localUploadValidationError is not null)
            {
                return new PostMutationResult(PostOperationStatus.Invalid, null, localUploadValidationError);
            }

            if (localUploadAttachments.Count > 0)
            {
                var attachableUrls = await _postStore.FindAttachableMediaUploadUrlsAsync(request.AuthorId, localUploadAttachments, cancellationToken);
                var missingUrls = localUploadAttachments.Where(upload => !attachableUrls.Contains(upload.Url)).ToArray();
                if (missingUrls.Length > 0)
                {
                    return new PostMutationResult(PostOperationStatus.Invalid, null, "Uploaded media must exist, belong to the author, and be pending attachment before publish.");
                }
            }

            var post = PostRecord.Create(request.AuthorId, request.DishId, request.Caption, media, request.PlaceId, request.Rating, request.PriceRange, request.Tags, request.LocationText);
            var created = localUploadAttachments.Count > 0
                ? await _postStore.CreatePostWithMediaUploadsAsync(post, localUploadAttachments, cancellationToken)
                : await _postStore.CreatePostAsync(post, cancellationToken);
            return new PostMutationResult(PostOperationStatus.Created, MapDetail(created), null);
        }
        catch (ArgumentException exception)
        {
            return new PostMutationResult(PostOperationStatus.Invalid, null, exception.Message);
        }
        catch (PostMediaUploadAttachmentException exception)
        {
            return new PostMutationResult(PostOperationStatus.Invalid, null, exception.Message);
        }
    }

    public async Task<PostDetailResponse?> GetPostAsync(Guid postId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(postId, Guid.Empty);
        var post = await _postStore.GetPostAsync(postId, cancellationToken);
        return post is null ? null : MapDetail(post);
    }

    public async Task<PostMediaUploadRecord?> GetDeliverableMediaUploadAsync(Guid uploadId, Guid? callerIdentityId, CancellationToken cancellationToken)
    {
        if (uploadId == Guid.Empty)
        {
            return null;
        }

        return await _postStore.GetDeliverableMediaUploadAsync(uploadId, callerIdentityId, cancellationToken);
    }

    public async Task<PostSearchResponse> SearchPostsAsync(PostSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var posts = await _postStore.SearchPostsAsync(criteria, cancellationToken);
        return new PostSearchResponse(posts.Select(MapSummary).ToArray(), criteria.Limit);
    }

    public async Task<DiscoverFeedResponse> GetDiscoverFeedAsync(DiscoverFeedCriteria criteria, CancellationToken cancellationToken)
    {
        var posts = await _postStore.GetDiscoverFeedAsync(criteria, cancellationToken);
        return new DiscoverFeedResponse(posts.Select(MapSummary).ToArray(), criteria.Limit);
    }

    public async Task<PostLikeResponse?> AddLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken)
    {
        if (postId == Guid.Empty || actorId == Guid.Empty) return null;
        var likeCount = await _postStore.AddLikeAsync(postId, actorId, cancellationToken);
        return likeCount is null ? null : new PostLikeResponse(postId, actorId, true, likeCount.Value);
    }

    public async Task<PostLikeResponse?> RemoveLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken)
    {
        if (postId == Guid.Empty || actorId == Guid.Empty) return null;
        var likeCount = await _postStore.RemoveLikeAsync(postId, actorId, cancellationToken);
        return likeCount is null ? null : new PostLikeResponse(postId, actorId, false, likeCount.Value);
    }

    public async Task<CommentMutationResult> AddCommentAsync(Guid postId, CreatePostCommentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (postId == Guid.Empty || request.AuthorId == Guid.Empty || string.IsNullOrWhiteSpace(request.Body)) return new CommentMutationResult(PostOperationStatus.Invalid, null, "Post id, author id, and comment body are required.");
        var comment = await _postStore.AddCommentAsync(postId, request.AuthorId, request.Body.Trim(), cancellationToken);
        return comment is null ? new CommentMutationResult(PostOperationStatus.NotFound, null, "The requested post was not found.") : new CommentMutationResult(PostOperationStatus.Created, MapComment(comment), null);
    }

    public async Task<RepostMutationResult> AddRepostAsync(Guid postId, CreatePostRepostRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (postId == Guid.Empty || request.ActorId == Guid.Empty)
        {
            return new RepostMutationResult(PostOperationStatus.Invalid, null, "Post id and actor id are required.");
        }

        if (request.Caption?.Trim() is { Length: > 280 })
        {
            return new RepostMutationResult(PostOperationStatus.Invalid, null, "Repost caption must be 280 characters or fewer.");
        }

        var repost = await _postStore.AddRepostAsync(postId, request.ActorId, request.Caption, cancellationToken);
        return repost is null
            ? new RepostMutationResult(PostOperationStatus.NotFound, null, "The requested post was not found.")
            : new RepostMutationResult(PostOperationStatus.Created, await MapRepostAsync(repost, cancellationToken), null);
    }

    public async Task<CommentMutationResult> DeleteCommentAsync(Guid commentId, Guid actorId, CancellationToken cancellationToken)
    {
        if (commentId == Guid.Empty || actorId == Guid.Empty) return new CommentMutationResult(PostOperationStatus.Invalid, null, "Comment id and actor id are required.");
        var existing = await _postStore.GetCommentAsync(commentId, cancellationToken);
        if (existing is null) return new CommentMutationResult(PostOperationStatus.NotFound, null, "The requested comment was not found.");
        if (existing.AuthorId != actorId) return new CommentMutationResult(PostOperationStatus.Forbidden, null, "Only the comment owner can delete the comment.");
        var deleted = await _postStore.SoftDeleteCommentAsync(commentId, actorId, cancellationToken);
        return new CommentMutationResult(PostOperationStatus.Deleted, MapComment(deleted!), null);
    }

    public async Task<ReportMutationResult> ReportPostAsync(Guid postId, CreateContentReportRequest request, CancellationToken cancellationToken) =>
        await CreateReportAsync(ReportTargetType.Post, postId, request, cancellationToken);

    public async Task<ReportMutationResult> ReportCommentAsync(Guid commentId, CreateContentReportRequest request, CancellationToken cancellationToken) =>
        await CreateReportAsync(ReportTargetType.Comment, commentId, request, cancellationToken);

    public async Task<AdminReportsResponse> GetReportsAsync(int? limit, CancellationToken cancellationToken)
    {
        var boundedLimit = Math.Clamp(limit ?? PostSearchCriteria.DefaultLimit, 1, PostSearchCriteria.MaxLimit);
        var reports = await _postStore.GetReportsAsync(boundedLimit, cancellationToken);
        return new AdminReportsResponse(reports.Select(MapReport).ToArray(), boundedLimit);
    }

    public async Task<ReportMutationResult> ResolveReportAsync(Guid reportId, ResolveContentReportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (reportId == Guid.Empty || request.ModeratorId == Guid.Empty || string.IsNullOrWhiteSpace(request.Action) || string.IsNullOrWhiteSpace(request.AuditNote)) return new ReportMutationResult(PostOperationStatus.Invalid, null, "Report id, moderator id, action, and audit note are required.");
        var report = await _postStore.ResolveReportAsync(reportId, request.ModeratorId, request.Action.Trim(), request.AuditNote.Trim(), cancellationToken);
        return report is null ? new ReportMutationResult(PostOperationStatus.NotFound, null, "The requested report was not found.") : new ReportMutationResult(PostOperationStatus.Resolved, MapReport(report), null);
    }

    public async Task<NotificationListResponse> GetNotificationsAsync(Guid recipientId, int? limit, CancellationToken cancellationToken)
    {
        if (recipientId == Guid.Empty)
        {
            return new NotificationListResponse([], 0, 0);
        }

        var boundedLimit = Math.Clamp(limit ?? PostSearchCriteria.DefaultLimit, 1, PostSearchCriteria.MaxLimit);
        var notifications = await _postStore.GetNotificationsAsync(recipientId, boundedLimit, cancellationToken);
        var unreadCount = await _postStore.CountUnreadNotificationsAsync(recipientId, cancellationToken);
        return new NotificationListResponse(notifications.Select(MapNotification).ToArray(), boundedLimit, unreadCount);
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
            : new NotificationMutationResult(PostOperationStatus.Updated, MapNotification(notification), null);
    }

    public async Task RecordMediaUploadAsync(RegisterMediaUploadCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.UploadId == Guid.Empty || command.OwnerId == Guid.Empty)
        {
            throw new ArgumentException("Upload id and owner id are required.", nameof(command));
        }

        await _postStore.RegisterMediaUploadAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<PostMediaUploadRecord>> ClaimMediaUploadsDueForCleanupAsync(DateTimeOffset utcNow, int? limit, CancellationToken cancellationToken)
    {
        var boundedLimit = Math.Clamp(limit ?? 100, 1, 500);
        return await _postStore.ClaimMediaUploadsDueForCleanupAsync(utcNow, boundedLimit, cancellationToken);
    }

    public async Task MarkMediaUploadDeletedAsync(Guid uploadId, CancellationToken cancellationToken)
    {
        if (uploadId == Guid.Empty) throw new ArgumentException("Upload id is required.", nameof(uploadId));
        await _postStore.MarkMediaUploadDeletedAsync(uploadId, cancellationToken);
    }

    private async Task<ReportMutationResult> CreateReportAsync(ReportTargetType targetType, Guid targetId, CreateContentReportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (targetId == Guid.Empty || request.ReporterId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason)) return new ReportMutationResult(PostOperationStatus.Invalid, null, "Target id, reporter id, and reason are required.");
        var report = await _postStore.CreateReportAsync(targetType, targetId, request.ReporterId, request.Reason.Trim(), request.Description, cancellationToken);
        return report is null ? new ReportMutationResult(PostOperationStatus.NotFound, null, "The requested report target was not found.") : new ReportMutationResult(PostOperationStatus.Created, MapReport(report), null);
    }

    private static PostDetailResponse MapDetail(PostRecord post) => new(post.Id, post.AuthorId, post.DishId, post.PlaceId, post.Caption, post.Rating, post.PriceRange, post.Tags, post.LocationText, post.Status.ToString(), post.IsHidden, post.Likes.Count, post.Reposts.Count, post.Comments.Count(comment => comment.IsVisible), post.Media.OrderBy(media => media.SortOrder).Select(MapMedia).ToArray(), post.Comments.Where(comment => comment.IsVisible).OrderBy(comment => comment.CreatedAtUtc).Select(MapComment).ToArray(), post.CreatedAtUtc, post.UpdatedAtUtc);
    private static PostSummaryResponse MapSummary(PostRecord post) => new(post.Id, post.AuthorId, post.DishId, post.PlaceId, post.Caption, post.Rating, post.PriceRange, post.Tags, post.Media.OrderBy(media => media.SortOrder).FirstOrDefault(media => media.IsPrimary)?.Url, post.IsHidden, post.Likes.Count, post.Reposts.Count, post.Comments.Count(comment => comment.IsVisible), post.CreatedAtUtc);
    private static PostMediaResponse MapMedia(PostMediaRecord media) => new(media.Id, media.Url, media.Type, media.ThumbnailUrl, media.Caption, media.IsPrimary, media.SortOrder);
    private static PostCommentResponse MapComment(PostCommentRecord comment) => new(comment.Id, comment.PostId, comment.AuthorId, comment.Body, comment.Status.ToString(), comment.CreatedAtUtc, comment.UpdatedAtUtc);
    private async Task<PostRepostResponse> MapRepostAsync(PostRepostRecord repost, CancellationToken cancellationToken) =>
        new(
            repost.Id,
            repost.PostId,
            repost.ActorId,
            repost.Caption,
            await _postStore.CountRepostsAsync(repost.PostId, cancellationToken),
            repost.CreatedAtUtc);

    private static ContentReportResponse MapReport(ContentReportRecord report) => new(report.Id, report.TargetType.ToString(), report.TargetId, report.ReporterId, report.Reason, report.Description, report.Status.ToString(), report.ModeratorId, report.ResolutionNote, report.CreatedAtUtc, report.ResolvedAtUtc);

    private static NotificationResponse MapNotification(PostNotificationRecord notification) =>
        new(
            notification.Id,
            notification.RecipientId,
            notification.ActorId,
            notification.Type,
            notification.TargetType,
            notification.TargetId,
            notification.PostId,
            notification.Message,
            notification.Status == NotificationStatus.Read,
            notification.CreatedAtUtc,
            notification.ReadAtUtc);

    private static bool IsPostMediaUploadUrl(string url) =>
        url.StartsWith("/api/media/uploads/", StringComparison.Ordinal);

    private static string? ValidateLocalMediaUploadReferences(
        IReadOnlyList<PostMediaRecord> media,
        out IReadOnlyList<PostMediaUploadAttachmentRequest> localUploadAttachments)
    {
        var attachments = new Dictionary<string, PostMediaUploadAttachmentRequest>(StringComparer.Ordinal);
        foreach (var item in media)
        {
            var urlIsLocalUpload = IsPostMediaUploadUrl(item.Url);
            var thumbnailIsLocalUpload = item.ThumbnailUrl is not null && IsPostMediaUploadUrl(item.ThumbnailUrl);
            if (thumbnailIsLocalUpload && !urlIsLocalUpload)
            {
                localUploadAttachments = [];
                return "Local uploaded thumbnails must be paired with their original uploaded media URL.";
            }

            if (!urlIsLocalUpload)
            {
                continue;
            }

            if (!thumbnailIsLocalUpload)
            {
                localUploadAttachments = [];
                return "Local uploaded media must include its paired local thumbnail URL.";
            }

            var localThumbnailUrl = item.ThumbnailUrl!;
            if (attachments.TryGetValue(item.Url, out var existing)
                && !string.Equals(existing.ThumbnailUrl, localThumbnailUrl, StringComparison.Ordinal))
            {
                localUploadAttachments = [];
                return "Local uploaded media cannot be published with conflicting thumbnail references.";
            }

            attachments[item.Url] = new PostMediaUploadAttachmentRequest(item.Url, localThumbnailUrl);
        }

        localUploadAttachments = attachments.Values.ToArray();
        return null;
    }
}
