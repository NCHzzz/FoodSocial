namespace FoodSocial.Identity.Api.Contracts;

/// <summary>
/// Represents a request to assign an account-level role to a subject.
/// </summary>
/// <param name="Subject">The target subject.</param>
/// <param name="Role">The canonical role to assign.</param>
public sealed record AssignRoleRequest(string Subject, string Role);
