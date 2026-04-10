namespace FoodSocial.Identity.Application;

/// <summary>
/// Represents the trusted claim payload used to ensure a local identity mapping.
/// </summary>
/// <param name="Subject">The trusted external subject claim.</param>
/// <param name="PreferredUsername">The trusted preferred username claim, when present.</param>
/// <param name="Email">The trusted email claim, when present.</param>
public sealed record EnsureIdentityCommand(
    string Subject,
    string? PreferredUsername,
    string? Email);
