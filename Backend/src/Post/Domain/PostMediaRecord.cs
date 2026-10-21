namespace FoodSocial.Post.Domain;

/// <summary>
/// Represents an individual media asset attached to a post.
/// </summary>
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
        return new PostMediaRecord
        {
            Id = Guid.NewGuid(),
            Url = url.Trim(),
            Type = string.IsNullOrWhiteSpace(type) ? "image" : type.Trim().ToLowerInvariant(),
            ThumbnailUrl = string.IsNullOrWhiteSpace(thumbnailUrl) ? null : thumbnailUrl.Trim(),
            Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim(),
            IsPrimary = isPrimary,
            SortOrder = sortOrder
        };
    }
}
