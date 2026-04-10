using Microsoft.AspNetCore.Authorization;

namespace FoodSocial.Identity.Api.Authorization;

/// <summary>
/// Represents an authorization requirement for one of multiple accepted account-level roles.
/// </summary>
/// <param name="AllowedRoles">The allowed canonical roles.</param>
public sealed record IdentityAppRoleRequirement(IReadOnlySet<string> AllowedRoles) : IAuthorizationRequirement;
