namespace FoodSocial.Identity.Application.DTOs;

/// <summary>
/// Represents a trusted request to assign an application-owned role to a subject.
/// </summary>
/// <param name="Subject">The target subject.</param>
/// <param name="Role">The requested role.</param>
public sealed record AssignIdentityRoleCommand(string Subject, string Role);
