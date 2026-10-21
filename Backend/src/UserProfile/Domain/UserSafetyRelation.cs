namespace FoodSocial.UserProfile.Domain;

/// <summary>
/// Represents a user-to-user relationship (block, mute) within the UserProfile boundary.
/// Follow relationships use the separate <c>UserFollow</c> entity.
/// </summary>
public sealed class UserSafetyRelation
{
    private UserSafetyRelation() { }

    /// <summary>
    /// Creates a new safety relationship (block or mute) between two users.
    /// </summary>
    public UserSafetyRelation(Guid actorIdentityId, Guid targetIdentityId, RelationshipType type)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(actorIdentityId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(targetIdentityId, Guid.Empty);

        if (actorIdentityId == targetIdentityId)
        {
            throw new ArgumentException("A user cannot target themselves.", nameof(targetIdentityId));
        }

        if (type != RelationshipType.Block && type != RelationshipType.Mute)
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Safety relationships must be of type Block or Mute.");
        }

        ActorIdentityId = actorIdentityId;
        TargetIdentityId = targetIdentityId;
        Type = type;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; } = UserProfileShell.DefaultTenantId;
    public Guid ActorIdentityId { get; private set; }
    public Guid TargetIdentityId { get; private set; }
    public RelationshipType Type { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
