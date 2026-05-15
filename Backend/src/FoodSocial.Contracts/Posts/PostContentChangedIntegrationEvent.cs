namespace FoodSocial.Contracts.Posts;

public sealed record PostContentChangedIntegrationEvent(
    Guid EventId,
    int EventVersion,
    DateTimeOffset OccurredAtUtc,
    string? CorrelationId,
    Guid PostId,
    Guid TenantId,
    Guid AuthorId,
    Guid DishId,
    Guid? PlaceId,
    string ChangeType)
{
    public const int CurrentVersion = 1;

    public static PostContentChangedIntegrationEvent Create(
        Guid postId,
        Guid tenantId,
        Guid authorId,
        Guid dishId,
        Guid? placeId,
        string changeType) =>
        new(
            Guid.NewGuid(),
            CurrentVersion,
            DateTimeOffset.UtcNow,
            null,
            postId,
            tenantId,
            authorId,
            dishId,
            placeId,
            changeType);
}
