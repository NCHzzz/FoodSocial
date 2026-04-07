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
        var subject = User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Problem(
                title: "Missing subject claim",
                detail: "The authenticated token does not contain a subject claim.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var response = new CurrentUserResponse(
            new AuthenticatedUserDto(
                subject,
                User.FindFirstValue("preferred_username") ?? User.Identity?.Name,
                User.FindFirstValue(ClaimTypes.Email),
                roles),
            DateTimeOffset.UtcNow);

        return Ok(response);
    }
}
