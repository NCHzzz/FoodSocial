namespace FoodSocial.Contracts.UserProfiles;

public sealed record UserNotificationResponse(
    Guid Id,
    Guid RecipientIdentityId,
    Guid ActorIdentityId,
    string Type,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

public sealed record UserNotificationListResponse(IReadOnlyList<UserNotificationResponse> Items, int Limit, int UnreadCount);
