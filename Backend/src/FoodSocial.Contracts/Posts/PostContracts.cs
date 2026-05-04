namespace FoodSocial.Contracts.Posts;

public sealed record CreatePostRequest(
    Guid AuthorId,
    Guid DishId,
    string Caption,
    IReadOnlyList<PostMediaRequest> Media,
    Guid? PlaceId = null,
    int? Rating = null,
    string? PriceRange = null,
    IReadOnlyList<string>? Tags = null,
    string? LocationText = null);

public sealed record PostMediaRequest(string Url, string Type = "image", string? ThumbnailUrl = null, string? Caption = null, bool IsPrimary = false);

public sealed record PostMediaResponse(Guid Id, string Url, string Type, string? ThumbnailUrl, string? Caption, bool IsPrimary, int SortOrder);

public sealed record MediaUploadResponse(
    Guid UploadId,
    string Url,
    string ThumbnailUrl,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CleanupDueAtUtc,
    string Status);

public sealed record PostDetailResponse(
    Guid Id,
    Guid AuthorId,
    Guid DishId,
    Guid? PlaceId,
    string Caption,
    int? Rating,
    string? PriceRange,
    IReadOnlyList<string> Tags,
    string? LocationText,
    string Status,
    bool IsHidden,
    int LikeCount,
    int RepostCount,
    int CommentCount,
    IReadOnlyList<PostMediaResponse> Media,
    IReadOnlyList<PostCommentResponse> Comments,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PostSummaryResponse(
    Guid Id,
    Guid AuthorId,
    Guid DishId,
    Guid? PlaceId,
    string Caption,
    int? Rating,
    string? PriceRange,
    IReadOnlyList<string> Tags,
    string? PrimaryMediaUrl,
    bool IsHidden,
    int LikeCount,
    int RepostCount,
    int CommentCount,
    DateTimeOffset CreatedAtUtc);

public sealed record PostSearchResponse(IReadOnlyList<PostSummaryResponse> Items, int Limit);

public sealed record DiscoverFeedResponse(IReadOnlyList<PostSummaryResponse> Items, int Limit);

public sealed record ActorRequest(Guid ActorId);

public sealed record PostLikeResponse(Guid PostId, Guid ActorId, bool Liked, int LikeCount);

public sealed record CreatePostRepostRequest(Guid ActorId, string? Caption = null);

public sealed record PostRepostResponse(Guid Id, Guid PostId, Guid ActorId, string? Caption, int RepostCount, DateTimeOffset CreatedAtUtc);

public sealed record CreatePostCommentRequest(Guid AuthorId, string Body);

public sealed record DeletePostCommentRequest(Guid ActorId);

public sealed record PostCommentResponse(Guid Id, Guid PostId, Guid AuthorId, string Body, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

public sealed record CreateContentReportRequest(Guid ReporterId, string Reason, string? Description = null);

public sealed record ContentReportResponse(
    Guid Id,
    string TargetType,
    Guid TargetId,
    Guid ReporterId,
    string Reason,
    string? Description,
    string Status,
    Guid? ModeratorId,
    string? ResolutionNote,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ResolvedAtUtc);

public sealed record AdminReportsResponse(IReadOnlyList<ContentReportResponse> Items, int Limit);

public sealed record ResolveContentReportRequest(Guid ModeratorId, string Action, string AuditNote);

public sealed record NotificationResponse(
    Guid Id,
    Guid RecipientId,
    Guid ActorId,
    string Type,
    string TargetType,
    Guid TargetId,
    Guid? PostId,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

public sealed record NotificationListResponse(IReadOnlyList<NotificationResponse> Items, int Limit, int UnreadCount);
