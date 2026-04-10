namespace FoodSocial.Identity.Api.Contracts;

/// <summary>
/// Represents the result of assigning a role to a subject.
/// </summary>
/// <param name="Subject">The target subject.</param>
/// <param name="Role">The assigned role.</param>
/// <param name="Created">Whether a new assignment was created.</param>
public sealed record AssignRoleResponse(string Subject, string Role, bool Created);
