namespace FoodSocial.Post.Domain;

/// <summary>
/// Represents a repost of an existing post by an actor.
/// </summary>
public sealed class PostRepostRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Guid ActorId { get; set; }
    public string? Caption { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
