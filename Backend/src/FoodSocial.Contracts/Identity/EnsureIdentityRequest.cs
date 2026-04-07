namespace FoodSocial.Contracts.Identity;

public sealed record EnsureIdentityRequest(
    string Subject,
    string? PreferredUsername,
    string? Email);
