namespace FoodSocial.Contracts.Dishes;

public sealed record DishCatalogChangedIntegrationEvent(
    Guid EventId,
    int EventVersion,
    DateTimeOffset OccurredAtUtc,
    string? CorrelationId,
    Guid DishId,
    Guid TenantId,
    string ChangeType,
    string Name,
    string Status)
{
    public const int CurrentVersion = 1;

    public static DishCatalogChangedIntegrationEvent Create(
        Guid dishId,
        Guid tenantId,
        string changeType,
        string name,
        string status) =>
        new(
            Guid.NewGuid(),
            CurrentVersion,
            DateTimeOffset.UtcNow,
            null,
            dishId,
            tenantId,
            changeType,
            name,
            status);
}
