namespace FoodSocial.Post.Domain;

/// <summary>
/// Represents a comment on a post.
/// </summary>
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
