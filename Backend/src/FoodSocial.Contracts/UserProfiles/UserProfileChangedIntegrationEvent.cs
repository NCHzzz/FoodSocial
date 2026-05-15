namespace FoodSocial.Contracts.UserProfiles;

public sealed record UserProfileChangedIntegrationEvent(
    Guid EventId,
    int EventVersion,
    DateTimeOffset OccurredAtUtc,
    string? CorrelationId,
    Guid IdentityId,
    Guid TenantId,
    string ChangeType,
    Guid? TargetId,
    string? TargetType)
{
    public const int CurrentVersion = 1;

    public static UserProfileChangedIntegrationEvent Create(
        Guid identityId,
        Guid tenantId,
        string changeType,
        Guid? targetId = null,
        string? targetType = null) =>
        new(
            Guid.NewGuid(),
            CurrentVersion,
            DateTimeOffset.UtcNow,
            null,
            identityId,
            tenantId,
            changeType,
            targetId,
            targetType);
}
