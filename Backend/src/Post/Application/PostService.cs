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
            var post = PostRecord.Create(request.AuthorId, request.DishId, request.Caption, media, request.PlaceId, request.Rating, request.PriceRange, request.Tags, request.LocationText);
            var created = await _postStore.CreatePostAsync(post, cancellationToken);
            return new PostMutationResult(PostOperationStatus.Created, MapDetail(created), null);
        }
        catch (ArgumentException exception)
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

    private async Task<ReportMutationResult> CreateReportAsync(ReportTargetType targetType, Guid targetId, CreateContentReportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (targetId == Guid.Empty || request.ReporterId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason)) return new ReportMutationResult(PostOperationStatus.Invalid, null, "Target id, reporter id, and reason are required.");
        var report = await _postStore.CreateReportAsync(targetType, targetId, request.ReporterId, request.Reason.Trim(), request.Description, cancellationToken);
        return report is null ? new ReportMutationResult(PostOperationStatus.NotFound, null, "The requested report target was not found.") : new ReportMutationResult(PostOperationStatus.Created, MapReport(report), null);
    }

    private static PostDetailResponse MapDetail(PostRecord post) => new(post.Id, post.AuthorId, post.DishId, post.PlaceId, post.Caption, post.Rating, post.PriceRange, post.Tags, post.LocationText, post.Status.ToString(), post.IsHidden, post.Likes.Count, post.Comments.Count(comment => comment.IsVisible), post.Media.OrderBy(media => media.SortOrder).Select(MapMedia).ToArray(), post.Comments.Where(comment => comment.IsVisible).OrderBy(comment => comment.CreatedAtUtc).Select(MapComment).ToArray(), post.CreatedAtUtc, post.UpdatedAtUtc);
    private static PostSummaryResponse MapSummary(PostRecord post) => new(post.Id, post.AuthorId, post.DishId, post.PlaceId, post.Caption, post.Rating, post.PriceRange, post.Tags, post.Media.OrderBy(media => media.SortOrder).FirstOrDefault(media => media.IsPrimary)?.Url, post.IsHidden, post.Likes.Count, post.Comments.Count(comment => comment.IsVisible), post.CreatedAtUtc);
    private static PostMediaResponse MapMedia(PostMediaRecord media) => new(media.Id, media.Url, media.Type, media.ThumbnailUrl, media.Caption, media.IsPrimary, media.SortOrder);
    private static PostCommentResponse MapComment(PostCommentRecord comment) => new(comment.Id, comment.PostId, comment.AuthorId, comment.Body, comment.Status.ToString(), comment.CreatedAtUtc, comment.UpdatedAtUtc);
    private static ContentReportResponse MapReport(ContentReportRecord report) => new(report.Id, report.TargetType.ToString(), report.TargetId, report.ReporterId, report.Reason, report.Description, report.Status.ToString(), report.ModeratorId, report.ResolutionNote, report.CreatedAtUtc, report.ResolvedAtUtc);
}
