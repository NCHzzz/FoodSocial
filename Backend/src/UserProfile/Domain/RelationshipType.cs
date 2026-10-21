namespace FoodSocial.UserProfile.Domain;

/// <summary>
/// Represents the type of a user-to-user relationship within the UserProfile boundary.
/// </summary>
public enum RelationshipType
{
    Follow,
    Block,
    Mute
}
