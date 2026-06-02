namespace FoodSocial.Identity.Application.DTOs;

/// <summary>
/// Represents the result of assigning an application-owned role.
/// </summary>
/// <param name="Subject">The target subject.</param>
/// <param name="Role">The normalized assigned role.</param>
/// <param name="Created">Indicates whether a new role assignment was created.</param>
public sealed record AssignIdentityRoleResult(string Subject, string Role, bool Created);
