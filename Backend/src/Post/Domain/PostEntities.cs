namespace FoodSocial.Post.Domain;

public enum PostStatus { Published, Hidden, Deleted }
public enum CommentStatus { Visible, Hidden, Deleted }
public enum ReportStatus { Submitted, ResolvedNoAction, ResolvedContentAction }
public enum ReportTargetType { Post, Comment }
public enum NotificationStatus { Unread, Read }

public sealed class PostRecord
{
    private PostRecord() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AuthorId { get; private set; }
    public Guid DishId { get; private set; }
    public Guid? PlaceId { get; private set; }
    public string Caption { get; private set; } = string.Empty;
    public int? Rating { get; private set; }
    public string? PriceRange { get; private set; }
    public List<string> Tags { get; private set; } = [];
    public string TagsSearchText { get; private set; } = string.Empty;
    public string? LocationText { get; private set; }
    public PostStatus Status { get; private set; } = PostStatus.Published;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public List<PostMediaRecord> Media { get; private set; } = [];
    public List<PostLikeRecord> Likes { get; private set; } = [];
    public List<PostCommentRecord> Comments { get; private set; } = [];
    public List<PostRepostRecord> Reposts { get; private set; } = [];

    public static PostRecord Create(Guid authorId, Guid dishId, string caption, IEnumerable<PostMediaRecord> media, Guid? placeId, int? rating, string? priceRange, IEnumerable<string>? tags, string? locationText)
    {
        if (authorId == Guid.Empty) throw new ArgumentException("Author id is required.", nameof(authorId));
        if (dishId == Guid.Empty) throw new ArgumentException("Dish id is required.", nameof(dishId));
        if (string.IsNullOrWhiteSpace(caption)) throw new ArgumentException("Caption is required.", nameof(caption));
        if (rating is < 1 or > 5) throw new ArgumentException("Rating must be between 1 and 5.", nameof(rating));
        var mediaList = media.ToList();
        if (mediaList.Count == 0 || mediaList.All(item => !item.IsPrimary)) throw new ArgumentException("At least one primary media URL is required.", nameof(media));
        var now = DateTimeOffset.UtcNow;
        var normalizedTags = (tags ?? []).Select(NormalizeTag).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
        return new PostRecord
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            DishId = dishId,
            PlaceId = placeId == Guid.Empty ? null : placeId,
            Caption = caption.Trim(),
            Rating = rating,
            PriceRange = string.IsNullOrWhiteSpace(priceRange) ? null : priceRange.Trim(),
            Tags = normalizedTags,
            TagsSearchText = string.Join(' ', normalizedTags),
            LocationText = string.IsNullOrWhiteSpace(locationText) ? null : locationText.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Media = mediaList
        };
    }

    public void Hide() { Status = PostStatus.Hidden; UpdatedAtUtc = DateTimeOffset.UtcNow; }
    public bool IsHidden => Status == PostStatus.Hidden || Status == PostStatus.Deleted;
    private static string NormalizeTag(string tag) => tag.Trim().TrimStart('#').ToLowerInvariant();
}

public sealed class PostMediaRecord
{
    private PostMediaRecord() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PostId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string Type { get; private set; } = "image";
    public string? ThumbnailUrl { get; private set; }
    public string? Caption { get; private set; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }
    public static PostMediaRecord Create(string url, string type, string? thumbnailUrl, string? caption, bool isPrimary, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("Media URL is required.", nameof(url));
        return new PostMediaRecord { Id = Guid.NewGuid(), Url = url.Trim(), Type = string.IsNullOrWhiteSpace(type) ? "image" : type.Trim().ToLowerInvariant(), ThumbnailUrl = string.IsNullOrWhiteSpace(thumbnailUrl) ? null : thumbnailUrl.Trim(), Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim(), IsPrimary = isPrimary, SortOrder = sortOrder };
    }
}

public sealed class PostLikeRecord
{
    public Guid PostId { get; set; }
    public Guid ActorId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PostRepostRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Guid ActorId { get; set; }
    public string? Caption { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PostCommentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;
    public CommentStatus Status { get; set; } = CommentStatus.Visible;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public bool IsVisible => Status == CommentStatus.Visible;
}

public sealed class ContentReportRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public Guid ReporterId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Submitted;
    public Guid? ModeratorId { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAtUtc { get; set; }
}

public sealed class PostNotificationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipientId { get; set; }
    public Guid ActorId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public Guid? PostId { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAtUtc { get; set; }

    public void MarkRead()
    {
        if (Status == NotificationStatus.Read) return;
        Status = NotificationStatus.Read;
        ReadAtUtc = DateTimeOffset.UtcNow;
    }
}
