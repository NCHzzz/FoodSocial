using FoodSocial.Identity.Api.Authorization;
using FoodSocial.Identity.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Identity.Api.Controllers;

/// <summary>
/// Exposes internal-only probes that require explicit service trust credentials.
/// </summary>
[ApiController]
[Route("identity/internal")]
public sealed class InternalTrustController : ControllerBase
{
    /// <summary>
    /// Returns a trust probe payload when explicit service credentials are accepted.
    /// </summary>
    /// <returns>The trust probe response.</returns>
    [HttpGet("service-trust-check")]
    [Authorize(Policy = IdentityAuthorizationPolicies.TrustedInternalService)]
    [ProducesResponseType<InternalServiceTrustProbeResponse>(StatusCodes.Status200OK)]
    public ActionResult<InternalServiceTrustProbeResponse> GetTrustProbe() =>
        Ok(new InternalServiceTrustProbeResponse(true));
}
