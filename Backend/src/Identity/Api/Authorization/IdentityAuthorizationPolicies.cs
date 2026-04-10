namespace FoodSocial.Identity.Api.Authorization;

/// <summary>
/// Defines policy names used by the Identity API authorization layer.
/// </summary>
public static class IdentityAuthorizationPolicies
{
    /// <summary>
    /// Policy requiring the account-level admin role.
    /// </summary>
    public const string AccountAdminOnly = "identity.account.admin-only";

    /// <summary>
    /// Policy requiring either admin or moderator role.
    /// </summary>
    public const string AccountAdminOrModerator = "identity.account.admin-or-moderator";

    /// <summary>
    /// Policy requiring explicit trusted internal-service credentials.
    /// </summary>
    public const string TrustedInternalService = "identity.internal.trusted-service";
}
