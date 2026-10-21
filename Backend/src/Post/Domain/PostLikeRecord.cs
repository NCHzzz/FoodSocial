namespace FoodSocial.Post.Domain;

/// <summary>
/// Represents a like on a post by an actor.
/// </summary>
public sealed class PostLikeRecord
{
    public Guid PostId { get; set; }
    public Guid ActorId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
