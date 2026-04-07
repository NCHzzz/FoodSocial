namespace FoodSocial.Contracts.Identity;

public sealed record EnsureIdentityResponse(
    Guid IdentityId,
    string Subject,
    bool Created);
