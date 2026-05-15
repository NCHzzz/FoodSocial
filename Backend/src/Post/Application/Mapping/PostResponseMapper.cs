using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Domain;

namespace FoodSocial.Post.Application.Mapping;

public sealed class PostResponseMapper(IPostStore postStore)
{
    private readonly IPostStore _postStore = postStore ?? throw new ArgumentNullException(nameof(postStore));

    public PostDetailResponse MapDetail(PostRecord post) =>
        new(
            post.Id,
            post.AuthorId,
            post.DishId,
            post.PlaceId,
            post.Caption,
            post.Rating,
            post.PriceRange,
            post.Tags,
            post.LocationText,
            post.Status.ToString(),
            post.IsHidden,
            post.Likes.Count,
            post.Reposts.Count,
            post.Comments.Count(comment => comment.IsVisible),
            post.Media.OrderBy(media => media.SortOrder).Select(MapMedia).ToArray(),
            post.Comments
                .Where(comment => comment.IsVisible)
                .OrderBy(comment => comment.CreatedAtUtc)
                .Select(MapComment)
                .ToArray(),
            post.CreatedAtUtc,
            post.UpdatedAtUtc);

    public PostSummaryResponse MapSummary(PostRecord post) =>
        new(
            post.Id,
            post.AuthorId,
            post.DishId,
            post.PlaceId,
            post.Caption,
            post.Rating,
            post.PriceRange,
            post.Tags,
            post.Media.OrderBy(media => media.SortOrder).FirstOrDefault(media => media.IsPrimary)?.Url,
            post.IsHidden,
            post.Likes.Count,
            post.Reposts.Count,
            post.Comments.Count(comment => comment.IsVisible),
            post.CreatedAtUtc);

    public PostMediaResponse MapMedia(PostMediaRecord media) =>
        new(media.Id, media.Url, media.Type, media.ThumbnailUrl, media.Caption, media.IsPrimary, media.SortOrder);

    public PostCommentResponse MapComment(PostCommentRecord comment) =>
        new(comment.Id, comment.PostId, comment.AuthorId, comment.Body, comment.Status.ToString(), comment.CreatedAtUtc, comment.UpdatedAtUtc);

    public async Task<PostRepostResponse> MapRepostAsync(PostRepostRecord repost, CancellationToken cancellationToken) =>
        new(
            repost.Id,
            repost.PostId,
            repost.ActorId,
            repost.Caption,
            await _postStore.CountRepostsAsync(repost.PostId, cancellationToken),
            repost.CreatedAtUtc);

    public ContentReportResponse MapReport(ContentReportRecord report) =>
        new(
            report.Id,
            report.TargetType.ToString(),
            report.TargetId,
            report.ReporterId,
            report.Reason,
            report.Description,
            report.Status.ToString(),
            report.ModeratorId,
            report.ResolutionNote,
            report.CreatedAtUtc,
            report.ResolvedAtUtc);

    public NotificationResponse MapNotification(PostNotificationRecord notification) =>
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
}
