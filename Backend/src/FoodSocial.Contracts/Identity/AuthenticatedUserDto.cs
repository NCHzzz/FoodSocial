namespace FoodSocial.Contracts.Identity;

public sealed record AuthenticatedUserDto(
    string Subject,
    string? PreferredUsername,
    string? Email,
    IReadOnlyList<string> Roles);
