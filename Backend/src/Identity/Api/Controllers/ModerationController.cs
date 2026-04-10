using FoodSocial.Identity.Api.Authorization;
using FoodSocial.Identity.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Identity.Api.Controllers;

/// <summary>
/// Exposes moderator/admin-protected probes for policy verification.
/// </summary>
[ApiController]
[Route("identity/moderation")]
[Authorize(Policy = IdentityAuthorizationPolicies.AccountAdminOrModerator)]
public sealed class ModerationController : ControllerBase
{
    /// <summary>
    /// Returns a lightweight authorization probe payload when moderator/admin policy is satisfied.
    /// </summary>
    /// <returns>The probe response.</returns>
    [HttpGet("audit")]
    [ProducesResponseType<ModerationAuditResponse>(StatusCodes.Status200OK)]
    public ActionResult<ModerationAuditResponse> GetAuditProbe() =>
        Ok(new ModerationAuditResponse(true));
}
