namespace FoodSocial.UserProfile.Application.DTOs;

/// <summary>
/// Represents an application-owned profile snapshot.
/// </summary>
public sealed record UserProfileSnapshot(
    Guid IdentityId,
    string Subject,
    string? DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyList<string> PreferredCuisines,
    IReadOnlyList<string> DislikedIngredients,
    IReadOnlyList<string> DietaryRestrictions,
    long FollowerCount,
    long FollowingCount);

/// <summary>
/// Represents profile ownership data used for authorization-sensitive operations.
/// </summary>
public sealed record UserProfileOwnership(
    Guid ShellId,
    Guid IdentityId,
    string Subject);

/// <summary>
/// Represents a compact user profile summary.
/// </summary>
public sealed record UserProfileSummary(
    Guid IdentityId,
    string? DisplayName,
    string? AvatarUrl);

/// <summary>
/// Represents a bounded user-profile connections list.
/// </summary>
public sealed record UserProfileConnectionsSnapshot(
    IReadOnlyList<UserProfileSummary> Items,
    long TotalCount);

/// <summary>
/// Represents a saved post reference owned by a profile.
/// </summary>
public sealed record SavedPostSnapshot(Guid PostId, DateTimeOffset SavedAtUtc);

/// <summary>
/// Represents a saved dish reference owned by a profile.
/// </summary>
public sealed record SavedDishSnapshot(Guid DishId, DateTimeOffset SavedAtUtc);

/// <summary>
/// Represents a saved place reference owned by a profile.
/// </summary>
public sealed record SavedPlaceSnapshot(Guid PlaceId, DateTimeOffset SavedAtUtc);

/// <summary>
/// Represents the owner-scoped saved-items snapshot.
/// </summary>
public sealed record UserSavedItemsSnapshot(
    IReadOnlyList<SavedPostSnapshot> Posts,
    IReadOnlyList<SavedDishSnapshot> Dishes,
    IReadOnlyList<SavedPlaceSnapshot> Places);

/// <summary>
/// Represents the status of an application-owned mutation.
/// </summary>
public enum UserProfileOperationStatus
{
    Success,
    NotFound,
    Forbidden,
    Invalid
}

/// <summary>
/// Represents the input for updating a user profile.
/// </summary>
public sealed record UpdateUserProfileCommand(
    string? DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyList<string> PreferredCuisines,
    IReadOnlyList<string> DislikedIngredients,
    IReadOnlyList<string> DietaryRestrictions);

/// <summary>
/// Represents the result of a profile update operation.
/// </summary>
public sealed record UserProfileUpdateResult(
    UserProfileOperationStatus Status,
    UserProfileSnapshot? Profile,
    string? Error);

/// <summary>
/// Represents the result of a follow or unfollow operation.
/// </summary>
public sealed record UserProfileMutationResult(
    UserProfileOperationStatus Status,
    Guid TargetIdentityId,
    bool Following,
    string? Error);

/// <summary>
/// Represents the result of a save or unsave operation.
/// </summary>
public sealed record UserSavedItemMutationResult(
    UserProfileOperationStatus Status,
    Guid OwnerIdentityId,
    Guid TargetId,
    string TargetType,
    bool Saved,
    string? Error);

/// <summary>
/// Represents the result of loading owner-scoped saved items.
/// </summary>
public sealed record UserSavedItemsResult(
    UserProfileOperationStatus Status,
    UserSavedItemsSnapshot? SavedItems,
    string? Error);


public sealed record UserNotificationSnapshot(
    Guid Id,
    Guid RecipientIdentityId,
    Guid ActorIdentityId,
    string Type,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

public sealed record UserNotificationListSnapshot(IReadOnlyList<UserNotificationSnapshot> Items, int Limit, int UnreadCount);
