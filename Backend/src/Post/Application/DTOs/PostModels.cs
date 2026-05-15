using FoodSocial.Contracts.Posts;

namespace FoodSocial.Post.Application.DTOs;

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

public sealed record PostSearchCriteria(IReadOnlyList<Guid> AuthorIds, Guid? DishId, Guid? PlaceId, string? Tag, string? Query, bool IncludeHidden, int Limit)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;
    public static PostSearchCriteria Create(IReadOnlyList<Guid>? authorIds, Guid? dishId, Guid? placeId, string? tag, string? query, bool includeHidden, int? limit) =>
        new(
            NormalizeAuthorIds(authorIds),
            dishId,
            placeId,
            Normalize(tag),
            Normalize(query),
            includeHidden,
            Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit));
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimStart('#').ToLowerInvariant();
    private static IReadOnlyList<Guid> NormalizeAuthorIds(IReadOnlyList<Guid>? authorIds) =>
        authorIds is null
            ? []
            : authorIds.Where(id => id != Guid.Empty).Distinct().ToArray();
}

public sealed record DiscoverFeedCriteria(int Limit)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 100;
    public static DiscoverFeedCriteria Create(int? limit) => new(Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit));
}
