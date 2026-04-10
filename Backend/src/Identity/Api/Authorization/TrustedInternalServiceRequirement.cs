using Microsoft.AspNetCore.Authorization;

namespace FoodSocial.Identity.Api.Authorization;

/// <summary>
/// Represents the requirement that an internal caller presents explicit trust credentials.
/// </summary>
public sealed class TrustedInternalServiceRequirement : IAuthorizationRequirement;
