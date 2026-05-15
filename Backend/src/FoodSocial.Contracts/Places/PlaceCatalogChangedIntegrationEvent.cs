namespace FoodSocial.Contracts.Places;

public sealed record PlaceCatalogChangedIntegrationEvent(
    Guid EventId,
    int EventVersion,
    DateTimeOffset OccurredAtUtc,
    string? CorrelationId,
    Guid PlaceId,
    Guid TenantId,
    string ChangeType,
    string Name,
    string Status,
    string City,
    string Area)
{
    public const int CurrentVersion = 1;

    public static PlaceCatalogChangedIntegrationEvent Create(
        Guid placeId,
        Guid tenantId,
        string changeType,
        string name,
        string status,
        string city,
        string area) =>
        new(
            Guid.NewGuid(),
            CurrentVersion,
            DateTimeOffset.UtcNow,
            null,
            placeId,
            tenantId,
            changeType,
            name,
            status,
            city,
            area);
}
