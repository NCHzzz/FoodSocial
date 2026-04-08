using System.Security.Claims;
using FoodSocial.Contracts.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Identity.Api.Controllers;

[ApiController]
[Route("identity/current-user")]
[Authorize]
public sealed class CurrentUserController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public ActionResult<CurrentUserResponse> GetCurrentUser()
    {
        var subject = IdentityClaimReader.GetSubject(User);

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Problem(
                title: "Missing subject claim",
                detail: "The authenticated token does not contain a subject claim.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var response = new CurrentUserResponse(
            new AuthenticatedUserDto(
                subject,
                IdentityClaimReader.GetPreferredUsername(User),
                IdentityClaimReader.GetEmail(User),
                IdentityClaimReader.GetRoles(User)),
            DateTimeOffset.UtcNow);

        return Ok(response);
    }
}
