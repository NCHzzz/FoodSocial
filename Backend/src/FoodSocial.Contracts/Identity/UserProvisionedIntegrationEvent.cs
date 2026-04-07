namespace FoodSocial.Contracts.Identity;

public sealed record UserProvisionedIntegrationEvent(
    Guid IdentityId,
    string Subject,
    string? PreferredUsername,
    string? Email,
    DateTimeOffset ProvisionedAtUtc);
