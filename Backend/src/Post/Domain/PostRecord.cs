namespace FoodSocial.Post.Domain;

/// <summary>
/// Represents the canonical Post entity owned by the Post service boundary.
/// </summary>
public sealed class PostRecord
{
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private PostRecord() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; } = DefaultTenantId;
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
            TagsSearchText = PostSearchText.BuildTagsSearchText(normalizedTags),
            LocationText = string.IsNullOrWhiteSpace(locationText) ? null : locationText.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Media = mediaList
        };
    }

    public void Hide()
    {
        Status = PostStatus.Hidden;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public bool IsHidden => Status == PostStatus.Hidden || Status == PostStatus.Deleted;

    private static string NormalizeTag(string tag) => tag.Trim().TrimStart('#').ToLowerInvariant();
}
