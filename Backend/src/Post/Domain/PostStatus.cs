namespace FoodSocial.Post.Domain;

/// <summary>
/// Represents the lifecycle status of a Post owned by the Post service boundary.
/// </summary>
public enum PostStatus
{
    Published = 0,
    Hidden = 1,
    Deleted = 2
}
