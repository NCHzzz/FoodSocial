namespace FoodSocial.Identity.Application.DTOs;

/// <summary>
/// Represents trusted claims for resolving the current Identity session projection.
/// </summary>
/// <param name="Subject">The trusted external subject.</param>
/// <param name="PreferredUsername">The preferred username from the authenticated token.</param>
/// <param name="Email">The email from the authenticated token.</param>
public sealed record CurrentUserQuery(string Subject, string? PreferredUsername, string? Email);
