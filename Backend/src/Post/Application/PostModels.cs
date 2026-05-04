using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Domain;

namespace FoodSocial.Post.Application;

public enum PostOperationStatus { Created, Updated, Deleted, NotFound, Invalid, Forbidden, Resolved }
public sealed record PostMutationResult(PostOperationStatus Status, PostDetailResponse? Post, string? Error);
public sealed record CommentMutationResult(PostOperationStatus Status, PostCommentResponse? Comment, string? Error);
public sealed record ReportMutationResult(PostOperationStatus Status, ContentReportResponse? Report, string? Error);

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
    Task<PostRecord?> GetPostAsync(Guid postId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PostRecord>> SearchPostsAsync(PostSearchCriteria criteria, CancellationToken cancellationToken);
    Task<IReadOnlyList<PostRecord>> GetDiscoverFeedAsync(DiscoverFeedCriteria criteria, CancellationToken cancellationToken);
    Task<int?> AddLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken);
    Task<int?> RemoveLikeAsync(Guid postId, Guid actorId, CancellationToken cancellationToken);
    Task<PostCommentRecord?> AddCommentAsync(Guid postId, Guid authorId, string body, CancellationToken cancellationToken);
    Task<PostCommentRecord?> GetCommentAsync(Guid commentId, CancellationToken cancellationToken);
    Task<PostCommentRecord?> SoftDeleteCommentAsync(Guid commentId, Guid actorId, CancellationToken cancellationToken);
    Task<ContentReportRecord?> CreateReportAsync(ReportTargetType targetType, Guid targetId, Guid reporterId, string reason, string? description, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentReportRecord>> GetReportsAsync(int limit, CancellationToken cancellationToken);
    Task<ContentReportRecord?> ResolveReportAsync(Guid reportId, Guid moderatorId, string action, string auditNote, CancellationToken cancellationToken);
}
